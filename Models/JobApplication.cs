namespace JobTracker.Models;

/// <summary>
/// Representa una postulación laboral.
///
/// POO aplicada aquí:
///  - Encapsulamiento: las propiedades solo se pueden modificar desde la propia clase
///    (set privado). Para cambiar datos se usa el método <see cref="Update"/>, que
///    además normaliza el texto (recorta espacios, mayúsculas en la moneda, etc.).
///  - Constructores: uno para registros nuevos y otro para registros leídos de la BD (con Id).
///  - Comportamiento propio: <see cref="DaysSinceApplication"/>.
/// </summary>
public class JobApplication
{
    public int Id { get; internal set; }
    public string Company { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public DateTime ApplicationDate { get; private set; }
    public string? Link { get; private set; }
    public decimal? Salary { get; private set; }
    public string Currency { get; private set; } = "COP";
    public ApplicationStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // TODO(FUTURO - IA / bots): campos que se llenarán automáticamente cuando conectes los bots.
    //   public string? Source { get; private set; }      // "LinkedIn", "Computrabajo", "Manual"...
    //   public string? AiSummary { get; private set; }   // resumen de la oferta generado por IA
    //   public int? MatchScore { get; private set; }     // 0-100: qué tanto encaja con tu perfil
    // Al agregarlos recuerda: 1) crear la columna en DatabaseContext.Initialize(),
    // 2) leerla/escribirla en JobApplicationDao, 3) mostrarla en MainWindow.axaml.

    /// <summary>Constructor para una postulación nueva (aún sin Id).</summary>
    public JobApplication(string company, string position, DateTime applicationDate, string? link,
                          decimal? salary, string currency, ApplicationStatus status, string? notes)
    {
        Apply(company, position, applicationDate, link, salary, currency, status, notes);
    }

    /// <summary>Constructor para una postulación que ya existe en la base de datos.</summary>
    public JobApplication(int id, string company, string position, DateTime applicationDate, string? link,
                          decimal? salary, string currency, ApplicationStatus status, string? notes)
        : this(company, position, applicationDate, link, salary, currency, status, notes)
    {
        Id = id;
    }

    public void Update(string company, string position, DateTime applicationDate, string? link,
                       decimal? salary, string currency, ApplicationStatus status, string? notes)
    {
        Apply(company, position, applicationDate, link, salary, currency, status, notes);
    }

    public int DaysSinceApplication => Math.Max(0, (DateTime.Today - ApplicationDate.Date).Days);

    private void Apply(string company, string position, DateTime applicationDate, string? link,
                       decimal? salary, string currency, ApplicationStatus status, string? notes)
    {
        Company = company.Trim();
        Position = position.Trim();
        ApplicationDate = applicationDate.Date;
        Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        Salary = salary;
        Currency = string.IsNullOrWhiteSpace(currency) ? "COP" : currency.Trim().ToUpperInvariant();
        Status = status;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
