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

        project.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
        project.Screens.Add(screenSave);
        project.SortElementsAndReferencesByName();

        screenSave.Initialize(null);

        return ScreenImportResult.Ok(screenSave);
    }
}
