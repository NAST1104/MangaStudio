using MangaStudio.Core.Interfaces;
using System.Text.RegularExpressions;

namespace MangaStudio.IO;

public class FileSorter : IFileSorter
{
    public IEnumerable<string> Sort(IEnumerable<string> paths)
    {
        return paths.OrderBy(
            p => Path.GetFileNameWithoutExtension(p),
            NaturalStringComparer.Instance);
    }

    // Natural sort comparer: splits strings into text and number chunks.
    // This makes "img2" sort before "img10" instead of after it.
    private sealed class NaturalStringComparer : IComparer<string?>
    {
        public static readonly NaturalStringComparer Instance = new();

        private static readonly Regex ChunkPattern =
            new(@"(\d+)", RegexOptions.Compiled);

        public int Compare(string? x, string? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            // Split each string into alternating text/number segments
            // e.g. "manga_10" → ["manga_", "10", ""]
            var xParts = ChunkPattern.Split(x);
            var yParts = ChunkPattern.Split(y);

            int length = Math.Min(xParts.Length, yParts.Length);
            for (int i = 0; i < length; i++)
            {
                int cmp;

                // If both chunks are pure numbers, compare numerically
                if (int.TryParse(xParts[i], out int xNum) &&
                    int.TryParse(yParts[i], out int yNum))
                    cmp = xNum.CompareTo(yNum);
                else
                    cmp = string.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);

                if (cmp != 0) return cmp;
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}