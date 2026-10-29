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

    // The content hash of each file as the tool finished writing it. Present only between
    // RecordWrittenContent and the next IgnoreNextChangeUntil for that file.
    private readonly ConcurrentDictionary<FilePath, string> _writtenContentHashes = new();

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => _timedChangesToIgnore;

    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null)
    {
        time = time ?? DateTime.Now.AddSeconds(5);

        // A save is about to start, so whatever was recorded for the previous one no longer describes the
        // file; until the new write is recorded, every event inside the window is the tool's own.
        _writtenContentHashes.TryRemove(filePath, out _);

        _timedChangesToIgnore.AddOrUpdate(
            filePath,
            time.Value,
            (key, existing) => time.Value > existing ? time.Value : existing);
    }

    public void RecordWrittenContent(FilePath filePath)
    {
        if (TryComputeContentHash(filePath, out string? hash))
        {
            _writtenContentHashes[filePath] = hash!;
        }
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
        if (_writtenContentHashes.TryGetValue(fileName, out string? writtenHash))
        {
            return TryComputeContentHash(fileName, out string? currentHash) && currentHash == writtenHash;
        }

        return true;
    }

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
