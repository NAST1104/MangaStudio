using MangaStudio.Core.Interfaces;
using NetVips;

namespace MangaStudio.Imaging.Vips;

// Accumulates a list of VipsImage handles.
// Because libvips is lazy, these handles cost almost no RAM.
// The actual pixel work happens only when FlushCanvas calls Arrayjoin + save.
internal sealed class VipsStitchCanvas : IStitchCanvas
{
    // Each entry is a lazy VipsImage snapshot appended by AppendToCanvas.
    internal List<Image> Layers { get; } = new();

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