namespace JobTracker.Services.Integrations;

/// <summary>
/// PUNTO DE CONEXIÓN para IA que analiza ofertas.
///
/// TODO(FUTURO - IA): crea una clase que implemente esta interfaz y llame al modelo que prefieras
/// (API de Claude, OpenAI, un modelo local con Ollama, etc.). Ideas:
///   - Resumir la descripción de la oferta en 3 líneas.
///   - Calcular qué tanto encaja con tu perfil (0-100).
///   - Redactar una carta de presentación a la medida.
///   - Detectar salarios o tecnologías mencionadas en el texto.
/// Guarda las llaves de API en variables de entorno, NUNCA en el código ni en GitHub.
/// </summary>
public interface IJobAiAnalyzer
{
    Task<string?> SummarizeAsync(JobPosting posting, CancellationToken cancellationToken = default);

    Task<int?> ScoreMatchAsync(JobPosting posting, string candidateProfile, CancellationToken cancellationToken = default);
}

/// <summary>Implementación vacía para que el programa funcione hoy sin IA.</summary>
public class NullJobAnalyzer : IJobAiAnalyzer
{
    public Task<string?> SummarizeAsync(JobPosting posting, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<int?> ScoreMatchAsync(JobPosting posting, string candidateProfile, CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
}
