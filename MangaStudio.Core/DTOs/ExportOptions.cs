namespace MangaStudio.Core.DTOs;

public enum ExportFormat { WebP, Jpg }

public class ExportOptions
{
    public ExportFormat Format { get; init; } = ExportFormat.WebP;
    public int Quality { get; init; } = 85;
    public int MaxStitchHeight { get; init; } = 10000;
    public bool DeleteOriginals { get; init; } = false;
}