using System;
using System.IO;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

/// <summary>
/// GumService.Initialize with a project whose component file is missing (#7963): the user's
/// <see cref="GraphicalUiElement.MissingFileBehavior"/>, set before Initialize, decides whether the load throws.
/// </summary>
public class MissingElementFileInitializeTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SkiaMissingElementFile7963_" + Path.GetRandomFileName());

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_directory, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void Initialize_ConsumeSilently_LoadsProjectAndListsMissingFile()
    {
        string gumxPath = WriteProjectWithMissingComponent();
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        GumService.Default.Initialize(surface.Canvas, 200, 100, gumxPath);

        GumService.Default.LastLoadResult.ShouldNotBeNull().MissingFiles.ShouldHaveSingleItem()
            .ShouldEndWith("MissingComponent.gucx");
        GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ConsumeSilently);
        GraphicalUiElement screen = Gum.Managers.ObjectFinder.Self.GumProjectSave!.Screens.Single()
            .ToGraphicalUiElement(RenderingLibrary.SystemManagers.Default, addToManagers: false);
        screen.Children.OfType<GraphicalUiElement>().Single(c => c.Name == "MissingInstance")
            .RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void Initialize_ThrowException_ThrowsNamingTheMissingFile()
    {
        string gumxPath = WriteProjectWithMissingComponent();
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        Should.Throw<Exception>(() => GumService.Default.Initialize(surface.Canvas, 200, 100, gumxPath))
            .Message.ShouldContain("MissingComponent.gucx");
    }

    [Fact]
    public void Initialize_KeepsMissingFileBehaviorSetBeforeIt_AcrossUninitialize()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        GumService.Default.Initialize(surface.Canvas, 200, 100);
        GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ConsumeSilently);
        GumService.Default.Uninitialize();
        GumService.Default.Initialize(surface.Canvas, 200, 100);

        GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ConsumeSilently);
    }

    [Fact]
    public void Initialize_DefaultsToThrowException_AcrossUninitialize_WhenNothingAssigned()
    {
        GraphicalUiElement.AssignedMissingFileBehavior = null;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        GumService.Default.Initialize(surface.Canvas, 200, 100);
        GumService.Default.Uninitialize();
        GumService.Default.Initialize(surface.Canvas, 200, 100);

        GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ThrowException);
    }

    private string WriteProjectWithMissingComponent()
    {
        Directory.CreateDirectory(_directory);
        string gumxPath = Path.Combine(_directory, "Proj.gumx");

        ComponentSave component = new ComponentSave { Name = "MissingComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "MissingInstance", BaseType = "MissingComponent", ParentContainer = screen });

        GumProjectSave project = new GumProjectSave();
        project.Screens.Add(screen);
        project.Components.Add(component);
        project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        project.Save(gumxPath, saveElements: true);
        File.Delete(Path.Combine(_directory, "Components", "MissingComponent.gucx"));

        return gumxPath;
    }
}
