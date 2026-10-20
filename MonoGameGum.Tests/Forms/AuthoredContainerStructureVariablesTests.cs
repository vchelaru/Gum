using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ClipsChildren and IsRenderTarget authored on an instance of a Forms control in the Gum tool
/// reach the runtime visual, whether or not the template lists them as hidden from instances. The
/// hidden list is a tool-only setting: a project that authored them before the template hid them
/// keeps its values.
/// </summary>
public class AuthoredContainerStructureVariablesTests : BaseTestClass
{
    [Theory]
    [InlineData("Controls/ItemsControl")]
    [InlineData("Controls/ListBox")]
    [InlineData("Controls/ScrollViewer")]
    [InlineData("Controls/Window")]
    [InlineData("Controls/DialogBox")]
    public void AuthoredInstance_ClipsChildrenAndIsRenderTarget_ReachTheVisual(string controlType)
    {
        GraphicalUiElement screen = CreateScreen("ControlInstance", controlType, new[]
        {
            Authored("ControlInstance.ClipsChildren", "bool", true),
            Authored("ControlInstance.IsRenderTarget", "bool", true),
        });

        GraphicalUiElement visual = screen.GetGraphicalUiElementByName("ControlInstance")!;
        visual.ClipsChildren.ShouldBeTrue();
        visual.IsRenderTarget.ShouldBeTrue();
    }

    private static VariableSave Authored(string name, string type, object value) =>
        new VariableSave { Name = name, Type = type, Value = value, SetsValue = true };

    private static GraphicalUiElement CreateScreen(string instanceName, string instanceType, VariableSave[] authoredVariables)
    {
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        ScreenSave screen = new ScreenSave { Name = "StructureScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceType, ParentContainer = screen });
        foreach (VariableSave variable in authoredVariables)
        {
            defaultState.Variables.Add(variable);
        }
        project.Screens.Add(screen);
        project.Initialize();

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.UpdateLayout();
        return screenVisual;
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
