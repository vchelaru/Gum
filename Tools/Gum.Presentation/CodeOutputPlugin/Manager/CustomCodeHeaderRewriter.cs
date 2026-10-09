using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Rewrites the namespace and partial class declarations in an element's custom code file so they
/// match the element's current identity (name, containing folder, base type) and code settings.
/// Used when an element is renamed or moved, when a code file migration moves its custom code, and
/// when a namespace or inheritance settings change leaves its header stale.
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
        List<string> oldBases = new List<string>();

        if (oldClassHeader.Contains(":"))
        {
            var colonIndex = oldClassHeader.IndexOf(":");
            oldBases = SplitBaseList(oldClassHeader.Substring(colonIndex + 1));
        }

        contents = contents.Remove(startOfLine, endOfLine - startOfLine);

        var newHeader = _customCodeGenerator.GetClassHeader(element, codeOutputProjectSettings);
        // When InheritanceLocation is InCustomCode the generated header already carries the base list,
        // so re-appending the old one would emit "X : New : Old".
        if (!newHeader.Contains(":"))
        {
            // The generated half declares the base class, so the custom half keeps only its own
            // additions (interfaces), or C# sees the base class declared twice.
            string? generatedBase = _customCodeGenerator.GetBaseClass(element, codeOutputProjectSettings);
            if (!string.IsNullOrEmpty(generatedBase))
            {
                oldBases.RemoveAll(oldBase => WithoutGlobal(oldBase) == WithoutGlobal(generatedBase));
            }
            if (oldBases.Count > 0)
            {
                newHeader += " : " + string.Join(", ", oldBases);
            }
        }
        contents = contents.Insert(startOfLine, newHeader);
    }

    // Splits at top-level commas only, so a generic base such as IFoo<A, B> stays one entry.
    private static List<string> SplitBaseList(string baseList)
    {
        List<string> bases = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < baseList.Length; i++)
        {
            char c = baseList[i];
            if (c == '<')
            {
                depth++;
            }
            else if (c == '>')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                bases.Add(baseList.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        bases.Add(baseList.Substring(start).Trim());
        bases.RemoveAll(string.IsNullOrEmpty);
        return bases;
    }

    private static string WithoutGlobal(string typeName) =>
        typeName.StartsWith("global::") ? typeName.Substring("global::".Length) : typeName;
}
