using System.Collections.ObjectModel;
using System.Windows.Input;
using MangaStudio.Core.Services;
using MangaStudio.UI.Commands;
using MangaStudio.UI.Services;
using Microsoft.Win32;
using System.IO;

namespace MangaStudio.UI.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly SettingsViewModel _settings;
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _cts;

    // ── Folder list ───────────────────────────────────────────────────────────
    public ObservableCollection<string> FolderList { get; } = new();

    private string? _selectedFolder;
    public string? SelectedFolder
    {
        get => _selectedFolder;
        set => SetField(ref _selectedFolder, value);
    }

    // ── Output path ───────────────────────────────────────────────────────────
    private string _outputPath = string.Empty;
    public string OutputPath
    {
        get => _outputPath;
        set { if (SetField(ref _outputPath, value)) PersistOutputPath(value); }
    }

    // ── Per-session quality ───────────────────────────────────────────────────
    private int _quality = 85;
    public int Quality
    {
        get => _quality;
        set => SetField(ref _quality, Math.Clamp(value, 1, 100));
    }
    // Shown next to the quality slider so the user knows which format is active
    public string QualityLabel => $"Output Quality ({_settings.OutputFormat})";

    // ── Processing state ──────────────────────────────────────────────────────
    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            SetField(ref _isProcessing, value);
            OnPropertyChanged(nameof(IsNotProcessing));
            RelayCommand.RaiseCanExecuteChanged();
        }
    }
    public bool IsNotProcessing => !_isProcessing;

    // ── Progress ──────────────────────────────────────────────────────────────
    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        private set => SetField(ref _progressValue, value);
    }

    private int _progressMax = 1;
    public int ProgressMax
    {
        get => _progressMax;
        private set => SetField(ref _progressMax, Math.Max(1, value));
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    private string _currentChapter = string.Empty;
    public string CurrentChapter
    {
        get => _currentChapter;
        private set => SetField(ref _currentChapter, value);
    }

    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        private set => SetField(ref _summaryText, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand AddFolderCommand { get; }
    public ICommand RemoveFolderCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }

    public HomeViewModel(
        PipelineOrchestrator orchestrator,
        SettingsViewModel settings,
        SettingsService settingsService)
    {
        _orchestrator = orchestrator;
        _settings = settings;
        _settingsService = settingsService;

        var saved = settingsService.Load();
        _outputPath = saved.DefaultOutputPath;
        _quality = saved.DefaultQuality;

        AddFolderCommand = new RelayCommand(
            AddFolder,
            () => !IsProcessing);

        RemoveFolderCommand = new RelayCommand(
            RemoveFolder,
            () => !IsProcessing && SelectedFolder is not null);

        BrowseOutputCommand = new RelayCommand(
            BrowseOutput,
            () => !IsProcessing);

        StartCommand = new AsyncRelayCommand(
            StartProcessingAsync,
            () => !IsProcessing
                  && FolderList.Count > 0
                  && !string.IsNullOrWhiteSpace(OutputPath));

        CancelCommand = new RelayCommand(
            CancelProcessing,
            () => IsProcessing);

        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.OutputFormat))
                OnPropertyChanged(nameof(QualityLabel));
        };
    }

    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select manga root folder"
        };

        if (dialog.ShowDialog() != true) return;

        var path = dialog.FolderName;
        if (!FolderList.Contains(path))
            FolderList.Add(path);

        RelayCommand.RaiseCanExecuteChanged();
    }

    private void RemoveFolder()
    {
        if (SelectedFolder is null) return;
        FolderList.Remove(SelectedFolder);
        SelectedFolder = null;
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select output folder"
        };

        if (dialog.ShowDialog() == true)
            OutputPath = dialog.FolderName;
    }

    private async Task StartProcessingAsync()
    {
        IsProcessing = true;
        SummaryText = string.Empty;
        ProgressValue = 0;
        StatusText = "Starting...";

        _cts = new CancellationTokenSource();

        var folders = FolderList.ToList();
        var options = _settings.BuildExportOptions(Quality);
        var token = _cts.Token;

        int totalSucceeded = 0;
        int totalFailed = 0;

        // Marshal progress updates back to the UI thread
        var progress = new Progress<(int current, int total, string currentChapter)>(p =>
        {
            ProgressMax = p.total;
            ProgressValue = p.current;
            CurrentChapter = p.currentChapter;
            StatusText = $"Chapter {p.current}/{p.total}";
        });

        try
        {
            // Task.Run moves all synchronous pipeline work (IO, image processing)
            // off the UI thread onto the thread pool.
            // Progress<T> and CancellationToken cross the boundary safely.
            var (succeeded, failed) = await Task.Run(async () =>
            {
                int s = 0, f = 0;

                for (int i = 0; i < folders.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var folderName = Path.GetFileName(folders[i]);

                    // Post a status update to the UI thread
                    ((IProgress<(int, int, string)>)progress)
                        .Report((i, folders.Count, $"Scanning: {folderName}"));

                    var results = await _orchestrator.ProcessAllAsync(
                        folders[i], OutputPath, options, progress, token);

                    s += results.Count(r => r.Success);
                    f += results.Count(r => !r.Success);
                }

                return (s, f);
            }, token);

            totalSucceeded = succeeded;
            totalFailed = failed;

            StatusText = "Completed";
            SummaryText = $"Done — {totalSucceeded} chapter(s) succeeded, {totalFailed} failed.";
            ProgressValue = ProgressMax;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
            SummaryText = $"Cancelled — {totalSucceeded} chapter(s) completed before cancel.";
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            SummaryText = $"Error: {ex.GetType().Name} — {ex.Message}";

            // Inner exception often has the real cause (e.g. libvips error inside a wrapper)
            if (ex.InnerException is not null)
                SummaryText += $"\nCause: {ex.InnerException.Message}";
        }
        finally
        {
            IsProcessing = false;
            CurrentChapter = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelProcessing()
    {
        _cts?.Cancel();
        StatusText = "Cancelling...";
    }

    private void PersistOutputPath(string path)
    {
        var s = _settingsService.Load();
        s.DefaultOutputPath = path;
        _settingsService.Save(s);
    }
}