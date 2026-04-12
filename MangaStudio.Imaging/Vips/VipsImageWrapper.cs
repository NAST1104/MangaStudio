using MangaStudio.Core.Interfaces;
using NetVips;

namespace MangaStudio.Imaging.Vips;

// Internal wrapper that adapts a NetVips.Image to our IImage interface.
// Only code inside MangaStudio.Imaging can see this class.
// External code works only with IImage — it never touches NetVips directly.
internal sealed class VipsImageWrapper : IImage
{
    // The real libvips image handle.
    // In libvips, this is lazy — pixel data is not in RAM until a save or
    // computation forces evaluation. Holding this object is cheap.
    internal Image Inner { get; }

    public int Width => Inner.Width;
    public int Height => Inner.Height;
    public string? SourcePath { get; }

    internal VipsImageWrapper(Image inner, string? sourcePath = null)
    {
        Inner = inner;
        SourcePath = sourcePath;
    }

    public void Dispose() => Inner.Dispose();
}