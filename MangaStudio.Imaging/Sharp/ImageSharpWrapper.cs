using MangaStudio.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MangaStudio.Imaging.Sharp;

// Internal wrapper that adapts SixLabors.ImageSharp.Image<Rgba32> to IImage.
// Unlike libvips, ImageSharp images are fully decoded in RAM when loaded.
// That is fine — ImageSharp is the debug/fallback backend, not the performance path.
internal sealed class ImageSharpWrapper : IImage
{
    internal Image<Rgba32> Inner { get; }

    public int Width => Inner.Width;
    public int Height => Inner.Height;
    public string? SourcePath { get; }

    internal ImageSharpWrapper(Image<Rgba32> inner, string? sourcePath = null)
    {
        Inner = inner;
        SourcePath = sourcePath;
    }

    public void Dispose() => Inner.Dispose();
}