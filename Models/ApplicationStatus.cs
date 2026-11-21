namespace JobTracker.Models;

/// <summary>
/// Etapas por las que pasa un proceso de selección.
/// El orden del enum es el orden del embudo (pipeline) que se muestra en la interfaz.
/// </summary>
public enum ApplicationStatus
{
    Applied = 0,
    InReview = 1,
    Interview = 2,
    TechnicalTest = 3,
    Offer = 4,
    Rejected = 5
}

public static class ApplicationStatusExtensions
{
    /// <summary>Texto en español que ve el usuario.</summary>
    public static string ToDisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => "Postulado",
        ApplicationStatus.InReview => "En revisión",
        ApplicationStatus.Interview => "Entrevista",
        ApplicationStatus.TechnicalTest => "Prueba técnica",
        ApplicationStatus.Offer => "Oferta",
        ApplicationStatus.Rejected => "Rechazado",
        _ => status.ToString()
    };
}
