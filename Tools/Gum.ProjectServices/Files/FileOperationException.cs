using System;

namespace Gum.Managers;

/// <summary>
/// Thrown when a file operation fails for a reason the user can act on. Callers show
/// <see cref="Exception.Message"/> directly - it is already written for the user, so it must not be
/// wrapped in a stack trace dump. Build the message with <see cref="FileOperationFailure"/>.
/// </summary>
public class FileOperationException : Exception
{
    public FileOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
