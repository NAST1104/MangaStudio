namespace MangaStudio.Core.DTOs;

public class ImageInfo
{
    public string FilePath { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSizeBytes { get; init; }
}