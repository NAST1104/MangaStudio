using MangaStudio.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MangaStudio.Imaging.Sharp;

// Accumulates fully decoded ImageSharp images in memory.
// Because ImageSharp keeps pixels in RAM, this canvas uses more memory than the
// VipsStitchCanvas. Keep MaxStitchHeight reasonable (≤ 5000px) when using
// the ImageSharp backend.
internal sealed class ImageSharpStitchCanvas : IStitchCanvas
{
    internal List<Image<Rgba32>> Layers { get; } = new();

    public int Width { get; internal set; }
    public int CurrentHeight { get; internal set; }
    public bool IsEmpty => Layers.Count == 0;

    public void Dispose()
    {
        foreach (var img in Layers)
            img.Dispose();
        Layers.Clear();
    }
}