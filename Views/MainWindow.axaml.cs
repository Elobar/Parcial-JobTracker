using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using JobTracker.Services.Integrations;
using JobTracker.UI;

namespace JobTracker.Views;

/// <summary>
/// Ventana principal. Solo se ocupa de la interfaz: lee el formulario, llama al servicio
/// y muestra el resultado. No contiene SQL ni reglas de validación.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DatabaseContext _database;
    private readonly JobApplicationService _service;

    private readonly ApplicationStatus[] _statuses = Enum.GetValues<ApplicationStatus>();
    private readonly Dictionary<ApplicationStatus, TextBlock> _statBlocks;
    private readonly DispatcherTimer _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };

    private List<JobApplication> _all = new();
    private JobApplication? _editing;      // null = el formulario está creando una postulación nueva
    private bool _suppressSelection;       // evita que cambiar la lista por código recargue el formulario
    private bool _ready;                   // evita reaccionar a eventos mientras se construye la ventana

    public MainWindow()
    {
        InitializeComponent();

        // Composición de dependencias: aquí se eligen las implementaciones concretas.
        // TODO(FUTURO - BOTS / IA): reemplaza NullJobScraper y NullJobAnalyzer por tus clases reales.
        _database = new DatabaseContext();
        _service = new JobApplicationService(
            new JobApplicationDao(_database),
            new NullJobScraper(),
            new NullJobAnalyzer());

        _statBlocks = new Dictionary<ApplicationStatus, TextBlock>
        {
            [ApplicationStatus.Applied] = StatApplied,
            [ApplicationStatus.InReview] = StatInReview,
            [ApplicationStatus.Interview] = StatInterview,
            [ApplicationStatus.TechnicalTest] = StatTechnicalTest,
            [ApplicationStatus.Offer] = StatOffer,
            [ApplicationStatus.Rejected] = StatRejected
        };

        _toastTimer.Tick += (_, _) =>
        {
            Toast.IsVisible = false;
            _toastTimer.Stop();
        };

        CurrencyBox.ItemsSource = new[] { "COP", "USD", "EUR" };
        StatusBox.ItemsSource = _statuses.Select(s => s.ToDisplayName()).ToList();
        FilterBox.ItemsSource = new[] { "Todos los estados" }
            .Concat(_statuses.Select(s => s.ToDisplayName()))
            .ToList();
        FilterBox.SelectedIndex = 0;

        // Filtra la lista mientras el usuario escribe en el buscador.
        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) ApplyFilter();
        };

        DbPathText.Text = "Base de datos: " + _database.DatabasePath;
        ClearForm();

        try
        {
            _database.Initialize();
            LoadData();
        }
        catch (Exception ex)
        {
            ShowToast("No se pudo abrir la base de datos: " + ex.Message, isError: true);
        }

        _ready = true;
    }

    // ================================================================ DATOS
    private void LoadData()
    {
        _all = _service.GetAll();
        UpdateStats();
        ApplyFilter();
    }

    private void UpdateStats()
    {
        var stats = _service.GetStats(_all);

        TotalText.Text = stats.Total switch
        {
            0 => "Aún no has registrado postulaciones",
            1 => "1 postulación registrada",
            _ => $"{stats.Total} postulaciones registradas"
        };

        foreach (var (status, block) in _statBlocks)
        {
            block.Text = stats.Count(status).ToString();
        }
    }

    private void ApplyFilter()
    {
        var index = FilterBox.SelectedIndex;
        ApplicationStatus? statusFilter = index > 0 ? _statuses[index - 1] : null;

        var cards = _service.Filter(_all, SearchBox.Text, statusFilter)
            .Select(a => new ApplicationCard(a))
            .ToList();

        _suppressSelection = true;
        ApplicationsList.ItemsSource = cards;
        if (_editing != null)
        {
            ApplicationsList.SelectedItem = cards.FirstOrDefault(c => c.Application.Id == _editing.Id);
        }
        _suppressSelection = false;

        // Estado vacío: distinto mensaje si no hay datos o si el filtro no encontró nada.
        EmptyState.IsVisible = cards.Count == 0;
        if (_all.Count == 0)
        {
            EmptyTitle.Text = "Todavía no hay postulaciones";
            EmptyHint.Text = "Completa el formulario y guarda la primera.";
        }
        else
        {
            EmptyTitle.Text = "Sin resultados";
            EmptyHint.Text = "Prueba con otra búsqueda o cambia el estado.";
        }
    }

    // ============================================================ FORMULARIO
    private void ClearForm()
    {
        _editing = null;

        CompanyBox.Text = string.Empty;
        PositionBox.Text = string.Empty;
        LinkBox.Text = string.Empty;
        SalaryBox.Text = string.Empty;
        NotesBox.Text = string.Empty;
        DateBox.SelectedDate = DateTime.Today;
        StatusBox.SelectedIndex = 0;
        CurrencyBox.SelectedIndex = 0;

        FormTitle.Text = "Nueva postulación";
        DeleteButton.IsVisible = false;
        ErrorBox.IsVisible = false;

        _suppressSelection = true;
        ApplicationsList.SelectedItem = null;
        _suppressSelection = false;
    }

    private void LoadIntoForm(JobApplication application)
    {
        _editing = application;

        CompanyBox.Text = application.Company;
        PositionBox.Text = application.Position;
        LinkBox.Text = application.Link;
        SalaryBox.Text = application.Salary?.ToString("0.##");
        NotesBox.Text = application.Notes;
        DateBox.SelectedDate = application.ApplicationDate;
        StatusBox.SelectedIndex = Array.IndexOf(_statuses, application.Status);
        CurrencyBox.SelectedItem = application.Currency;

        FormTitle.Text = "Editar postulación";
        DeleteButton.IsVisible = true;
        ErrorBox.IsVisible = false;
    }

    private JobApplicationForm ReadForm() => new JobApplicationForm(
        CompanyBox.Text ?? string.Empty,
        PositionBox.Text ?? string.Empty,
        DateBox.SelectedDate,
        LinkBox.Text,
        SalaryBox.Text,
        CurrencyBox.SelectedItem as string ?? "COP",
        _statuses[Math.Max(0, StatusBox.SelectedIndex)],
        NotesBox.Text);

    // ============================================================== EVENTOS
    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        ClearForm();
        CompanyBox.Focus();
    }

    private void OnClearClick(object? sender, RoutedEventArgs e) => ClearForm();

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var form = ReadForm();
        var result = _editing == null ? _service.Create(form) : _service.Update(_editing.Id, form);

        if (!result.Success)
        {
            ErrorText.Text = result.Errors.Count > 0
                ? string.Join(Environment.NewLine, result.Errors.Select(err => "• " + err))
                : result.Message;
            ErrorBox.IsVisible = true;
            ShowToast("Revisa los datos del formulario", isError: true);
            return;
        }

        ShowToast(result.Message, isError: false);
        ClearForm();
        LoadData();
    }

    private void OnApplicationSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection) return;

        if (ApplicationsList.SelectedItem is ApplicationCard card)
        {
            LoadIntoForm(card.Application);
        }
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_ready) ApplyFilter();
    }

    private void OnOpenLinkClick(object? sender, RoutedEventArgs e)
    {
        var link = LinkBox.Text?.Trim();
        if (string.IsNullOrEmpty(link) || !Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ShowToast("Escribe un enlace válido que empiece por http:// o https://", isError: true);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        }
        catch (Exception)
        {
            ShowToast("No se pudo abrir el navegador.", isError: true);
        }
    }

    // TODO(FUTURO - BOTS + IA): este es el primer punto de conexión. Cuando reemplaces NullJobScraper
    // por un bot real, JobApplicationService.ImportFromLinkAsync devolverá un JobPosting y este método
    // llenará el formulario solo. También puedes llamar a SummarizePostingAsync para guardar el resumen.
    private async void OnAutofillClick(object? sender, RoutedEventArgs e)
    {
        var url = LinkBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            ShowToast("Pega primero el enlace de la oferta.", isError: true);
            return;
        }

        try
        {
            var posting = await _service.ImportFromLinkAsync(url);
            if (posting == null)
            {
                ShowToast("El autocompletado con bots e IA estará disponible próximamente.", isError: false);
                return;
            }

            CompanyBox.Text = posting.Company;
            PositionBox.Text = posting.Position;
            if (posting.Salary.HasValue) SalaryBox.Text = posting.Salary.Value.ToString("0.##");
            if (!string.IsNullOrWhiteSpace(posting.Currency)) CurrencyBox.SelectedItem = posting.Currency;
            ShowToast("Datos importados desde la oferta.", isError: false);
        }
        catch (Exception)
        {
            ShowToast("No se pudo leer la oferta.", isError: true);
        }
    }

    // ---------------------------------------------------------- eliminación
    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_editing == null) return;

        DeleteMessage.Text = $"Se eliminará \"{_editing.Position}\" en {_editing.Company}. Esta acción no se puede deshacer.";
        DeleteOverlay.IsVisible = true;
    }

    private void OnDeleteCancel(object? sender, RoutedEventArgs e) => DeleteOverlay.IsVisible = false;

    private void OnDeleteConfirm(object? sender, RoutedEventArgs e)
    {
        DeleteOverlay.IsVisible = false;
        if (_editing == null) return;

        var result = _service.Delete(_editing.Id);
        ShowToast(result.Message, isError: !result.Success);

        ClearForm();
        LoadData();
    }

    // ============================================================ NOTIFICACIÓN
    private void ShowToast(string message, bool isError)
    {
        ToastText.Text = message;
        Toast.Background = new SolidColorBrush(Color.Parse(isError ? "#B42318" : "#15803D"));
        Toast.IsVisible = true;

        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
