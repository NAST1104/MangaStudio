using MangaStudio.Core.DTOs;
using ImageInfo = MangaStudio.Core.DTOs.ImageInfo;

namespace MangaStudio.Core.Interfaces;

public interface IImageService
{
    // Read width/height from the file header only — no pixels loaded into RAM.
    ImageInfo ReadInfo(string path);

    // Load an image. The caller owns the returned IImage and must dispose it.
    IImage Load(string path);

    // Resize to targetWidth, preserving aspect ratio.
    // Always returns a new IImage — caller must dispose both input and result.
    IImage Resize(IImage input, int targetWidth);

    // Crop a region out of an image.
    // Returns a new IImage — caller must dispose both input and result.
    IImage Crop(IImage input, int x, int y, int width, int height);

    // Create an empty canvas ready to receive images via AppendToCanvas.
    IStitchCanvas CreateCanvas(int width);

    // Append one image to the bottom of the canvas.
    // The canvas takes an internal copy — you may dispose image after this call.
    // Returns the same canvas (fluent style, no allocation).
    IStitchCanvas AppendToCanvas(IStitchCanvas canvas, IImage image);

    // Write the canvas contents to disk, then reset the canvas to empty.
    // The canvas is reusable immediately after this call.
    void FlushCanvas(IStitchCanvas canvas, string outputPath, ExportOptions options);

    // Save a single IImage to disk.
    void Save(IImage input, string path, ExportOptions options);
}