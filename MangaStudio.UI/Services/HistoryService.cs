using System.Text.Json;
using System.IO;
using MangaStudio.UI.Models;

namespace MangaStudio.UI.Services;

public class HistoryService
{
    private static readonly string HistoryPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");

    private readonly List<HistoryEntry> _entries;

    public HistoryService()
    {
        _entries = LoadFromDisk();
    }

    public IReadOnlyList<HistoryEntry> GetAll() =>
        _entries.OrderByDescending(e => e.ProcessedAt).ToList();

    public void AddEntry(HistoryEntry entry)
    {
        _entries.Add(entry);
        SaveToDisk();
    }

    public void Clear()
    {
        _entries.Clear();
        SaveToDisk();
    }

    public void Remove(string id)
    {
        _entries.RemoveAll(e => e.Id == id);
        SaveToDisk();
    }

    private List<HistoryEntry> LoadFromDisk()
    {
        try
        {
            if (File.Exists(HistoryPath))
            {
                var json = File.ReadAllText(HistoryPath);
                return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    private void SaveToDisk()
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(_entries, opts));
        }
        catch { }
    }
}