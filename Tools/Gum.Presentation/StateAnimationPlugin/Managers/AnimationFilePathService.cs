using Gum.Commands;
using Gum.DataTypes;
using Gum.ToolStates;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

public class AnimationFilePathService : IAnimationFilePathService
{
    private readonly ISelectedState _selectedState;
    private readonly IFileCommands _fileCommands;

    public AnimationFilePathService(ISelectedState selectedState, IFileCommands fileCommands)
    {
        _selectedState = selectedState;
        _fileCommands = fileCommands;
    }

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
            var absoluteFileName = fullPathXmlForElement.RemoveExtension() + "Animations.ganx";

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
            var absoluteFileName = fullPathXmlForElement.RemoveExtension() + "Animations.ganx";

            return absoluteFileName;
        }
    }
}
