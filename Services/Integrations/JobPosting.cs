namespace JobTracker.Services.Integrations;

/// <summary>
/// Oferta de trabajo tal como la devolvería un bot o una API externa.
/// Es el "idioma común" entre los scrapers, la IA y el resto de la aplicación.
/// </summary>
public record JobPosting(
    string Company,
    string Position,
    string Url,
    string? Description = null,
    decimal? Salary = null,
    string? Currency = null,
    string? Source = null);
