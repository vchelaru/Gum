using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using System;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Writes generated files through <see cref="IRetryService"/>, so a file an editor or antivirus
/// scanner briefly holds open is retried instead of failing the generation.
/// </summary>
public class RetryingCodeFileWriter : ICodeFileWriter
{
    private readonly IRetryService _retryService;
    private readonly ICodeGenLogger _logger;

    public RetryingCodeFileWriter(IRetryService retryService, ICodeGenLogger logger)
    {
        _retryService = retryService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool TryWrite(string filePath, string contents)
    {
        try
        {
            _retryService.TryMultipleTimes(() =>
            {
                string? directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                System.IO.File.WriteAllText(filePath, contents);
            });
            return true;
        }
        catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
        {
            _logger.PrintError($"Error writing {filePath}: {e.Message}");
            return false;
        }
    }
}
