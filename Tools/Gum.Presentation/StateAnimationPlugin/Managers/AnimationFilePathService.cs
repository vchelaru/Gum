using Gum.StateAnimation.SaveClasses;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

public class AnimationFilePathService : IAnimationFilePathService
{
    private readonly ISelectedState _selectedState;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectManager _projectManager;

    public AnimationFilePathService(ISelectedState selectedState, IFileCommands fileCommands, IProjectManager projectManager)
    {
        _selectedState = selectedState;
        _fileCommands = fileCommands;
        _projectManager = projectManager;
    }

    /// <summary>
    /// "Animations.ganx" or "Animations.ganj" depending on the currently-open project's own format
    /// (issue #4182) - independent of whichever extension <see cref="IFileCommands.GetFullPathXmlFile"/>
    /// happened to resolve for the element itself.
    /// </summary>
    private string AnimationsFileNameSuffix =>
        ElementAnimationsSave.GetFileNameSuffix(GumProjectSave.IsJsonFormat(_projectManager.GumProjectSave?.FullFileName ?? ""));

    /// <inheritdoc/>
    public FilePath? GetAbsoluteAnimationFileNameFor(string elementName)
    {
        var selectedElement = _selectedState.SelectedElement;
        var fullPathXmlForElement =
            selectedElement != null ? _fileCommands.GetFullPathXmlFile(selectedElement, elementName)
            : null;

        if (fullPathXmlForElement == null)
        {
            return null;
        }
        else
        {
            var absoluteFileName = fullPathXmlForElement.RemoveExtension() + AnimationsFileNameSuffix;

            return absoluteFileName;
        }
    }

    /// <inheritdoc/>
    public FilePath? GetAbsoluteAnimationFileNameFor(ElementSave elementSave)
    {
        var fullPathXmlForElement = _fileCommands.GetFullPathXmlFile(elementSave, elementSave.Name);

        if (fullPathXmlForElement == null)
        {
            return null;
        }
        else
        {
            var absoluteFileName = fullPathXmlForElement.RemoveExtension() + AnimationsFileNameSuffix;

            return absoluteFileName;
        }
    }
}
