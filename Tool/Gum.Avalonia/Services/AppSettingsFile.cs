using System;
using System.IO;
using System.Text.Json;

namespace Gum.Avalonia.Services;

/// <summary>
/// Keeps the per-user <c>appsettings.json</c> loadable. The configuration host refuses to start on a
/// file it cannot parse, so a truncated or hand-damaged file would otherwise stop the tool on every
/// launch until the user found and deleted it.
/// </summary>
public static class AppSettingsFile
{
    /// <summary>
    /// Creates <paramref name="path"/> as an empty object if it is missing. If it is not a JSON
    /// object, moves it aside (the returned path) and starts over with an empty one, so the tool
    /// starts with default settings and the damaged file is kept for the user.
    /// </summary>
    /// <returns>Where the unreadable file was moved, or null if nothing had to be moved.</returns>
    public static string? EnsureLoadable(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{}");
            return null;
        }

        if (IsJsonObject(File.ReadAllText(path)))
        {
            return null;
        }

        string backupPath = path + ".unreadable";
        File.Copy(path, backupPath, overwrite: true);
        File.WriteAllText(path, "{}");
        return backupPath;
    }

    private static bool IsJsonObject(string text)
    {
        try
        {
            // The same leniency the JSON configuration provider parses with.
            using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
