using System;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Writes a generated file to disk. The headless service writes through one of these so the tool can
/// keep its retry-on-locked-file behavior while the CLI writes directly.
/// </summary>
public interface ICodeFileWriter
{
    /// <summary>
    /// Creates the file's folder if needed and writes <paramref name="contents"/>, reporting a
    /// file-system failure (an unreachable or read-only output folder) through the implementation's
    /// logger instead of throwing, so one bad path doesn't abort the whole run.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    bool TryWrite(string filePath, string contents);
}

/// <summary>
/// Writes each file once, with no retry.
/// </summary>
public class DirectCodeFileWriter : ICodeFileWriter
{
    private readonly ICodeGenLogger _logger;

    public DirectCodeFileWriter(ICodeGenLogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool TryWrite(string filePath, string contents)
    {
        try
        {
            string? directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            System.IO.File.WriteAllText(filePath, contents);
            return true;
        }
        catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
        {
            _logger.PrintError($"Error writing {filePath}: {e.Message}");
            return false;
        }
    }
}
