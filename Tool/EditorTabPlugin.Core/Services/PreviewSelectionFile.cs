using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Reads and writes the selection file that carries a <see cref="PreviewSelectionMessage"/> from
/// the tool to a running GumPreview. The two processes touch the file concurrently (the tool
/// writes on every selection change, GumPreview polls it), so both sides open it sharing every
/// access, and the writer retries a sharing violation instead of throwing it into the tool's
/// plugin event. A reader that catches the file mid-write sees a prefix with no end marker
/// (see <see cref="PreviewSelectionMessage.TryParse"/>) and simply polls again. Compiled into
/// both projects, like the message itself.
/// </summary>
public static class PreviewSelectionFile
{
    private const int WriteAttempts = 5;
    private const int RetryDelayMilliseconds = 20;

    /// <summary>
    /// Writes <paramref name="content"/> over the file. Returns false when the file stayed locked
    /// through every retry.
    /// </summary>
    public static bool TryWrite(string path, string content)
    {
        for (int attempt = 0; attempt < WriteAttempts; attempt++)
        {
            try
            {
                using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using StreamWriter writer = new StreamWriter(stream);
                writer.Write(content);
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
        return false;
    }

    /// <summary>
    /// Reads and parses the file; null when it's missing, locked, or mid-write (no element line yet).
    /// </summary>
    public static PreviewSelectionMessage? TryRead(string path)
    {
        try
        {
            using FileStream stream = OpenForReading(path);
            using StreamReader reader = new StreamReader(stream);
            List<string> lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
            return PreviewSelectionMessage.TryParse(lines);
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Opens the file sharing every access, so the tool can replace it while it's open here.
    /// </summary>
    public static FileStream OpenForReading(string path) =>
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
}
