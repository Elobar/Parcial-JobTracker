using JobTracker.Models;

namespace JobTracker.Services;

/// <summary>
/// Datos "crudos" tal como salen del formulario (todavía sin validar).
/// La ventana los empaqueta aquí y el servicio decide si son válidos.
/// </summary>
public record JobApplicationForm(
    string Company,
    string Position,
    DateTime? ApplicationDate,
    string? Link,
    string? SalaryText,
    string Currency,
    ApplicationStatus Status,
    string? Notes);

/// <summary>Conteo de postulaciones por etapa, para el embudo de la parte superior.</summary>
public record ApplicationStats(int Total, IReadOnlyDictionary<ApplicationStatus, int> ByStatus)
{
    public int Count(ApplicationStatus status) => ByStatus.TryGetValue(status, out var n) ? n : 0;
}
