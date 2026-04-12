namespace MangaStudio.Core.Interfaces;

// Holds the accumulating state of one output chunk while stitching.
// Width is fixed for the lifetime of the canvas (all images in a chapter share one width).
// CurrentHeight grows as images are appended.
// After FlushCanvas() is called by IImageService, the canvas is reset to empty
// and is ready to accumulate the next chunk — you do not need to create a new one.
public interface IStitchCanvas : IDisposable
{
    int Width { get; }
    int CurrentHeight { get; }
    bool IsEmpty { get; }
}