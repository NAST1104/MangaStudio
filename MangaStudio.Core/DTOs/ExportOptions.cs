namespace MangaStudio.Core.DTOs;

public enum ExportFormat { WebP, Jpg }

public class ExportOptions
{
    // WebP hard limit enforced by the encoder
    public const int WebPMaxHeight = 15000;

    // JPEG has no practical limit for manga use
    public const int JpgMaxHeight = 100000;

    public ExportFormat Format { get; init; } = ExportFormat.WebP;
    public int Quality { get; init; } = 85;
    public bool DeleteOriginals { get; init; } = false;

    private int _maxStitchHeight = 10000;
    public int MaxStitchHeight
    {
        get => _maxStitchHeight;
        init => _maxStitchHeight = Clamp(value, Format);
    }

    public int SafeMaxHeight =>
        Format == ExportFormat.WebP ? WebPMaxHeight : JpgMaxHeight;

    public static int Clamp(int requested, ExportFormat format)
    {
        int limit = format == ExportFormat.WebP ? WebPMaxHeight : JpgMaxHeight;
        return Math.Min(requested, limit);
    }
}