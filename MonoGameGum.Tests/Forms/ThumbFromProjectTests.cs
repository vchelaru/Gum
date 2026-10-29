using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Slider and ScrollBar components loaded from a project find their thumb by the "ThumbInstance"
/// name, whichever component backs it: the per-control SliderThumb/ScrollBarThumb that Add Forms
/// ships now, or the shared Controls/Thumb that older projects keep (#6808).
/// </summary>
public class ThumbFromProjectTests : BaseTestClass
{
    [Theory]
    [InlineData("Controls/Slider")]
    [InlineData("Controls/ScrollBar")]
    public void ThumbInstance_ShouldBeADragHandle_WhenLoadedFromTemplate(string controlName)
    {
        GumProjectSave project = LoadTemplate();

        AssertThumbIsDragHandle(project, controlName);
    }

    [Theory]
    [InlineData("Controls/Slider")]
    [InlineData("Controls/ScrollBar")]
    public void ThumbInstance_ShouldBeADragHandle_WhenProjectUsesSharedThumbComponent(string controlName)
    {
        GumProjectSave project = LoadTemplate();
        ComponentSave sliderThumb = project.Components.Single(c => c.Name == "Controls/SliderThumb");
        ComponentSave sharedThumb = sliderThumb.Clone();
        sharedThumb.Name = "Controls/Thumb";
        project.Components.RemoveAll(c => c.Name is "Controls/SliderThumb" or "Controls/ScrollBarThumb");
        project.Components.Add(sharedThumb);
        foreach (string name in new[] { "Controls/Slider", "Controls/ScrollBar" })
        {
            project.Components.Single(c => c.Name == name).Instances
                .Single(i => i.Name == "ThumbInstance").BaseType = "Controls/Thumb";
        }
        project.Initialize();
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        AssertThumbIsDragHandle(project, controlName);
    }

    private static void AssertThumbIsDragHandle(GumProjectSave project, string controlName)
    {
        ScreenSave screen = new() { Name = "ThumbScreen" };
        screen.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "ControlInstance", BaseType = controlName, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.UpdateLayout();
        GraphicalUiElement visual = screenVisual.GetGraphicalUiElementByName("ControlInstance")!;

        InteractiveGue thumbVisual = (InteractiveGue)visual.GetGraphicalUiElementByName("ThumbInstance")!;
        Button thumb = thumbVisual.FormsControlAsObject.ShouldBeAssignableTo<Button>()!;
        thumb.IsDragHandle.ShouldBeTrue();
        ((InteractiveGue)visual).FormsControlAsObject.ShouldBeAssignableTo<RangeBase>();
    }

    private static GumProjectSave LoadTemplate()
    {
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiation("Container", () => new Gum.GueDeriving.ContainerRuntime());
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
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
