using System.Text.Json;
using System.IO;
using MangaStudio.UI.Models;

namespace MangaStudio.UI.Services;

public class SettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* return defaults on any error */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, opts));
        }
        catch { /* best-effort */ }
    }
}