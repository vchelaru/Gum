using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using System.Collections.Generic;

namespace GumFormsPlugin.Services;

/// <summary>
/// Finds the project's Standards that adding a theme would replace and that the user would miss:
/// ones they changed and something uses.
/// </summary>
public interface IModifiedStandardsFinder
{
    /// <summary>
    /// Names of the project's Standards that <paramref name="theme"/> has a different version of, that
    /// differ from the tool's defaults (or have categories), and that an element in the project uses.
    /// </summary>
    IReadOnlyList<string> FindModifiedInUse(GumProjectSave theme);
}

/// <inheritdoc/>
public class ModifiedStandardsFinder : IModifiedStandardsFinder
{
    private readonly IStandardComparer _standardComparer;

    public ModifiedStandardsFinder(IStandardComparer standardComparer)
    {
        _standardComparer = standardComparer;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> FindModifiedInUse(GumProjectSave theme)
    {
        List<string> names = new List<string>();
        foreach (StandardElementSave themeStandard in theme.StandardElements)
        {
            StandardElementSave? projectStandard = ObjectFinder.Self.GetStandardElement(themeStandard.Name);
            if (projectStandard == null)
            {
                continue;
            }

            // The project already has what the theme would write, so nothing is lost.
            if (!_standardComparer.Compare(themeStandard, projectStandard).HasDifferences)
            {
                continue;
            }

            if (IsModified(projectStandard) && ObjectFinder.Self.GetElementReferencesToThis(projectStandard).Count > 0)
            {
                names.Add(themeStandard.Name);
            }
        }

        return names;
    }

    // Compared with what the tool would create for a new project, which has no categories.
    private bool IsModified(StandardElementSave standard)
    {
        StateSave? defaults = StandardElementsManager.Self.GetDefaultStateFor(standard.Name)?.Clone();
        if (defaults == null)
        {
            return false;
        }

        StandardElementSave reference = new StandardElementSave { Name = standard.Name };
        defaults.ParentContainer = reference;
        reference.States.Add(defaults);
        return _standardComparer.Compare(standard, reference).HasDifferences;
    }
}
