using System.Globalization;
using Avalonia.Media;
using JobTracker.Models;

namespace JobTracker.UI;

/// <summary>
/// Adaptador de presentación: toma una JobApplication y expone justo lo que la lista necesita
/// (textos ya formateados y colores). Así el modelo no se mezcla con detalles visuales.
/// </summary>
public class ApplicationCard
{
    private static readonly string[] AvatarPalette =
        { "#0F766E", "#2563EB", "#7C3AED", "#C2410C", "#B7791F", "#BE185D" };

    public JobApplication Application { get; }

    public string Company => Application.Company;
    public string Position => Application.Position;
    public string Initial { get; }
    public string Subtitle { get; }
    public string StatusText { get; }

    public IBrush AvatarBackground { get; }
    public IBrush AvatarForeground { get; }
    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }

    public ApplicationCard(JobApplication application)
    {
        Application = application;

        Initial = application.Company.Length > 0
            ? application.Company.Substring(0, 1).ToUpperInvariant()
            : "?";
        StatusText = application.Status.ToDisplayName();

        // Fecha · antigüedad · salario (solo si existe)
        var culture = CultureInfo.CurrentCulture;
        var date = application.ApplicationDate.ToString("dd MMM yyyy", culture);
        var days = application.DaysSinceApplication;
        var ago = days == 0 ? "hoy" : days == 1 ? "hace 1 día" : $"hace {days} días";
        Subtitle = $"{date}  ·  {ago}";
        if (application.Salary.HasValue)
        {
            var salary = application.Salary.Value.ToString("N0", culture);
            Subtitle += $"  ·  {salary} {application.Currency}";
        }

        var avatarColor = Color.Parse(AvatarPalette[ColorIndex(application.Company)]);
        AvatarForeground = new SolidColorBrush(avatarColor);
        AvatarBackground = new SolidColorBrush(Tint(avatarColor));

        var statusColor = Color.Parse(StatusHex(application.Status));
        StatusForeground = new SolidColorBrush(statusColor);
        StatusBackground = new SolidColorBrush(Tint(statusColor));
    }

    /// <summary>Color de cada etapa; los mismos valores se usan en el embudo de MainWindow.axaml.</summary>
    public static string StatusHex(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => "#2563EB",
        ApplicationStatus.InReview => "#B7791F",
        ApplicationStatus.Interview => "#7C3AED",
        ApplicationStatus.TechnicalTest => "#C2410C",
        ApplicationStatus.Offer => "#15803D",
        ApplicationStatus.Rejected => "#B42318",
        _ => "#647084"
    };

    // Versión translúcida del color, para fondos suaves.
    private static Color Tint(Color c) => Color.FromArgb(34, c.R, c.G, c.B);

    // Índice estable (no usa GetHashCode, que cambia entre ejecuciones).
    private static int ColorIndex(string text)
    {
        var sum = 0;
        foreach (var ch in text) sum += ch;
        return sum % AvatarPalette.Length;
    }
}
