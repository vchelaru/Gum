using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// <see cref="ObjectFinder.GetAllFilesInProject(GumBundleInclusion, bool)"/> with font resolution off: the
/// file watcher asks this after most edits, and resolving every Text instance's effective font was nearly all
/// of its cost (about 400 ms of a 430 ms walk on a project with 800 instances).
/// </summary>
public class ObjectFinderFileReferencesWithoutFontResolutionTests : BaseTestClass
{
    private static readonly string FakeProjectDirectory =
        OperatingSystem.IsWindows() ? "C:/FakeGumProject/" : "/tmp/FakeGumProject/";

    private const GumBundleInclusion CoreAndExternal = GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles;

    private ScreenSave AddScreen()
    {
        Project.FullFileName = FakeProjectDirectory + "MyProject.gumx";
        ObjectFinder.Self.GumProjectSave = Project;
        ScreenSave screen = new ScreenSave { Name = "Screen1" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        return screen;
    }

    private static void AddInstance(ScreenSave screen, string name, string baseType, params VariableSave[] variables)
    {
        screen.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = screen });
        foreach (VariableSave variable in variables)
        {
            variable.Name = name + "." + variable.Name;
            variable.SetsValue = true;
            screen.DefaultState.Variables.Add(variable);
        }
    }

    [Fact]
    public void GetAllFilesInProject_WithoutFontResolution_StillIncludesAFileASpriteNames()
    {
        ScreenSave screen = AddScreen();
        AddInstance(screen, "Picture", "Sprite",
            new VariableSave { Name = "SourceFile", Type = "string", Value = "Images/hero.png", IsFile = true });

        List<string> files = ObjectFinder.Self.GetAllFilesInProject(CoreAndExternal, resolveFontFiles: false).ToList();

        files.ShouldContain(new FilePath(FakeProjectDirectory + "Images/hero.png").StandardizedCaseSensitive);
    }

    [Fact]
    public void GetAllFilesInProject_WithoutFontResolution_StillIncludesACustomFontFileATextNames()
    {
        ScreenSave screen = AddScreen();
        AddInstance(screen, "Label", "Text",
            new VariableSave { Name = "UseCustomFont", Type = "bool", Value = true },
            new VariableSave { Name = "CustomFontFile", Type = "string", Value = "Fonts/Custom.fnt", IsFile = true });

        List<string> files = ObjectFinder.Self.GetAllFilesInProject(CoreAndExternal, resolveFontFiles: false).ToList();

        files.ShouldContain(new FilePath(FakeProjectDirectory + "Fonts/Custom.fnt").StandardizedCaseSensitive);
    }

}
