using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MangaStudio.Tests.Helpers;

// Creates real image files on disk for use in tests.
// All images are tiny so tests run fast.
public static class TestImageFactory
{
    // Creates a solid-colour PNG and returns the full path.
    // width/height default to 100x200 — small but realistic proportions.
    public static string CreatePng(
        string directory,
        string fileName,
        int width  = 100,
        int height = 200,
        string hexColor = "#4a90d9")  // a mid-blue
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);

        var color = Color.ParseHex(hexColor);
        using var image = new Image<Rgba32>(width, height, color);
        image.SaveAsPng(path);

        return path;
    }

    // Creates several PNG files with the same dimensions and returns their paths.
    public static List<string> CreatePngs(
        string directory,
        int count,
        int width  = 100,
        int height = 200)
    {
        var paths = new List<string>();
        for (int i = 1; i <= count; i++)
            paths.Add(CreatePng(directory, $"{i:D3}.png", width, height));
        return paths;
    }
}