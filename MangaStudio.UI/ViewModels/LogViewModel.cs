using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Windows.Input;
using MangaStudio.UI.Commands;
using MangaStudio.UI.Models;
using Microsoft.Win32;

namespace MangaStudio.UI.ViewModels;

public class LogViewModel : BaseViewModel
{
    private const int MaxEntries = 2000;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public ICommand ExportCommand { get; }
    public ICommand ClearCommand { get; }

    public LogViewModel()
    {
        ExportCommand = new RelayCommand(ExportToFile, () => Entries.Count > 0);
        ClearCommand = new RelayCommand(Clear, () => Entries.Count > 0);
    }

    // Called by UiLogSink — always on the UI thread
    public void AddEntry(LogEntry entry)
    {
        if (Entries.Count >= MaxEntries)
            Entries.RemoveAt(0);

        Entries.Add(entry);
    }

    private void Clear() => Entries.Clear();

    private void ExportToFile()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Logs",
            Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt",
            FileName = $"MangaStudio_{DateTime.Now:yyyyMMdd_HHmmss}.log"
        };

        if (dialog.ShowDialog() != true) return;

        var sb = new StringBuilder();
        foreach (var e in Entries)
            sb.AppendLine($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level,-11}] {e.Message}");

        File.WriteAllText(dialog.FileName, sb.ToString());
    }
}