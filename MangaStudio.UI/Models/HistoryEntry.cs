using MangaStudio.Core.DTOs;

namespace MangaStudio.UI.Models;

public class HistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime ProcessedAt { get; init; }
    public string MangaName { get; init; } = string.Empty;
    public string MangaRootPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int TotalChapters { get; init; }
    public int Succeeded { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public ExportFormat Format { get; init; }
    public int Quality { get; init; }
    public List<string> FailedChapterNames { get; init; } = new();
}