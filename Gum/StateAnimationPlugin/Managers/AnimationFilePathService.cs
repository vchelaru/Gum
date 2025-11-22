using Gum.DataTypes;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

internal class AnimationFilePathService
{
    private readonly ISelectedState _selectedState;
    public AnimationFilePathService()
    {
        _selectedState = _selectedState = Locator.GetRequiredService<ISelectedState>();
    }
    
    public FilePath GetAbsoluteAnimationFileNameFor(string elementName)
    {
        var fullPathXmlForElement = ElementSaveExtensionMethodsGumTool.GetFullPathXmlFile(_selectedState.SelectedElement, elementName);

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
