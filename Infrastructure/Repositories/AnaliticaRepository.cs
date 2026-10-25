using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Reportes estadísticos con pipelines de agregación sobre la colección "Incidencias".
/// Los cálculos los hace MongoDB en la nube; al backend solo llegan los resultados agrupados.
/// Las etapas se escriben como BsonDocument para que se lean igual que en MongoDB Compass / mongosh.
/// </summary>
public class AnaliticaRepository : IAnaliticaRepository
{
    private const double MilisegundosPorHora = 3_600_000;

    private readonly IMongoCollection<Incidencia> _collection;

    public AnaliticaRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Incidencia>("Incidencias");
    }

    // ─── GET /api/analitica/tiempos-edificio ────────────────────────────────

    public async Task<List<TiempoEdificio>> TiemposPorEdificioAsync(TiemposEdificioConsulta consulta)
    {
        // 1. $match: solo resueltas con ambas fechas registradas (+ filtros)
        var match = new BsonDocument
        {
            { "estado", "resuelta" },
            { "fechas.creacion",   new BsonDocument("$type", "date") },
            { "fechas.resolucion", RangoFecha(consulta.Desde, consulta.Hasta) }
        };
        if (consulta.Categoria is not null) match.Add("categoria", consulta.Categoria);
        if (consulta.Prioridad is not null) match.Add("prioridad", consulta.Prioridad);

        // 2. $project: horas de atención = (resolucion - creacion) en ms / 3,600,000
        var project = new BsonDocument
        {
            { "_id", 0 },
            { "edificio", "$ubicacion.edificio" },
            { "horas", new BsonDocument("$divide", new BsonArray
                {
                    new BsonDocument("$subtract", new BsonArray { "$fechas.resolucion", "$fechas.creacion" }),
                    MilisegundosPorHora
                })
            }
        };

        // 3. $group: por edificio → total de reportes y promedio de horas (+ mínimo y máximo)
        var group = new BsonDocument
        {
            { "_id", "$edificio" },
            { "totalReportes", new BsonDocument("$sum", 1) },
            { "promedioHoras", new BsonDocument("$avg", "$horas") },
            { "minimoHoras",   new BsonDocument("$min", "$horas") },
            { "maximoHoras",   new BsonDocument("$max", "$horas") }
        };

        // 4. $sort: por número de reportes o por tiempo promedio (de mayor a menor)
        var sort = consulta.OrdenarPorTiempo
            ? new BsonDocument { { "promedioHoras", -1 }, { "totalReportes", -1 } }
            : new BsonDocument { { "totalReportes", -1 }, { "promedioHoras", -1 } };

        var resultados = await _collection.Aggregate<BsonDocument>(new BsonDocument[]
        {
            new("$match",   match),
            new("$project", project),
            new("$group",   group),
            new("$sort",    sort)
        }).ToListAsync();

        return resultados.Select(d => new TiempoEdificio(
            Edificio:      d["_id"].IsString ? d["_id"].AsString : "Sin edificio",
            TotalReportes: d["totalReportes"].ToInt32(),
            PromedioHoras: Redondear(d["promedioHoras"]),
            MinimoHoras:   Redondear(d["minimoHoras"]),
            MaximoHoras:   Redondear(d["maximoHoras"]))).ToList();
    }

    // ─── GET /api/analitica/eficiencia-global ───────────────────────────────

    /// <summary>
    /// Un solo pipeline: $match con los filtros y un $facet con tres sub-pipelines que corren sobre los
    /// mismos documentos: horas hombre ($sum), top 10 de etiquetas ($unwind + $group + $limit) y
    /// casos con proveedor externo (tecnico_externo existe y no es null).
    /// </summary>
    public async Task<EficienciaGlobal> EficienciaGlobalAsync(EficienciaGlobalConsulta consulta)
    {
        var match = new BsonDocument();
        if (consulta.Desde is not null || consulta.Hasta is not null)
            match.Add("fechas.creacion", RangoFecha(consulta.Desde, consulta.Hasta));
        if (consulta.Categoria is not null) match.Add("categoria", consulta.Categoria);
        if (consulta.Edificio  is not null) match.Add("ubicacion.edificio", consulta.Edificio);

        var horas = new BsonDocument("$ifNull", new BsonArray { "$horas_invertidas", 0 });

        var facet = new BsonDocument
        {
            // KPI 1: horas hombre totales
            { "general", new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", BsonNull.Value },
                        { "totalIncidencias", new BsonDocument("$sum", 1) },
                        { "horasHombre",      new BsonDocument("$sum", horas) },
                        { "conHoras",         new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                                                  { new BsonDocument("$gt", new BsonArray { horas, 0 }), 1, 0 })) }
                    })
                }
            },
            // KPI 2: top 10 etiquetas
            { "topTags", new BsonArray
                {
                    new BsonDocument("$unwind", "$tags"),
                    new BsonDocument("$group", new BsonDocument { { "_id", "$tags" }, { "total", new BsonDocument("$sum", 1) } }),
                    new BsonDocument("$sort",  new BsonDocument { { "total", -1 }, { "_id", 1 } }),
                    new BsonDocument("$limit", 10)
                }
            },
            // KPI 3: casos derivados a proveedores externos, por empresa
            { "externos", new BsonArray
                {
                    // tecnico_externo existe, no es null y no está marcado como "no requiere servicio externo"
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "tecnico_externo", new BsonDocument { { "$exists", true }, { "$ne", BsonNull.Value } } },
                        { "tecnico_externo.requiere_servicio_externo", new BsonDocument("$ne", false) }
                    }),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id",   new BsonDocument("$ifNull", new BsonArray { "$tecnico_externo.empresa_proveedor", "Sin empresa" }) },
                        { "total", new BsonDocument("$sum", 1) }
                    }),
                    new BsonDocument("$sort", new BsonDocument { { "total", -1 }, { "_id", 1 } })
                }
            }
        };

        var resultado = await _collection.Aggregate<BsonDocument>(new BsonDocument[]
        {
            new("$match", match),
            new("$facet", facet)
        }).FirstAsync();

        var general = resultado["general"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;

        var topTags = resultado["topTags"].AsBsonArray
            .Select(t => (t["_id"].ToString()!, t["total"].ToInt32()))
            .ToList();

        var porEmpresa = resultado["externos"].AsBsonArray
            .Select(e => (e["_id"].ToString()!, e["total"].ToInt32()))
            .ToList();

        return new EficienciaGlobal(
            TotalIncidencias:   general?["totalIncidencias"].ToInt32() ?? 0,
            HorasHombreTotales: general is null ? 0 : Redondear(general["horasHombre"]),
            IncidenciasConHoras: general?["conHoras"].ToInt32() ?? 0,
            TopTags:            topTags,
            CasosExternos:      porEmpresa.Sum(e => e.Item2),
            CasosPorEmpresa:    porEmpresa);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>{ $type: "date", $gte: desde, $lt: hasta } (solo los límites que vengan).</summary>
    private static BsonDocument RangoFecha(DateTime? desde, DateTime? hasta)
    {
        var rango = new BsonDocument("$type", "date");
        if (desde is not null) rango.Add("$gte", desde.Value);
        if (hasta is not null) rango.Add("$lt",  hasta.Value);
        return rango;
    }

    private static double Redondear(BsonValue valor)
        => valor.IsNumeric ? Math.Round(valor.ToDouble(), 2) : 0;
}
