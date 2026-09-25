using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.NineSlicePlugin;

[Export(typeof(PluginBase))]
internal class MainNineSlicePlugin : CorePriorityPlugin
{
    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ProjectLocationSet += HandleProjectLocationSet;
    }

    private void HandleProjectLocationSet(FilePath path)
    {
        // Setting the project's location gives it a file name.
        var gumProject = ObjectFinder.Self.GumProjectSave;
        if (gumProject?.FullFileName is not { } projectFileName)
        {
            return;
        }

        var sourceFile = Path.Combine(GetExecutingDirectory(), "Content", "ExampleSpriteFrame.png");
        var destinationFile = FileManager.GetDirectory(projectFileName) + "ExampleSpriteFrame.png";
        try
        {
            System.IO.File.Copy(sourceFile, destinationFile);

            var nineSliceStandard = gumProject.StandardElements.Find(item => item.Name == "NineSlice");
            if (nineSliceStandard == null)
            {
                return;
            }
            nineSliceStandard.GetDefaultStateOrThrow().SetValue("SourceFile", "ExampleSpriteFrame.png", "string");

            _fileCommands.TryAutoSaveElement(nineSliceStandard);    
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput($"Error copying ExampleSpriteFrame.png: {e}");
        }
    }

    static string GetExecutingDirectory()
    {
        // Assembly.GetExecutingAssembly().Location returns empty string in single-file published apps.
        return AppContext.BaseDirectory;
    }

}
