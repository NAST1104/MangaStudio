using MangaStudio.Core.DTOs;
using MangaStudio.Core.Enums;

namespace MangaStudio.UI.Models;

public class AppSettings
{
    public ImagingBackend Backend { get; set; } = ImagingBackend.Vips;
    public int MaxStitchHeight { get; set; } = 10000;
    public bool DeleteOriginals { get; set; } = false;
    public int DefaultQuality { get; set; } = 85;
    public string DefaultOutputPath { get; set; } = string.Empty;
    public ExportFormat OutputFormat { get; set; } = ExportFormat.WebP;
    public DuplicateAction DuplicateAction { get; set; } = DuplicateAction.Skip;
}