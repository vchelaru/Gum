using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Gum.Diagnostics;

/// <summary>
/// Writes the diagnostics zip. The tool's own text (system info, errors, plugins, logs, settings) goes
/// through the <see cref="PathScrubber"/>; the user's typed description and project files are copied as they are.
/// </summary>
public static class DiagnosticsZipWriter
{
    /// <summary>Folders never copied from the C# project: build output and IDE or tool caches.</summary>
    public static readonly string[] SkippedFolders = { "bin", "obj", ".git", ".vs", ".idea", "node_modules" };

    private static readonly string[] GumProjectExtensions = { ".gumx", ".gusx", ".gucx", ".gutx", ".behx", ".ganx" };

    /// <summary>Creates a new zip at <paramref name="zipPath"/>; fails if the file already exists.</summary>
    public static void Write(
        string zipPath,
        DiagnosticsRequest request,
        DiagnosticsSystemInfo info,
        DiagnosticsContent content,
        PathScrubber scrubber)
    {
        using FileStream stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write);
        using ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create);

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            AddText(zip, "description.txt", request.Description);
        }

        AddText(zip, "system-info.txt", scrubber.Scrub(FormatSystemInfo(info)));
        AddText(zip, "errors.txt", scrubber.Scrub(content.ErrorsText));
        AddText(zip, "plugins.txt", scrubber.Scrub(content.PluginsText));

        foreach (string log in content.LogFiles)
        {
            AddScrubbedFile(zip, "logs/" + Path.GetFileName(log), log, scrubber);
        }
        foreach (string settings in content.SettingsFiles)
        {
            AddScrubbedFile(zip, "settings/" + Path.GetFileName(settings), settings, scrubber);
        }

        if (request.IncludeGumProject && content.GumProjectFile != null)
        {
            string? folder = Path.GetDirectoryName(content.GumProjectFile);
            if (folder != null)
            {
                AddFolder(zip, "gum-project", folder, extensions: GumProjectExtensions);
            }
        }

        if (request.IncludeCSharpProject && content.CSharpProjectFolder != null)
        {
            AddFolder(zip, "csharp-project", content.CSharpProjectFolder, extensions: null);
        }
    }

    /// <summary>The text of <c>system-info.txt</c>.</summary>
    public static string FormatSystemInfo(DiagnosticsSystemInfo info)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("Gum version: " + info.GumVersion);
        text.AppendLine("Operating system: " + info.OperatingSystem);
        text.AppendLine(".NET runtime: " + info.DotNetRuntime);
        text.AppendLine("CPU architecture: " + info.CpuArchitecture);
        text.AppendLine("Display scale: " + info.DisplayScale);
        text.AppendLine("Graphics card: " + info.GraphicsCard);
        return text.ToString();
    }

    private static void AddText(ZipArchive zip, string entryName, string text)
    {
        ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static void AddScrubbedFile(ZipArchive zip, string entryName, string path, PathScrubber scrubber)
    {
        string text;
        try
        {
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new StreamReader(stream);
            text = reader.ReadToEnd();
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        AddText(zip, entryName, scrubber.Scrub(text));
    }

    private static void AddFolder(ZipArchive zip, string entryRoot, string folder, string[]? extensions)
    {
        foreach (string file in EnumerateFiles(folder))
        {
            if (extensions != null && Array.IndexOf(extensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
            {
                continue;
            }

            string relative = Path.GetRelativePath(folder, file).Replace('\\', '/');
            try
            {
                using FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                ZipArchiveEntry entry = zip.CreateEntry(entryRoot + "/" + relative, CompressionLevel.Optimal);
                using Stream destination = entry.Open();
                source.CopyTo(destination);
            }
            catch (IOException)
            {
                // A file another program holds exclusively is left out; the rest still goes in.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string folder)
    {
        foreach (string file in Directory.GetFiles(folder))
        {
            yield return file;
        }
        foreach (string subFolder in Directory.GetDirectories(folder))
        {
            string name = Path.GetFileName(subFolder);
            if (Array.Exists(SkippedFolders, skipped => string.Equals(skipped, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            foreach (string file in EnumerateFiles(subFolder))
            {
                yield return file;
            }
        }
    }
}
