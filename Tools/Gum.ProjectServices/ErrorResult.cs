namespace Gum.ProjectServices;

/// <summary>
/// Represents a single error found during project error checking.
/// This is a plain data object with no UI framework dependencies.
/// </summary>
public class ErrorResult
{
    public string ElementName { get; set; }
    public string Message { get; set; }
    public ErrorSeverity Severity { get; set; }

    /// <summary>
    /// Stable error code for this result (e.g. <c>"GUM0001"</c>). When set, a tool
    /// can display the code alongside the message and resolve a help URL via
    /// <see cref="IErrorDocsRegistry"/>. Null for errors that have not yet been
    /// assigned a code.
    /// </summary>
    public string? Code { get; set; }

    public ErrorResult()
    {
        ElementName = string.Empty;
        Message = string.Empty;
        Severity = ErrorSeverity.Error;
    }
}

/// <summary>
/// Severity level for an error result.
/// </summary>
public enum ErrorSeverity
{
    Warning,
    Error
}
