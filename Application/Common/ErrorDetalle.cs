namespace Application.Common;

/// <summary>
/// Error de validación ligado a un campo específico, para que el frontend
/// pueda marcar en rojo el input exacto y mostrar el motivo.
/// </summary>
/// <param name="Campo">Ruta del campo. Ej: "contenido[2].salon", "correo".</param>
/// <param name="Mensaje">Por qué está mal. Ej: "No puede estar vacío."</param>
public record ErrorDetalle(string Campo, string Mensaje);
