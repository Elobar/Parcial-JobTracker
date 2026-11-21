namespace JobTracker.Services.Integrations;

/// <summary>
/// PUNTO DE CONEXIÓN para bots que leen páginas de empleo.
///
/// TODO(FUTURO - BOTS): crea clases que implementen esta interfaz, por ejemplo:
///   - LinkedInScraper      (Playwright / Selenium)
///   - ComputrabajoScraper  (HttpClient + HtmlAgilityPack)
///   - ElEmpleoScraper
/// y cámbialas en MainWindow (donde se crea JobApplicationService) en lugar de NullJobScraper.
/// Nada más en el programa necesita modificarse.
/// </summary>
public interface IJobScraper
{
    /// <summary>Lee UNA oferta a partir de su enlace. Devuelve null si no pudo.</summary>
    Task<JobPosting?> FetchFromUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Busca ofertas por palabras clave (ej. "desarrollador C# remoto").</summary>
    Task<IReadOnlyList<JobPosting>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación vacía ("Null Object"): permite que el programa funcione hoy sin bots.
/// Cuando tengas un bot real, deja de usar esta clase.
/// </summary>
public class NullJobScraper : IJobScraper
{
    public Task<JobPosting?> FetchFromUrlAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult<JobPosting?>(null);

    public Task<IReadOnlyList<JobPosting>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JobPosting>>(Array.Empty<JobPosting>());
}
