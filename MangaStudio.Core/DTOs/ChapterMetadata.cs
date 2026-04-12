namespace MangaStudio.Core.DTOs;

public class ChapterMetadata
{
    public string OriginalPath { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public List<string> ImagePaths { get; init; } = new();
    public int TargetWidth { get; set; }
    public List<int> ImageWidths { get; init; } = new();
    public List<int> ImageHeights { get; init; } = new();
}