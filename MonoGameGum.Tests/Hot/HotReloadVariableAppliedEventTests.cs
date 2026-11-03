using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// <see cref="GumHotReloadManager.VariableApplied"/> reports each variable a reload writes, which is
/// what Preview's log shows as "what the tool sent".
/// </summary>
public class HotReloadVariableAppliedEventTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static GumProjectSave Build(float rectHeight)
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave standard = new StandardElementSave { Name = "ColoredRectangle" };
        StateSave standardState = new StateSave { Name = "Default", ParentContainer = standard };
        standardState.Variables.Add(new VariableSave { Name = "Height", Value = 100f, Type = "float", SetsValue = true });
        standard.States.Add(standardState);
        project.StandardElements.Add(standard);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenState);
        screen.Instances.Add(new InstanceSave { Name = "Rect", BaseType = "ColoredRectangle", ParentContainer = screen });
        screenState.Variables.Add(new VariableSave { Name = "Rect.Height", Value = rectHeight, Type = "float", SetsValue = true });
        project.Screens.Add(screen);
        return project;
    }

    [Fact]
    public void Reload_ChangedVariable_RaisesVariableAppliedForTheInstance()
    {
        GumProjectSave oldProject = Build(255f);
        ObjectFinder.Self.GumProjectSave = oldProject;
        GraphicalUiElement screen = oldProject.Screens.Single().ToGraphicalUiElement();
        GumProjectSave newProject = Build(128f);
        ObjectFinder.Self.GumProjectSave = newProject;
        List<(string Instance, string Variable, object? Value)> applied = new();
        void Handler(GraphicalUiElement element, VariableSave variable)
        {
            if (element.ElementSave?.Name == "TestScreen")
            {
                applied.Add((element.Name + "|" + element.ElementSave?.Name, variable.Name, variable.Value));
            }
        }

        GumHotReloadManager.VariableApplied += Handler;
        try
        {
            GumHotReloadManager.ApplyDiff(new[] { screen }, newProject, SystemManagers.Default, oldProject);
        }
        finally
        {
            GumHotReloadManager.VariableApplied -= Handler;
        }

        applied.ShouldContain(("TestScreen|TestScreen", "Rect.Height", (object?)128f));
    }

    [Fact]
    public void Reload_ThrowingSubscriber_StillAppliesTheVariable()
    {
        GumProjectSave oldProject = Build(255f);
        ObjectFinder.Self.GumProjectSave = oldProject;
        GraphicalUiElement screen = oldProject.Screens.Single().ToGraphicalUiElement();
        GumProjectSave newProject = Build(128f);
        ObjectFinder.Self.GumProjectSave = newProject;
        void Handler(GraphicalUiElement element, VariableSave variable)
        {
            if (element == screen)
            {
                throw new System.InvalidOperationException("subscriber failed");
            }
        }

        GumHotReloadManager.VariableApplied += Handler;
        try
        {
            GumHotReloadManager.ApplyDiff(new[] { screen }, newProject, SystemManagers.Default, oldProject);
        }
        finally
        {
            GumHotReloadManager.VariableApplied -= Handler;
        }

        screen.ContainedElements.Concat(screen.Children).First(c => c.Name == "Rect").Height.ShouldBe(128f);
    }
}
