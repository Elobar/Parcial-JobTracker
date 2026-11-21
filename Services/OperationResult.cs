namespace JobTracker.Services;

/// <summary>Resultado de una operación de la capa de servicio: éxito o lista de errores legibles.</summary>
public class OperationResult
{
    public bool Success { get; }
    public string Message { get; }
    public IReadOnlyList<string> Errors { get; }

    private OperationResult(bool success, string message, IReadOnlyList<string> errors)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public static OperationResult Ok(string message) =>
        new OperationResult(true, message, Array.Empty<string>());

    public static OperationResult Fail(string message, IEnumerable<string>? errors = null) =>
        new OperationResult(false, message, (errors ?? Array.Empty<string>()).ToList());
}
