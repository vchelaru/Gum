using Gum.DataTypes;
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
    public FilePath GetAbsoluteAnimationFileNameFor(string elementName)
    {
        var fullPathXmlForElement = ElementSaveExtensionMethodsGumTool.GetFullPathXmlFile(SelectedState.Self.SelectedElement, elementName);

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

    public FilePath GetAbsoluteAnimationFileNameFor(ElementSave elementSave)
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
