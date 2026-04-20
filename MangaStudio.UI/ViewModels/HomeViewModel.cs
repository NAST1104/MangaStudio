using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using MangaStudio.Core.DTOs;
using MangaStudio.Core.Services;
using MangaStudio.UI.Commands;
using MangaStudio.UI.Models;
using MangaStudio.UI.Services;
using Microsoft.Win32;

namespace MangaStudio.UI.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly SettingsViewModel _settings;
    private readonly SettingsService _settingsService;
    private readonly HistoryViewModel _history;
    private CancellationTokenSource? _cts;

    // Raised on the UI thread after processing completes — View shows SummaryWindow
    public event Action<List<ProcessingResult>, string>? ProcessingCompleted;

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

    // ── Quality ───────────────────────────────────────────────────────────────
    private int _quality = 85;
    public int Quality
    {
        get => _quality;
        set => SetField(ref _quality, Math.Clamp(value, 1, 100));
    }

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
        SettingsService settingsService,
        HistoryViewModel history)
    {
        _orchestrator = orchestrator;
        _settings = settings;
        _settingsService = settingsService;
        _history = history;

        var saved = settingsService.Load();
        _outputPath = saved.DefaultOutputPath;
        _quality = saved.DefaultQuality;

        AddFolderCommand = new RelayCommand(
            AddFolder, () => !IsProcessing);

        RemoveFolderCommand = new RelayCommand(
            RemoveFolder, () => !IsProcessing && SelectedFolder is not null);

        BrowseOutputCommand = new RelayCommand(
            BrowseOutput, () => !IsProcessing);

        StartCommand = new AsyncRelayCommand(
            StartProcessingAsync,
            () => !IsProcessing
                  && FolderList.Count > 0
                  && !string.IsNullOrWhiteSpace(OutputPath));

        CancelCommand = new RelayCommand(
            CancelProcessing, () => IsProcessing);

        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.OutputFormat))
                OnPropertyChanged(nameof(QualityLabel));
        };
    }

    // Called from code-behind for drag and drop
    public void AddFolders(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(Directory.Exists))
            if (!FolderList.Contains(path))
                FolderList.Add(path);

        RelayCommand.RaiseCanExecuteChanged();
    }

    private void AddFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select manga root folder" };
        if (dialog.ShowDialog() != true) return;
        AddFolders(new[] { dialog.FolderName });
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
        var dialog = new OpenFolderDialog { Title = "Select output folder" };
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

        var allResults = new List<ProcessingResult>();

        var progress = new Progress<(int current, int total, string currentChapter)>(p =>
        {
            ProgressMax = p.total;
            ProgressValue = p.current;
            CurrentChapter = p.currentChapter;
            StatusText = $"Chapter {p.current}/{p.total}";
        });

        try
        {
            await Task.Run(async () =>
            {
                for (int i = 0; i < folders.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var mangaName = Path.GetFileName(
                        folders[i].TrimEnd(Path.DirectorySeparatorChar,
                                           Path.AltDirectorySeparatorChar));

                    ((IProgress<(int, int, string)>)progress)
                        .Report((i, folders.Count, $"Scanning: {mangaName}"));

                    var results = await _orchestrator.ProcessAllAsync(
                        folders[i], OutputPath, options, progress, token);

                    allResults.AddRange(results);

                    // Save to history
                    var entry = new HistoryEntry
                    {
                        ProcessedAt = DateTime.Now,
                        MangaName = mangaName,
                        MangaRootPath = folders[i],
                        OutputPath = OutputPath,
                        TotalChapters = results.Count,
                        Succeeded = results.Count(r => r.Success && !r.IsSkipped),
                        Skipped = results.Count(r => r.IsSkipped),
                        Failed = results.Count(r => !r.Success),
                        FailedChapterNames = results
                            .Where(r => !r.Success)
                            .Select(r => r.ChapterName)
                            .ToList(),
                        Format = options.Format,
                        Quality = options.Quality
                    };

                    _history.AddEntry(entry);
                }
            }, token);

            var succeeded = allResults.Count(r => r.Success && !r.IsSkipped);
            var skipped = allResults.Count(r => r.IsSkipped);
            var failed = allResults.Count(r => !r.Success);

            StatusText = "Completed";
            SummaryText = $"Done — {succeeded} succeeded, {skipped} skipped, {failed} failed.";
            ProgressValue = ProgressMax;

            ProcessingCompleted?.Invoke(allResults, OutputPath);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
            SummaryText = "Cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            SummaryText = $"Error: {ex.GetType().Name} — {ex.Message}";
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