using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using System.Text.RegularExpressions;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Rewrites the namespace and partial class declarations in an element's custom code file so they
/// match the element's current identity (name, containing folder, base type) and code settings.
/// Used when an element is renamed or moved, and when a code file migration moves its custom code.
/// </summary>
public class CustomCodeHeaderRewriter
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;

    public CustomCodeHeaderRewriter(CodeGenerator codeGenerator, CustomCodeGenerator customCodeGenerator)
    {
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
    }

    /// <returns>The updated file contents.</returns>
    public string Rewrite(string contents, ElementSave element,
        CodeOutputElementSettings? elementSettings, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        RenameNamespaceInCode(element, elementSettings, codeOutputProjectSettings, ref contents);
        RenameClassInCode(element, codeOutputProjectSettings, ref contents);
        return contents;
    }

    private void RenameNamespaceInCode(ElementSave element, CodeOutputElementSettings? elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, ref string contents)
    {
        var newNamespace = _codeGenerator.GetElementNamespace(element, elementSettings, codeOutputProjectSettings);

        ////////////////Early Out/////////////////
        // Generation would emit no namespace at all, so there is nothing to rename the existing
        // (presumably hand-written) namespace to.
        if (string.IsNullOrEmpty(newNamespace))
        {
            return;
        }

        // Matches both block-scoped ("namespace Foo") and file-scoped ("namespace Foo;") declarations.
        var match = Regex.Match(contents,
            @"^[ \t]*namespace[ \t]+(?<name>[^\s;{]+)",
            RegexOptions.Multiline);

        if (!match.Success)
        {
            return;
        }
        //////////////End Early Out///////////////

        var nameGroup = match.Groups["name"];
        contents = contents.Remove(nameGroup.Index, nameGroup.Length);
        contents = contents.Insert(nameGroup.Index, newNamespace);
    }

    private void RenameClassInCode(ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings, ref string contents)
    {
        var startOfLine = contents.IndexOf("partial class ");
        ////////////////Early Out/////////////////
        if (startOfLine <= -1)
        {
            return;
        }
        //////////////End Early Out///////////////

        var endOfLine = contents.IndexOf("\n", startOfLine + 1);
        if (endOfLine > startOfLine && contents[endOfLine - 1] == '\r')
        {
            endOfLine--;
        }

        var oldClassHeader = contents.Substring(startOfLine, endOfLine - startOfLine);
        string suffix = string.Empty;

        if (oldClassHeader.Contains(":"))
        {
            var colonIndex = oldClassHeader.IndexOf(":");
            suffix = " " + oldClassHeader.Substring(colonIndex).Trim();
        }

        contents = contents.Remove(startOfLine, endOfLine - startOfLine);

        var newHeader = _customCodeGenerator.GetClassHeader(element, codeOutputProjectSettings);
        // When InheritanceLocation is InCustomCode the generated header already carries the base list,
        // so re-appending the old one would emit "X : New : Old".
        if (!newHeader.Contains(":"))
        {
            newHeader += suffix;
        }
        contents = contents.Insert(startOfLine, newHeader);
    }
}
