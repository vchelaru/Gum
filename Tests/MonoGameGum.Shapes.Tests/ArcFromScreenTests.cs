using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Shapes.Tests;

/// <summary>
/// #7296 - an Arc instance loaded from a screen (the path gumcli screenshot and a game take) must
/// instantiate without recursing and carry the authored values on its renderable.
/// </summary>
public class ArcFromScreenTests : RegistrationIsolatedTest
{
    // #7524 - a Blend authored on an Arc instance in a saved project reaches the Apos renderable
    // (and the runtime's own Blend) when the project loads; the screen goes through the same
    // XML a .gumx stores it in.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.Additive)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void ToGraphicalUiElement_ArcInstanceWithSavedBlend_AppliesBlendToRenderable(Gum.RenderingLibrary.Blend blend)
    {
        AposShapeRuntime.RegisterRuntimeTypes();

        StandardElementSave arcStandard = new() { Name = "Arc" };
        arcStandard.States.Add(new StateSave { Name = "Default", ParentContainer = arcStandard });

        ScreenSave authored = new() { Name = "ArcBlendScreen" };
        authored.States.Add(new StateSave { Name = "Default", ParentContainer = authored });
        authored.Instances.Add(new InstanceSave { Name = "ArcInstance", BaseType = "Arc" });
        authored.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Blend", Type = "Blend", Value = blend, SetsValue = true });

        string xml;
        ToolsUtilities.FileManager.XmlSerialize(authored, out xml);
        string path = Path.Combine(Path.GetTempPath(), "gum7524_" + blend + ".gusx");
        File.WriteAllText(path, xml);
        ScreenSave screen = ToolsUtilities.FileManager.XmlDeserialize<ScreenSave>(path);
        File.Delete(path);
        foreach (InstanceSave instance in screen.Instances) { instance.ParentContainer = screen; }
        foreach (StateSave state in screen.States) { state.ParentContainer = screen; }

        GumProjectSave project = new();
        project.StandardElements.Add(arcStandard);
        project.Screens.Add(screen);
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = screen.ToGraphicalUiElement();

            ArcRuntime instance = (ArcRuntime)visual.GetGraphicalUiElementByName("ArcInstance")!;
            ((Arc)instance.RenderableComponent!).Blend.ShouldBe(blend);
            instance.Blend.ShouldBe(blend);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    [Fact]
    public void ToGraphicalUiElement_ArcInstanceWithAuthoredVariables_AppliesThicknessAndAnglesToRenderable()
    {
        AposShapeRuntime.RegisterRuntimeTypes();

        StandardElementSave arcStandard = new() { Name = "Arc" };
        arcStandard.States.Add(new StateSave { Name = "Default", ParentContainer = arcStandard });

        ScreenSave screen = new() { Name = "ArcScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "ArcInstance", BaseType = "Arc", ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Thickness", Type = "float", Value = 24f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.SweepAngle", Type = "float", Value = 270f, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "ArcInstance.StartAngle", Type = "float", Value = 30f, SetsValue = true });

        GumProjectSave project = new();
        project.StandardElements.Add(arcStandard);
        project.Screens.Add(screen);
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            GraphicalUiElement visual = screen.ToGraphicalUiElement();

            GraphicalUiElement instance = visual.GetGraphicalUiElementByName("ArcInstance")!;
            instance.ShouldBeAssignableTo<ArcRuntime>();
            Arc renderable = (Arc)instance.RenderableComponent!;
            ((ArcRuntime)instance).Thickness.ShouldBe(24f);
            renderable.SweepAngle.ShouldBe(270f);
            renderable.StartAngle.ShouldBe(30f);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }
}
