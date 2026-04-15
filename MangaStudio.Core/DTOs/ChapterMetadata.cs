namespace MangaStudio.Core.DTOs;

public class ChapterMetadata
{
    public string OriginalPath { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public List<string> ImagePaths { get; init; } = new();
    public int TargetWidth { get; set; }
    public List<int> ImageWidths { get; init; } = new();
    public List<int> ImageHeights { get; init; } = new();

    // Populated by StitchPlanner — each inner list is the image indices for one output chunk.
    // Example: [[0,1,2],[3],[4]]
    public List<List<int>> ChunkPlan { get; set; } = new();
}