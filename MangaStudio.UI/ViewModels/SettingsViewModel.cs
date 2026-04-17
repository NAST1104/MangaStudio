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

    // ── Max stitch height ─────────────────────────────────────────────────────
    private int _maxStitchHeight;
    public int MaxStitchHeight
    {
        get => _maxStitchHeight;
        set { if (SetField(ref _maxStitchHeight, Math.Max(500, value))) Save(); }
    }

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

    // ── Backend note ──────────────────────────────────────────────────────────
    public string BackendNote =>
        "Backend change takes effect on next app launch.";

    public ICommand ResetCommand { get; }

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        _settings = service.Load();

        _backend = _settings.Backend;
        _maxStitchHeight = _settings.MaxStitchHeight;
        _deleteOriginals = _settings.DeleteOriginals;
        _defaultQuality = _settings.DefaultQuality;

        ResetCommand = new RelayCommand(ResetToDefaults);
    }

    // Build ExportOptions combining saved settings and the per-session quality override
    public ExportOptions BuildExportOptions(int sessionQuality) => new()
    {
        Format = ExportFormat.WebP,
        Quality = sessionQuality,
        MaxStitchHeight = _maxStitchHeight,
        DeleteOriginals = _deleteOriginals
    };

    private void Save()
    {
        _settings.Backend = _backend;
        _settings.MaxStitchHeight = _maxStitchHeight;
        _settings.DeleteOriginals = _deleteOriginals;
        _settings.DefaultQuality = _defaultQuality;
        _service.Save(_settings);
    }

    private void ResetToDefaults()
    {
        Backend = ImagingBackend.Vips;
        MaxStitchHeight = 10000;
        DeleteOriginals = false;
        DefaultQuality = 85;
    }
}