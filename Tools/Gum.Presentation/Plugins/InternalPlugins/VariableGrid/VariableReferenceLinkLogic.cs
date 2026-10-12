using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using GumRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Turns the right side of a variable reference into a link that selects where the value comes from.
/// </summary>
public interface IVariableReferenceLinkLogic
{
    /// <summary>
    /// Returns a link for <paramref name="rightSide"/>, or null when it is not a plain variable path
    /// (an expression, an owner-relative <c>@Name</c>, a language lookup) or names something that does not exist.
    /// </summary>
    /// <param name="rightSide">The text after the <c>=</c> of a reference row.</param>
    /// <param name="owner">The element that contains the reference; bare names resolve against it.</param>
    DetailTextLink? GetLink(string rightSide, ElementSave owner);

    /// <summary>
    /// Returns an "Unlink" action for the row <paramref name="variableName"/> when a line in
    /// <paramref name="state"/>'s own reference list assigns it, or null. The action comments that line
    /// out (the row keeps its current value and becomes editable) and commits it like a grid edit, so undo works.
    /// </summary>
    /// <param name="variableName">The unqualified variable the row shows, such as <c>X</c> or <c>Red</c>.</param>
    /// <param name="instance">The instance the row belongs to; null on an element-level row.</param>
    /// <param name="owner">The element that contains the reference list.</param>
    /// <param name="state">The state whose own reference list is edited.</param>
    DetailTextLink? GetUnlinkAction(string variableName, InstanceSave? instance, ElementSave owner, StateSave state);
}

/// <inheritdoc/>
public class VariableReferenceLinkLogic : IVariableReferenceLinkLogic
{
    // Identifier segments joined by dots; only the first may carry slashes (an element path).
    private static readonly Regex PathPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_/]*(\.[A-Za-z_][A-Za-z0-9_]*)*$");

    private readonly ISelectedState _selectedState;
    private readonly IObjectFinder _objectFinder;
    private readonly ISetVariableLogic _setVariableLogic;

    public VariableReferenceLinkLogic(ISelectedState selectedState, IObjectFinder objectFinder, ISetVariableLogic setVariableLogic)
    {
        _selectedState = selectedState;
        _objectFinder = objectFinder;
        _setVariableLogic = setVariableLogic;
    }

    /// <inheritdoc/>
    public DetailTextLink? GetUnlinkAction(string variableName, InstanceSave? instance, ElementSave owner, StateSave state)
    {
        string listName = instance == null ? "VariableReferences" : instance.Name + ".VariableReferences";
        VariableListSave<string>? list = state.VariableLists.FirstOrDefault(item => item.Name == listName) as VariableListSave<string>;
        if (list == null)
        {
            return null;
        }

        // A composite line (Color = ...) is stored collapsed but assigns each channel, so expand it
        // against the element that owns the channels before matching.
        ElementSave? lineOwner = instance == null ? owner : _objectFinder.GetElementSave(instance);
        int FindLine() => list.Value.FindIndex(line => AssignsVariable(line, variableName, lineOwner));

        if (FindLine() < 0)
        {
            return null;
        }

        return new DetailTextLink("Unlink", () =>
        {
            int index = FindLine();
            if (index < 0)
            {
                return;
            }

            List<string> before = new List<string>(list.Value);
            list.Value[index] = "//" + list.Value[index];
            _setVariableLogic.PropertyValueChanged("VariableReferences", before, instance, state);
        });
    }

    private static bool AssignsVariable(string line, string variableName, ElementSave? lineOwner)
    {
        if (line.TrimStart().StartsWith("//"))
        {
            return false;
        }

        return ElementSaveExtensions.ExpandCompositeReferenceLine(line, lineOwner).Any(expanded =>
        {
            int equalsIndex = expanded.IndexOf('=');
            return equalsIndex > 0 && expanded.Substring(0, equalsIndex).Trim() == variableName;
        });
    }

    /// <inheritdoc/>
    public DetailTextLink? GetLink(string rightSide, ElementSave owner)
    {
        string path = rightSide.Trim();
        if (!PathPattern.IsMatch(path))
        {
            return null;
        }

        string[] segments = path.Split('.');
        string first = segments[0];

        if (segments.Length == 1)
        {
            // A bare name is a variable on the containing element.
            return first.Contains('/') ? null : CreateLink(path, owner, null);
        }

        InstanceSave? siblingInstance = owner.GetInstance(first);
        if (siblingInstance != null)
        {
            return segments.Length == 2 ? CreateLink(path, owner, siblingInstance) : null;
        }

        ElementSave? element = _objectFinder.GetElementSave(first);
        if (element == null)
        {
            return null;
        }

        if (segments.Length == 2)
        {
            return CreateLink(path, element, null);
        }

        InstanceSave? instance = segments.Length == 3 ? element.GetInstance(segments[1]) : null;
        return instance == null ? null : CreateLink(path, element, instance);
    }

    private DetailTextLink CreateLink(string text, ElementSave element, InstanceSave? instance)
    {
        return new DetailTextLink(text, () =>
        {
            _selectedState.SelectedElement = element;
            if (instance != null)
            {
                _selectedState.SelectedInstance = instance;
            }
        });
    }
}
