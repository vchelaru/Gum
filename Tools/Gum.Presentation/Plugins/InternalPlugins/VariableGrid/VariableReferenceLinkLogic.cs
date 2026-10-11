using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
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
}

/// <inheritdoc/>
public class VariableReferenceLinkLogic : IVariableReferenceLinkLogic
{
    // Identifier segments joined by dots; only the first may carry slashes (an element path).
    private static readonly Regex PathPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_/]*(\.[A-Za-z_][A-Za-z0-9_]*)*$");

    private readonly ISelectedState _selectedState;
    private readonly IObjectFinder _objectFinder;

    public VariableReferenceLinkLogic(ISelectedState selectedState, IObjectFinder objectFinder)
    {
        _selectedState = selectedState;
        _objectFinder = objectFinder;
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
