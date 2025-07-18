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
internal class MainNineSlicePlugin : InternalPlugin
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
        var gumProject = ObjectFinder.Self.GumProjectSave;

        var sourceFile = Path.Combine(GetExecutingDirectory(), "Content\\ExampleSpriteFrame.png");
        var destinationFile = FileManager.GetDirectory(gumProject.FullFileName) + "ExampleSpriteFrame.png";
        try
        {
            System.IO.File.Copy(sourceFile, destinationFile);

            var nineSliceStandard = gumProject.StandardElements.Find(item => item.Name == "NineSlice");
            nineSliceStandard.DefaultState.SetValue("SourceFile", "ExampleSpriteFrame.png", "string");

            GumCommands.Self.FileCommands.TryAutoSaveElement(nineSliceStandard);    
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput($"Error copying ExampleSpriteFrame.png: {e}");
        }
    }

    static string GetExecutingDirectory()
    {
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return path;
    }

}
