using System.Text.RegularExpressions;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Decides whether an element's custom code file still contains nothing but the generated stub.
/// </summary>
public interface ICustomCodeStubDetector
{
    /// <summary>
    /// Returns true when <paramref name="customCodeFileContents"/> holds no user-authored code -
    /// only usings, a namespace, a class header and an empty-bodied <c>CustomInitialize</c>.
    /// </summary>
    bool IsUntouchedStub(string customCodeFileContents);
}

/// <summary>
/// Structural implementation of <see cref="ICustomCodeStubDetector"/>: strips comments, usings, the
/// namespace and the class header, then requires what remains to be an empty-bodied
/// <c>CustomInitialize</c> and nothing else. Deliberately not a comparison against
/// <see cref="CustomCodeGenerator"/>'s current output, which changes across Gum versions and would
/// misjudge files written by an older one. Anything it can't account for reads as "not a stub", so
/// the failure direction is an unnecessary prompt rather than recycling user code.
/// </summary>
public class CustomCodeStubDetector : ICustomCodeStubDetector
{
    private static readonly Regex BlockCommentRegex = new(@"/\*.*?\*/", RegexOptions.Singleline);
    private static readonly Regex LineCommentRegex = new(@"//[^\r\n]*");
    private static readonly Regex WhitespaceRegex = new(@"\s+");

    // Anchored at the start of the remaining text so a `using var x = ...;` inside a method body
    // can never be mistaken for a using directive.
    private static readonly Regex UsingDirectiveRegex = new(@"^using\s+[^;{}]*;\s*");
    private static readonly Regex FileScopedNamespaceRegex = new(@"^namespace\s+[\w\.@]+\s*;\s*");
    private static readonly Regex BlockScopedNamespaceRegex = new(@"^namespace\s+[\w\.@]+\s*\{\s*");

    private const string ClassModifiers = @"(?:(?:public|internal|private|protected|sealed|abstract|static|partial)\s+)*";
    private static readonly Regex ClassHeaderRegex = new(
        $@"^{ClassModifiers}class\s+@?\w+\s*(?::\s*[^{{}}]+)?\{{\s*");

    private const string MethodModifiers = @"(?:(?:public|internal|private|protected|partial)\s+)*";
    private static readonly Regex EmptyCustomInitializeRegex = new(
        $@"^{MethodModifiers}void\s+CustomInitialize\s*\(\s*\)\s*\{{\s*\}}\s*$");

    /// <inheritdoc/>
    public bool IsUntouchedStub(string customCodeFileContents)
    {
        string remaining = Normalize(customCodeFileContents);

        // Nothing at all is nothing to lose, so it counts as untouched.
        if (remaining.Length == 0)
        {
            return true;
        }

        remaining = StripRepeatedly(UsingDirectiveRegex, remaining);

        bool hasNamespaceClosingBrace = false;
        if (FileScopedNamespaceRegex.Match(remaining) is { Success: true } fileScoped)
        {
            remaining = remaining.Substring(fileScoped.Length);
        }
        else if (BlockScopedNamespaceRegex.Match(remaining) is { Success: true } blockScoped)
        {
            remaining = remaining.Substring(blockScoped.Length);
            hasNamespaceClosingBrace = true;
        }

        Match classHeader = ClassHeaderRegex.Match(remaining);
        if (!classHeader.Success)
        {
            return false;
        }
        remaining = remaining.Substring(classHeader.Length);

        // The class body's closing brace, plus the namespace's if it was block-scoped.
        string expectedClosing = hasNamespaceClosingBrace ? "} }" : "}";
        if (!remaining.EndsWith(expectedClosing))
        {
            return false;
        }
        remaining = remaining.Substring(0, remaining.Length - expectedClosing.Length).TrimEnd();

        return EmptyCustomInitializeRegex.IsMatch(remaining);
    }

    /// <summary>
    /// Removes comments and collapses every whitespace run to a single space, so the remaining
    /// checks can be written against one predictable spacing rather than the file's formatting.
    /// </summary>
    private static string Normalize(string contents)
    {
        string withoutComments = BlockCommentRegex.Replace(contents, " ");
        withoutComments = LineCommentRegex.Replace(withoutComments, " ");
        return WhitespaceRegex.Replace(withoutComments, " ").Trim();
    }

    private static string StripRepeatedly(Regex regex, string text)
    {
        Match match = regex.Match(text);
        while (match.Success)
        {
            text = text.Substring(match.Length);
            match = regex.Match(text);
        }
        return text;
    }
}
