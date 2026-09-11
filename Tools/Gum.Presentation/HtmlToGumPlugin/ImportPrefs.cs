using System;
using System.IO;
using System.Text.Json;

namespace HtmlToGumPlugin;

/// <summary>The last import's options, kept per user so the dialog opens with them next time.</summary>
public sealed class ImportPrefs
{
    public string LastSource { get; set; } = "";
    public bool LastIsUrl { get; set; }
    public string Selector { get; set; } = "body";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public bool NoResponsive { get; set; }
    public string DestinationSubfolder { get; set; } = "";

    private static string PrefsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HtmlToGumPlugin", "import-prefs.json");

    public static ImportPrefs Load()
    {
        try
        {
            string path = PrefsPath;
            if (!File.Exists(path))
            {
                return new ImportPrefs();
            }
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ImportPrefs>(json) ?? new ImportPrefs();
        }
        catch
        {
            return new ImportPrefs();
        }
    }

    public void Save()
    {
        try
        {
            string path = PrefsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // prefs are best-effort
        }
    }
}
