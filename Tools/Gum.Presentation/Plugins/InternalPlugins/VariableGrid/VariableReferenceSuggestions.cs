using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Expressions;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>One name offered for the right side of a variable reference line.</summary>
/// <param name="Text">The full path to put in the line, such as <c>Slider.Minimum</c>.</param>
/// <param name="IsContinuation">
/// True for an instance or element name, which is only a step toward a variable (<c>Slider</c>
/// before <c>.Minimum</c>). The editor inserts it with a trailing dot and asks again.
/// </param>
/// <param name="Signature">What the list shows instead of <paramref name="Text"/>, such as <c>Clamp(value, min, max)</c>; null shows the text.</param>
/// <param name="Insertion">What the editor types when it is not the text itself, such as <c>Clamp(</c>; null for the default.</param>
/// <param name="IsTypeMismatch">True when the variable's declared type cannot be assigned to the left side; it is listed after the compatible ones.</param>
public readonly record struct VariableReferenceSuggestion(
    string Text, bool IsContinuation, string? Signature = null, string? Insertion = null, bool IsTypeMismatch = false)
{
    /// <summary>What the editor types for this suggestion.</summary>
    public string InsertText => Insertion ?? (IsContinuation ? Text + "." : Text);

    /// <summary>What the list shows for this suggestion.</summary>
    public string DisplayText => Signature ?? Text;
}

/// <summary>The suggestions for one caret position and the text they replace.</summary>
/// <param name="ReplaceStart">Column where the word being typed starts.</param>
/// <param name="ReplaceLength">Length of the word being typed, up to the caret.</param>
/// <param name="Items">Ordered suggestions; empty when there is nothing to offer.</param>
public readonly record struct VariableReferenceSuggestionResult(
    int ReplaceStart, int ReplaceLength, IReadOnlyList<VariableReferenceSuggestion> Items)
{
    /// <summary>Nothing to offer.</summary>
    public static VariableReferenceSuggestionResult None { get; } =
        new VariableReferenceSuggestionResult(0, 0, Array.Empty<VariableReferenceSuggestion>());
}

/// <summary>Suggests the names that can follow the <c>=</c> of a <c>VariableReferences</c> line.</summary>
public interface IVariableReferenceSuggestions
{
    /// <summary>
    /// Suggestions for the word ending at <paramref name="caretColumn"/> in <paramref name="lineText"/>.
    /// </summary>
    /// <param name="owner">The element that contains the line; bare names resolve against it.</param>
    /// <param name="owningInstanceName">The instance whose row holds the line, or null on an element row.</param>
    /// <param name="lineText">The text of the one line being edited.</param>
    /// <param name="caretColumn">The caret's index within <paramref name="lineText"/>.</param>
    VariableReferenceSuggestionResult GetSuggestions(ElementSave owner, string? owningInstanceName, string lineText, int caretColumn);
}

/// <inheritdoc/>
public class VariableReferenceSuggestions : IVariableReferenceSuggestions
{
    private const int MaxSuggestions = 100;

    /// <summary>The reserved identifier that reads the current language index.</summary>
    public const string LocalizationName = "global::Localization.CurrentLanguage";

    // Parameter names shown in the list. The evaluator's table (ExpressionFunctions) owns which functions exist
    // and how many arguments each takes; a test fails when it gains one this table lacks.
    private static readonly Dictionary<string, string[]> FunctionParameters = new Dictionary<string, string[]>
    {
        ["Sin"] = new[] { "degrees" },
        ["Cos"] = new[] { "degrees" },
        ["Tan"] = new[] { "degrees" },
        ["Sqrt"] = new[] { "value" },
        ["Abs"] = new[] { "value" },
        ["Floor"] = new[] { "value" },
        ["Ceiling"] = new[] { "value" },
        ["Round"] = new[] { "value" },
        ["Min"] = new[] { "a", "b" },
        ["Max"] = new[] { "a", "b" },
        ["Clamp"] = new[] { "value", "min", "max" },
    };

    /// <summary>The function signature shown in the list, such as <c>Clamp(value, min, max)</c>.</summary>
    public static string GetFunctionSignature(string name) =>
        name + "(" + string.Join(", ", FunctionParameters.TryGetValue(name, out string[]? parameters)
            ? parameters
            : Enumerable.Range(1, ExpressionFunctions.GetArgumentCount(name)).Select(i => "arg" + i)) + ")";

    // Prefixes the checker's qualified-path pattern accepts, with the project list each reads.
    private static readonly string[] QualifiedPrefixes = { "Components/", "Screens/", "Standards/" };

    /// <inheritdoc/>
    public VariableReferenceSuggestionResult GetSuggestions(ElementSave owner, string? owningInstanceName, string lineText, int caretColumn)
    {
        caretColumn = Math.Clamp(caretColumn, 0, lineText.Length);
        string beforeCaret = lineText.Substring(0, caretColumn);

        if (lineText.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return VariableReferenceSuggestionResult.None;
        }

        // Only the right side is offered; a line with no "=" yet is still the left side.
        int equalsIndex = beforeCaret.IndexOf('=');
        if (equalsIndex < 0 || beforeCaret.Count(c => c == '"') % 2 == 1)
        {
            return VariableReferenceSuggestionResult.None;
        }

        int wordStart = caretColumn;
        while (wordStart > equalsIndex + 1 && IsWordChar(beforeCaret, wordStart - 1))
        {
            wordStart--;
        }
        string word = beforeCaret.Substring(wordStart);

        string leftType = GetLeftSideType(owner, owningInstanceName, beforeCaret.Substring(0, equalsIndex).Trim());
        List<VariableReferenceSuggestion> candidates = new List<VariableReferenceSuggestion>();
        AddCandidates(owner, owningInstanceName, word, leftType, candidates);

        List<VariableReferenceSuggestion> accepted = candidates
            .Where(candidate => candidate.Text.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .Where(candidate => candidate.Text.Length != word.Length || !candidate.Text.Equals(word, StringComparison.Ordinal) || candidate.IsContinuation)
            .Distinct()
            .Where(candidate => candidate.IsContinuation || IsAcceptedByChecker(owner, owningInstanceName, candidate.Text))
            .OrderBy(candidate => candidate.IsTypeMismatch ? 1 : 0)
            .Take(MaxSuggestions)
            .ToList();

        // Functions and the localization name are not variables, so the GUM0009 check does not apply to them.
        // Function names are case sensitive, as the evaluator's are.
        if (word.IndexOfAny(new[] { '.', '/', '@', ':' }) < 0)
        {
            foreach (string functionName in ExpressionFunctions.Names.Where(name => name.StartsWith(word, StringComparison.Ordinal)))
            {
                accepted.Add(new VariableReferenceSuggestion(functionName, false, GetFunctionSignature(functionName), functionName + "("));
            }
        }
        if (word.Length > 0 &&
            (LocalizationName.StartsWith(word, StringComparison.OrdinalIgnoreCase) ||
             LocalizationName.Substring("global::".Length).StartsWith(word, StringComparison.OrdinalIgnoreCase)))
        {
            accepted.Add(new VariableReferenceSuggestion(LocalizationName, false));
        }

        return new VariableReferenceSuggestionResult(wordStart, word.Length, accepted);
    }

    // A colon belongs to the word only as half of "::" (global::Localization...), not as a ternary's colon.
    private static bool IsWordChar(string text, int index)
    {
        char c = text[index];
        return char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '/' || c == '@' ||
            (c == ':' && ((index > 0 && text[index - 1] == ':') || (index + 1 < text.Length && text[index + 1] == ':')));
    }

    /// <summary>
    /// The one test for whether a name is allowed: the check behind GUM0009. A name it reports as
    /// unresolvable is never offered, so the popup and the warning cannot disagree.
    /// </summary>
    private static bool IsAcceptedByChecker(ElementSave owner, string? owningInstanceName, string path) =>
        HeadlessErrorChecker.FindUnresolvableReference(owner, owningInstanceName, "X = " + path) == null;

    private static string GetLeftSideType(ElementSave owner, string? owningInstanceName, string leftName)
    {
        if (leftName.Length == 0 || leftName.Contains(' '))
        {
            return string.Empty;
        }
        InstanceSave? instance = owningInstanceName == null ? null : FindInstance(owner, owningInstanceName);
        ElementSave? element = instance == null ? owner : ObjectFinder.Self.GetElementSave(instance.BaseType);
        return element == null ? string.Empty : ObjectFinder.Self.GetRootVariable(leftName, element)?.Type ?? string.Empty;
    }

    private static readonly HashSet<string> NumericTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "float", "int", "double", "decimal", "long", "short", "byte",
    };

    /// <summary>Whether a value declared as <paramref name="rightType"/> can be assigned to <paramref name="leftType"/>; unknown types count as compatible.</summary>
    private static bool IsAssignable(string leftType, string? rightType)
    {
        if (leftType.Length == 0 || string.IsNullOrEmpty(rightType) || leftType == "string" || leftType == rightType)
        {
            return true;
        }
        return NumericTypes.Contains(leftType) && NumericTypes.Contains(rightType);
    }

    private static string? GetDeclaredType(ElementSave element, string name)
    {
        if (name == "Index")
        {
            return "int";
        }
        if (name.StartsWith("Absolute", StringComparison.Ordinal))
        {
            return "float";
        }
        return ObjectFinder.Self.GetRootVariable(name, element)?.Type;
    }

    private void AddCandidates(ElementSave owner, string? owningInstanceName, string word, string leftType, List<VariableReferenceSuggestion> candidates)
    {
        // "@" names the instance that owns the row; only an instance row has one.
        if (word.StartsWith('@'))
        {
            InstanceSave? ownerInstance = owningInstanceName == null ? null : FindInstance(owner, owningInstanceName);
            ElementSave? ownerType = ownerInstance == null ? null : ObjectFinder.Self.GetElementSave(ownerInstance.BaseType);
            if (ownerType != null)
            {
                AddMembers(ownerType, "@", word.Substring(1), word, leftType, candidates);
            }
            return;
        }

        // An element path: Components/Foo, then Components/Foo.Member or Components/Foo.Instance.Member.
        string? qualifiedPrefix = QualifiedPrefixes.FirstOrDefault(prefix => word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (qualifiedPrefix != null)
        {
            int dotAfterElement = word.IndexOf('.');
            if (dotAfterElement < 0)
            {
                AddElementPaths(candidates);
                return;
            }
            string elementPath = word.Substring(0, dotAfterElement);
            ElementSave? element = FindQualifiedElement(elementPath);
            if (element != null)
            {
                AddMembers(element, elementPath + ".", word.Substring(dotAfterElement + 1), word, leftType, candidates);
            }
            return;
        }

        AddMembers(owner, string.Empty, word, word, leftType, candidates);

        // Element paths are offered once the word could still become one.
        if (word.Contains('/') || QualifiedPrefixes.Any(prefix => prefix.StartsWith(word, StringComparison.OrdinalIgnoreCase)) && word.Length > 0)
        {
            AddElementPaths(candidates);
        }
    }

    /// <summary>
    /// Adds instance names, the element's own variables and, for a typed <c>Instance.</c> prefix, that
    /// instance's variables, each with <paramref name="pathPrefix"/> in front.
    /// </summary>
    private static void AddMembers(ElementSave element, string pathPrefix, string typedMember, string word, string leftType, List<VariableReferenceSuggestion> candidates)
    {
        // Each typed "Instance." steps into that instance's component, so nested instances
        // (Slider.Thumb.Width) are offered the same way as top-level ones.
        int dot;
        while ((dot = typedMember.IndexOf('.')) >= 0)
        {
            InstanceSave? instance = FindInstance(element, typedMember.Substring(0, dot));
            ElementSave? instanceType = instance == null ? null : ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (instance == null || instanceType == null)
            {
                return;
            }
            pathPrefix += instance.Name + ".";
            element = instanceType;
            typedMember = typedMember.Substring(dot + 1);
        }

        foreach (string instanceName in ElementSaveExtensions.GetReferencableInstanceNames(element).Distinct())
        {
            candidates.Add(new VariableReferenceSuggestion(pathPrefix + instanceName, true));
        }
        foreach (string name in GetVariableNames(element))
        {
            string path = pathPrefix + name;
            // The type is only looked up for names that pass the typed prefix; the rest are filtered out anyway.
            bool mismatch = path.StartsWith(word, StringComparison.OrdinalIgnoreCase) && !IsAssignable(leftType, GetDeclaredType(element, name));
            candidates.Add(new VariableReferenceSuggestion(path, false, IsTypeMismatch: mismatch));
        }
    }

    private static void AddElementPaths(List<VariableReferenceSuggestion> candidates)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return;
        }
        foreach (ElementSave element in project.Components.Cast<ElementSave>().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(new VariableReferenceSuggestion("Components/" + element.Name, true));
        }
        foreach (ElementSave element in project.Screens.Cast<ElementSave>().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(new VariableReferenceSuggestion("Screens/" + element.Name, true));
        }
        foreach (ElementSave element in project.StandardElements.Cast<ElementSave>().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(new VariableReferenceSuggestion("Standards/" + element.Name, true));
        }
    }

    private static ElementSave? FindQualifiedElement(string qualifiedPath)
    {
        string elementName = qualifiedPath.Substring(qualifiedPath.IndexOf('/') + 1);
        return elementName.Length == 0 ? null : ObjectFinder.Self.GetElementSave(elementName);
    }

    private static InstanceSave? FindInstance(ElementSave element, string name)
    {
        foreach (ElementSave inChain in ObjectFinder.Self.GetBaseElements(element).Append(element).Reverse())
        {
            InstanceSave? instance = inChain.Instances.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (instance != null)
            {
                return instance;
            }
        }
        return null;
    }

    /// <summary>
    /// Every unqualified variable name that might be read from <paramref name="element"/>: stored and
    /// default variables of it and its bases, exposed names, category selectors, Forms properties, and
    /// the layout-computed names. The checker then decides which of them resolve.
    /// </summary>
    private static IEnumerable<string> GetVariableNames(ElementSave element)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        List<string> ordered = new List<string>();
        void Add(string? name)
        {
            if (!string.IsNullOrEmpty(name) && !name.Contains('.') && names.Add(name))
            {
                ordered.Add(name);
            }
        }

        foreach (ElementSave inChain in ObjectFinder.Self.GetBaseElements(element).Prepend(element))
        {
            foreach (StateSave state in inChain.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    Add(variable.Name);
                    Add(variable.ExposedAsName);
                }
            }
            foreach (StateSaveCategory category in inChain.Categories)
            {
                Add(category.Name + "State");
            }
            if (inChain is StandardElementSave)
            {
                foreach (VariableSave variable in StandardElementsManager.Self.TryGetDefaultStateFor(inChain.Name, throwExceptionOnMissing: false)?.Variables ?? new List<VariableSave>())
                {
                    Add(variable.Name);
                }
            }
        }
        foreach (VariableSave declaration in FormsBehaviorChain.GetFormsPropertyDeclarations(element))
        {
            Add(declaration.Name);
        }
        Add("State");
        foreach (string name in new[] { "AbsoluteX", "AbsoluteY", "AbsoluteLeft", "AbsoluteTop", "AbsoluteRight", "AbsoluteBottom", "AbsoluteWidth", "AbsoluteHeight", "Index" })
        {
            Add(name);
        }
        // A composite color such as "FillColor" stands for its channels.
        foreach (string name in ordered.ToList())
        {
            if (name.EndsWith("Red", StringComparison.Ordinal) && names.Contains(name.Substring(0, name.Length - 3) + "Green"))
            {
                Add(name.Substring(0, name.Length - 3) + "Color");
            }
        }
        return ordered.OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }
}
