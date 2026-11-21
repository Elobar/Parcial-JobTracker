using System.Globalization;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services.Integrations;
using Microsoft.Data.Sqlite;

namespace JobTracker.Services;

/// <summary>
/// Lógica del programa: validaciones, reglas y coordinación entre la interfaz y el DAO.
/// La ventana NO habla con la base de datos; siempre pasa por aquí.
/// </summary>
public class JobApplicationService
{
    private const int MaxTextLength = 80;

    private readonly JobApplicationDao _dao;
    private readonly IJobScraper _scraper;
    private readonly IJobAiAnalyzer _analyzer;

    public JobApplicationService(JobApplicationDao dao, IJobScraper scraper, IJobAiAnalyzer analyzer)
    {
        _dao = dao;
        _scraper = scraper;
        _analyzer = analyzer;
    }

    // ---------------------------------------------------------------- CRUD
    public List<JobApplication> GetAll() => _dao.GetAll();

    public OperationResult Create(JobApplicationForm form)
    {
        var errors = Validate(form, out var salary);
        if (errors.Count > 0)
        {
            return OperationResult.Fail("Hay datos por corregir.", errors);
        }

        try
        {
            var application = new JobApplication(form.Company, form.Position, form.ApplicationDate!.Value,
                form.Link, salary, form.Currency, form.Status, form.Notes);
            _dao.Insert(application);
            return OperationResult.Ok("Postulación guardada.");
        }
        catch (SqliteException ex)
        {
            return OperationResult.Fail("No se pudo guardar en la base de datos.", new[] { ex.Message });
        }
    }

    public OperationResult Update(int id, JobApplicationForm form)
    {
        var errors = Validate(form, out var salary);
        if (errors.Count > 0)
        {
            return OperationResult.Fail("Hay datos por corregir.", errors);
        }

        try
        {
            var application = new JobApplication(id, form.Company, form.Position, form.ApplicationDate!.Value,
                form.Link, salary, form.Currency, form.Status, form.Notes);

            return _dao.Update(application)
                ? OperationResult.Ok("Cambios guardados.")
                : OperationResult.Fail("La postulación ya no existe.");
        }
        catch (SqliteException ex)
        {
            return OperationResult.Fail("No se pudo actualizar la base de datos.", new[] { ex.Message });
        }
    }

    public OperationResult Delete(int id)
    {
        try
        {
            return _dao.Delete(id)
                ? OperationResult.Ok("Postulación eliminada.")
                : OperationResult.Fail("La postulación ya no existe.");
        }
        catch (SqliteException ex)
        {
            return OperationResult.Fail("No se pudo eliminar de la base de datos.", new[] { ex.Message });
        }
    }

    // ------------------------------------------------- consultas en memoria
    public IEnumerable<JobApplication> Filter(IEnumerable<JobApplication> source, string? text, ApplicationStatus? status)
    {
        var term = text?.Trim();

        return source.Where(a =>
            (status is null || a.Status == status) &&
            (string.IsNullOrEmpty(term) ||
             a.Company.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             a.Position.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    public ApplicationStats GetStats(IReadOnlyCollection<JobApplication> applications)
    {
        var byStatus = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(s => s, s => applications.Count(a => a.Status == s));
        return new ApplicationStats(applications.Count, byStatus);
    }

    // --------------------------------------- conexiones futuras (bots / IA)
    /// <summary>
    /// TODO(FUTURO - BOTS): hoy devuelve null porque se usa NullJobScraper.
    /// Cuando conectes un bot real, este método llenará el formulario automáticamente.
    /// </summary>
    public Task<JobPosting?> ImportFromLinkAsync(string url, CancellationToken cancellationToken = default) =>
        _scraper.FetchFromUrlAsync(url, cancellationToken);

    /// <summary>
    /// TODO(FUTURO - BOTS): buscar ofertas por palabras clave y mostrarlas en una lista
    /// "Descubrir ofertas" dentro de la ventana principal.
    /// </summary>
    public Task<IReadOnlyList<JobPosting>> SearchPostingsAsync(string query, CancellationToken cancellationToken = default) =>
        _scraper.SearchAsync(query, cancellationToken);

    /// <summary>
    /// TODO(FUTURO - IA): resumen automático de la oferta. Hoy devuelve null (NullJobAnalyzer).
    /// </summary>
    public Task<string?> SummarizePostingAsync(JobPosting posting, CancellationToken cancellationToken = default) =>
        _analyzer.SummarizeAsync(posting, cancellationToken);

    // ------------------------------------------------------------ validación
    private static List<string> Validate(JobApplicationForm form, out decimal? salary)
    {
        var errors = new List<string>();
        salary = null;

        if (string.IsNullOrWhiteSpace(form.Company))
            errors.Add("La empresa es obligatoria.");
        else if (form.Company.Trim().Length > MaxTextLength)
            errors.Add($"La empresa no puede superar {MaxTextLength} caracteres.");

        if (string.IsNullOrWhiteSpace(form.Position))
            errors.Add("El cargo es obligatorio.");
        else if (form.Position.Trim().Length > MaxTextLength)
            errors.Add($"El cargo no puede superar {MaxTextLength} caracteres.");

        if (form.ApplicationDate is null)
            errors.Add("Selecciona la fecha de postulación.");
        else if (form.ApplicationDate.Value.Date > DateTime.Today)
            errors.Add("La fecha de postulación no puede ser futura.");

        if (!string.IsNullOrWhiteSpace(form.Link) && !IsHttpUrl(form.Link.Trim()))
            errors.Add("El enlace debe empezar por http:// o https://.");

        if (!string.IsNullOrWhiteSpace(form.SalaryText))
        {
            if (!TryParseSalary(form.SalaryText, out var parsed))
                errors.Add("El salario debe ser un número válido, por ejemplo 4500000.");
            else if (parsed < 0)
                errors.Add("El salario no puede ser negativo.");
            else
                salary = parsed;
        }

        return errors;
    }

    private static bool IsHttpUrl(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool TryParseSalary(string text, out decimal value)
    {
        var clean = text.Trim().Replace("$", "").Replace(" ", "");
        return decimal.TryParse(clean, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
