using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MangaStudio.Core.DTOs;

namespace MangaStudio.UI.Views;

public partial class SummaryWindow : Window
{
    private readonly string _outputPath;

    public SummaryWindow(List<ProcessingResult> results, string outputPath)
    {
        InitializeComponent();
        _outputPath = outputPath;
        DataContext = new SummaryWindowViewModel(results);

        // Register the BoolToVisibility converter as a resource at runtime
        Resources["BoolToVisibility"] = new BooleanToVisibilityConverter();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_outputPath))
            Process.Start("explorer.exe", _outputPath);
    }
}

// ── ViewModel used only by this window ──────────────────────────────────────

public class SummaryWindowViewModel
{
    public int SucceededCount { get; }
    public int SkippedCount { get; }
    public int FailedCount { get; }
    public List<SummaryRow> Results { get; }

    public SummaryWindowViewModel(List<ProcessingResult> results)
    {
        SucceededCount = results.Count(r => r.Success && !r.IsSkipped);
        SkippedCount = results.Count(r => r.IsSkipped);
        FailedCount = results.Count(r => !r.Success);
        Results = results.Select(r => new SummaryRow(r)).ToList();
    }
}

public class SummaryRow
{
    public ProcessingResult Result { get; }
    public string StatusIcon { get; }
    public bool HasError { get; }
    public string FileCountText { get; }

    public SummaryRow(ProcessingResult result)
    {
        Result = result;
        HasError = !result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage);

        StatusIcon = result.IsSkipped ? "⊘" :
                     result.Success ? "✓" : "✗";

        FileCountText = result.OutputFileCount > 0
            ? $"{result.OutputFileCount} file(s)"
            : string.Empty;
    }
}