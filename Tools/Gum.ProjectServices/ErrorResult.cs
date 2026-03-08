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
