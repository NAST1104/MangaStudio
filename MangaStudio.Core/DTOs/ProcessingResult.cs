namespace MangaStudio.Core.DTOs;

public class ProcessingResult
{
    public bool Success { get; init; }
    public bool IsSkipped { get; init; }
    public string ChapterName { get; init; } = string.Empty;
    public int OutputFileCount { get; init; }
    public List<string> Warnings { get; init; } = new();
    public string? ErrorMessage { get; init; }
}