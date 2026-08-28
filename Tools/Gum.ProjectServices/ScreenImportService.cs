using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ScreenImportService : IScreenImportService
{
    /// <inheritdoc/>
    public ScreenImportResult ImportScreen(GumProjectSave project, ScreenSave screenSave)
    {
        if (ObjectFinder.Self.GetElementSave(screenSave.Name) != null)
        {
            return ScreenImportResult.Conflict(screenSave.Name);
        }

        List<ElementReference> elementReferences = project.ScreenReferences;
        elementReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
        elementReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

        List<ScreenSave> screens = project.Screens;
        screens.Add(screenSave);
        screens.Sort((first, second) => first.Name.CompareTo(second.Name));

        screenSave.Initialize(null);

        return ScreenImportResult.Ok(screenSave);
    }
}
