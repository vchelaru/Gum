using System;
using System.IO;

namespace Gum.Managers;

/// <summary>
/// Builds user-facing messages for failed file operations. Read-only files, files open in another
/// program, and source-control locks are all ordinary situations for a Gum user, so they get an
/// explanation of what to do rather than a raw exception dump.
/// </summary>
public static class FileOperationFailure
{
    /// <summary>
    /// True when the exception indicates the file could not be accessed rather than a
    /// programming error.
    /// </summary>
    public static bool IsAccessFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;

    /// <summary>
    /// Builds a message explaining a failed file operation. <paramref name="attemptedAction"/> is a
    /// full sentence naming the file, such as "Could not move this file to the recycle bin:\n[path]".
    /// </summary>
    public static string BuildMessage(string attemptedAction, Exception exception)
    {
        string guidance = IsAccessFailure(exception)
            ? "The file may be read-only, open in another program, or locked by source control. " +
              "Make the file writable and try again."
            : "The operation failed unexpectedly.";

        return $"{attemptedAction}\n\n{guidance}\n\nDetails: {exception.Message}";
    }
}
