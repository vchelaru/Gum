using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

public class AnimationFilePathService : IAnimationFilePathService
{
    private readonly ISelectedState _selectedState;

    public AnimationFilePathService(ISelectedState selectedState)
    {
        _selectedState = selectedState;
    }

    /// <inheritdoc/>
    public FilePath? GetAbsoluteAnimationFileNameFor(string elementName)
    {
        var selectedElement = _selectedState.SelectedElement;
        var fullPathXmlForElement = 
            selectedElement != null ? ElementSaveExtensionMethodsGumTool.GetFullPathXmlFile(selectedElement, elementName)
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
        var fullPathXmlForElement = elementSave.GetFullPathXmlFile();

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
