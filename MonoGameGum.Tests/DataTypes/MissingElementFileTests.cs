using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// A project whose component file is missing (#7963): <see cref="GraphicalUiElement.MissingFileBehavior"/>
/// decides whether the load throws, and an instance of the missing component builds as an empty container.
/// </summary>
public class MissingElementFileTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MissingElementFile7963_" + Path.GetRandomFileName());

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_directory, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void ThrowIfFailed_ConsumeSilently_DoesNotThrowForMissingFiles()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        GumLoadResult loadResult = new GumLoadResult();
        loadResult.MissingFiles.Add("Components/Missing.gucx");

        Should.NotThrow(() => loadResult.ThrowIfFailed(new GumProjectSave()));
    }

    [Fact]
    public void ThrowIfFailed_ConsumeSilently_ThrowsForErrorMessageOrNullProject()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        GumLoadResult withError = new GumLoadResult { ErrorMessage = "Bad project" };
        GumLoadResult withMissingFile = new GumLoadResult();
        withMissingFile.MissingFiles.Add("Components/Missing.gucx");

        Should.Throw<Exception>(() => withError.ThrowIfFailed(new GumProjectSave())).Message.ShouldContain("Bad project");
        Should.Throw<Exception>(() => withMissingFile.ThrowIfFailed(null));
    }

    [Fact]
    public void ThrowIfFailed_ThrowException_ThrowsNamingTheMissingFile()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        GumLoadResult loadResult = new GumLoadResult();
        loadResult.MissingFiles.Add("Components/Missing.gucx");

        Should.Throw<Exception>(() => loadResult.ThrowIfFailed(new GumProjectSave()))
            .Message.ShouldContain("Missing file: Components/Missing.gucx");
    }

    // Matches how a missing texture draws nothing: the instance is there, empty, with a container renderable.
    [Fact]
    public void ToGraphicalUiElement_InstanceOfMissingComponent_IsEmptyContainer_WhenConsumeSilently()
    {
        GraphicalUiElement screen = BuildScreenWithMissingComponentInstance(MissingFileBehavior.ConsumeSilently);

        GraphicalUiElement missingInstance = screen.Children.OfType<GraphicalUiElement>().Single(c => c.Name == "MissingInstance");
        missingInstance.Children.ShouldBeEmpty();
        missingInstance.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    // Pins the behavior from before #7963: no renderable, and the build itself does not throw
    // (GumService.Initialize already threw at load).
    [Fact]
    public void ToGraphicalUiElement_InstanceOfMissingComponent_HasNoRenderable_WhenThrowException()
    {
        GraphicalUiElement screen = BuildScreenWithMissingComponentInstance(MissingFileBehavior.ThrowException);

        GraphicalUiElement missingInstance = screen.Children.OfType<GraphicalUiElement>().Single(c => c.Name == "MissingInstance");
        missingInstance.Children.ShouldBeEmpty();
        missingInstance.RenderableComponent.ShouldBeNull();
    }

    private GraphicalUiElement BuildScreenWithMissingComponentInstance(MissingFileBehavior behavior)
    {
        GumProjectSave project = LoadProjectWithMissingComponent(out GumLoadResult loadResult);
        loadResult.MissingFiles.ShouldHaveSingleItem().ShouldEndWith("MissingComponent.gucx");
        GraphicalUiElement.MissingFileBehavior = behavior;
        ObjectFinder.Self.GumProjectSave = project;
        project.Initialize();

        return project.Screens.Single(s => s.Name == "MainScreen").ToGraphicalUiElement();
    }

    private GumProjectSave LoadProjectWithMissingComponent(out GumLoadResult loadResult)
    {
        Directory.CreateDirectory(_directory);
        string gumxPath = Path.Combine(_directory, "Proj.gumx");

        ComponentSave component = new ComponentSave { Name = "MissingComponent", BaseType = "Container" };
        component.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default", ParentContainer = component });
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "MissingInstance", BaseType = "MissingComponent", ParentContainer = screen });

        GumProjectSave toSave = new GumProjectSave();
        toSave.Screens.Add(screen);
        toSave.Components.Add(component);
        toSave.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        toSave.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        toSave.Save(gumxPath, saveElements: true);
        File.Delete(Path.Combine(_directory, "Components", "MissingComponent.gucx"));

        GumProjectSave? loaded = GumProjectSave.Load(gumxPath, out loadResult);
        return loaded.ShouldNotBeNull();
    }
}
