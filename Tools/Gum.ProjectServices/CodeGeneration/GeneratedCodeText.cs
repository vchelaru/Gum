namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// The text form of a generated file. Generators build text with the OS line ending, so the same project
/// would otherwise produce CRLF on Windows and LF (plus a hard-coded CRLF header line) elsewhere.
/// </summary>
public static class GeneratedCodeText
{
    /// <summary>
    /// <paramref name="text"/> with every line ending as CRLF, whatever mix it came in, which is what
    /// generation has always written on Windows.
    /// </summary>
    public static string ToCrLf(string text) => text.Replace("\r\n", "\n").Replace("\n", "\r\n");
}
