using System.Windows.Input;
using MangaStudio.Core.DTOs;
using MangaStudio.Core.Enums;
using MangaStudio.UI.Commands;
using MangaStudio.UI.Models;
using MangaStudio.UI.Services;

namespace MangaStudio.UI.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _service;
    private readonly AppSettings _settings;

    // ── Imaging backend ───────────────────────────────────────────────────────
    private ImagingBackend _backend;
    public ImagingBackend Backend
    {
        get => _backend;
        set { if (SetField(ref _backend, value)) Save(); }
    }

    public IEnumerable<ImagingBackend> AvailableBackends => Enum.GetValues<ImagingBackend>();

    // ── Output format ─────────────────────────────────────────────────────────
    private ExportFormat _outputFormat;
    public ExportFormat OutputFormat
    {
        get => _outputFormat;
        set
        {
            if (SetField(ref _outputFormat, value))
            {
                // Update notes — do NOT re-clamp MaxStitchHeight here.
                // The user's value is preserved. The format limit is only
                // enforced at processing time inside BuildExportOptions.
                OnPropertyChanged(nameof(FormatNote));
                OnPropertyChanged(nameof(MaxSafeHeight));
                OnPropertyChanged(nameof(MaxStitchHeightNote));
                Save();
            }
        }
    }

    public IEnumerable<ExportFormat> AvailableFormats => Enum.GetValues<ExportFormat>();

    public string FormatNote => _outputFormat switch
    {
        ExportFormat.WebP => "WebP — best compression, max 15,000px per image. Recommended for short chapters.",
        ExportFormat.Jpg => "JPEG — no height limit, universal support. Recommended for tall manhua chapters.",
        _ => string.Empty
    };

    public int MaxSafeHeight => _outputFormat == ExportFormat.WebP
        ? ExportOptions.WebPMaxHeight
        : ExportOptions.JpgMaxHeight;

    // ── Max stitch height ─────────────────────────────────────────────────────
    private int _maxStitchHeight;
    public int MaxStitchHeight
    {
        get => _maxStitchHeight;
        set
        {
            // Clamp to at least 500px and at most the current format's safe limit
            var clamped = Math.Min(
                Math.Max(500, value),
                MaxSafeHeight);

            if (SetField(ref _maxStitchHeight, clamped))
            {
                OnPropertyChanged(nameof(MaxStitchHeightNote));
                Save();
            }
        }
    }

    public string MaxStitchHeightNote =>
        $"Current format limit: {MaxSafeHeight:N0}px. " +
        $"Your setting: {_maxStitchHeight:N0}px.";

    // ── Delete originals ──────────────────────────────────────────────────────
    private bool _deleteOriginals;
    public bool DeleteOriginals
    {
        get => _deleteOriginals;
        set { if (SetField(ref _deleteOriginals, value)) Save(); }
    }

    // ── Default quality ───────────────────────────────────────────────────────
    private int _defaultQuality;
    public int DefaultQuality
    {
        get => _defaultQuality;
        set { if (SetField(ref _defaultQuality, Math.Clamp(value, 1, 100))) Save(); }
    }

    public string BackendNote => "Backend change takes effect on next app launch.";

    public ICommand ResetCommand { get; }

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        _settings = service.Load();

        _backend = _settings.Backend;
        _outputFormat = _settings.OutputFormat;
        _maxStitchHeight = _settings.MaxStitchHeight;
        _deleteOriginals = _settings.DeleteOriginals;
        _defaultQuality = _settings.DefaultQuality;

        ResetCommand = new RelayCommand(ResetToDefaults);
    }

    // Called by HomeViewModel — combines saved settings with per-session quality
    public ExportOptions BuildExportOptions(int sessionQuality) => new()
    {
        Format = _outputFormat,
        Quality = sessionQuality,
        MaxStitchHeight = Math.Min(_maxStitchHeight, MaxSafeHeight),
        DeleteOriginals = _deleteOriginals
    };

    private void Save()
    {
        _settings.Backend = _backend;
        _settings.OutputFormat = _outputFormat;
        _settings.MaxStitchHeight = _maxStitchHeight;
        _settings.DeleteOriginals = _deleteOriginals;
        _settings.DefaultQuality = _defaultQuality;
        _service.Save(_settings);
    }

    private void ResetToDefaults()
    {
        Backend = ImagingBackend.Vips;
        OutputFormat = ExportFormat.WebP;
        MaxStitchHeight = 10000;
        DeleteOriginals = false;
        DefaultQuality = 85;
    }
}