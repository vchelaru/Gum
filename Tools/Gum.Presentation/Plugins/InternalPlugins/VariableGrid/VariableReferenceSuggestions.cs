using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
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
public readonly record struct VariableReferenceSuggestion(string Text, bool IsContinuation)
{
    /// <summary>What the editor types for this suggestion.</summary>
    public string InsertText => IsContinuation ? Text + "." : Text;
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
        while (wordStart > equalsIndex + 1 && IsWordChar(beforeCaret[wordStart - 1]))
        {
            wordStart--;
        }
        string word = beforeCaret.Substring(wordStart);

        List<VariableReferenceSuggestion> candidates = new List<VariableReferenceSuggestion>();
        AddCandidates(owner, word, candidates);

        List<VariableReferenceSuggestion> accepted = candidates
            .Where(candidate => candidate.Text.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .Where(candidate => candidate.Text.Length != word.Length || !candidate.Text.Equals(word, StringComparison.Ordinal) || candidate.IsContinuation)
            .Distinct()
            .Where(candidate => candidate.IsContinuation || IsAcceptedByChecker(owner, owningInstanceName, candidate.Text))
            .Take(MaxSuggestions)
            .ToList();

        return new VariableReferenceSuggestionResult(wordStart, word.Length, accepted);
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '/' || c == '@';

    /// <summary>
    /// The one test for whether a name is allowed: the check behind GUM0009. A name it reports as
    /// unresolvable is never offered, so the popup and the warning cannot disagree.
    /// </summary>
    private static bool IsAcceptedByChecker(ElementSave owner, string? owningInstanceName, string path) =>
        HeadlessErrorChecker.FindUnresolvableReference(owner, owningInstanceName, "X = " + path) == null;

    private void AddCandidates(ElementSave owner, string word, List<VariableReferenceSuggestion> candidates)
    {
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
                AddMembers(element, elementPath + ".", word.Substring(dotAfterElement + 1), candidates);
            }
            return;
        }

        AddMembers(owner, string.Empty, word, candidates);

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
    private static void AddMembers(ElementSave element, string pathPrefix, string typedMember, List<VariableReferenceSuggestion> candidates)
    {
        int dot = typedMember.IndexOf('.');
        if (dot >= 0)
        {
            InstanceSave? instance = FindInstance(element, typedMember.Substring(0, dot));
            ElementSave? instanceType = instance == null ? null : ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (instance != null && instanceType != null)
            {
                foreach (string name in GetVariableNames(instanceType))
                {
                    candidates.Add(new VariableReferenceSuggestion(pathPrefix + instance.Name + "." + name, false));
                }
            }
            return;
        }

        foreach (string instanceName in ElementSaveExtensions.GetReferencableInstanceNames(element).Distinct())
        {
            candidates.Add(new VariableReferenceSuggestion(pathPrefix + instanceName, true));
        }
        foreach (string name in GetVariableNames(element))
        {
            candidates.Add(new VariableReferenceSuggestion(pathPrefix + name, false));
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
