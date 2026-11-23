using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public class FileWatchIgnoreList : IFileWatchIgnoreList
{
    private readonly ConcurrentDictionary<FilePath, DateTime> _timedChangesToIgnore = new();

    // The tool's latest write of each file: a save in progress (no hash) from IgnoreNextChangeUntil, then
    // the content hash once RecordWrittenContent records it. Each call stores a new object, so a check
    // can tell that a write started or finished while it read the file.
    private readonly ConcurrentDictionary<FilePath, ToolWrite> _toolWrites = new();

    private readonly Func<FilePath, string?> _readContentHash;

    public FileWatchIgnoreList() : this(ComputeContentHash)
    {
    }

    /// <param name="readContentHash">Reads a file's content hash, or null when it cannot be read; tests use it to write in between.</param>
    internal FileWatchIgnoreList(Func<FilePath, string?> readContentHash)
    {
        _readContentHash = readContentHash;
    }

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => _timedChangesToIgnore;

    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null)
    {
        time = time ?? DateTime.Now.AddSeconds(5);

        // A save is about to start, so whatever was recorded for the previous one no longer describes the
        // file; until the new write is recorded, every event inside the window is the tool's own.
        _toolWrites[filePath] = new ToolWrite(null);

        _timedChangesToIgnore.AddOrUpdate(
            filePath,
            time.Value,
            (key, existing) => time.Value > existing ? time.Value : existing);
    }

    public void RecordWrittenContent(FilePath filePath)
    {
        if (TryComputeContentHash(filePath, out string? hash))
        {
            _toolWrites[filePath] = new ToolWrite(hash);
        }
    }

    public void RecordWrittenContent(FilePath filePath, byte[] writtenContent)
    {
        _toolWrites[filePath] = new ToolWrite(Convert.ToHexString(SHA256.HashData(writtenContent)));
    }

    public bool TryGetIgnoreFileChange(FilePath fileName)
    {
        if (!_timedChangesToIgnore.TryGetValue(fileName, out DateTime timeToIgnoreUntil)
            || timeToIgnoreUntil <= DateTime.Now)
        {
            return false;
        }

        // Once the tool's write is recorded, only a file that still holds those bytes is the tool's own
        // doing. Different bytes (or a file that is gone or unreadable) are an outside edit that landed
        // inside the window and must not be dropped.
        if (!_toolWrites.TryGetValue(fileName, out ToolWrite? recorded) || recorded.ContentHash == null)
        {
            return true;
        }

        string? currentHash = _readContentHash(fileName);

        // Saves run on the UI thread while this runs on a watcher thread. A save that started or finished
        // while the file was read (a chip add saves twice in a few milliseconds, #8414) leaves bytes that
        // match no record, and they are still the tool's own.
        if (!_toolWrites.TryGetValue(fileName, out ToolWrite? afterRead) || !ReferenceEquals(afterRead, recorded))
        {
            return true;
        }

        return currentHash != null && currentHash == recorded.ContentHash;
    }

    /// <summary>One write by the tool; <see cref="ContentHash"/> is null while the save is still writing.</summary>
    private sealed class ToolWrite
    {
        public ToolWrite(string? contentHash) => ContentHash = contentHash;

        public string? ContentHash { get; }
    }

    /// <summary>The SHA-256 of the file's bytes, or null when it is missing or unreadable.</summary>
    internal static string? ComputeContentHash(FilePath filePath) =>
        TryComputeContentHash(filePath, out string? hash) ? hash : null;

    private static bool TryComputeContentHash(FilePath filePath, out string? hash)
    {
        try
        {
            using FileStream stream = new FileStream(
                filePath.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            hash = Convert.ToHexString(SHA256.HashData(stream));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            hash = null;
            return false;
        }
    }
}
