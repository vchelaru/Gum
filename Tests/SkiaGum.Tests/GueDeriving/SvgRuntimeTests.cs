using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using SkiaGum;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Content;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class SvgRuntimeTests : BaseTestClass
{
    public SvgRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static string WriteSvg()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "GumSvgRuntimeTest_" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>");
        return svgPath;
    }

    [Fact]
    public void ToGraphicalUiElement_SvgInstanceWithSourceFile_LoadsTexture()
    {
        string svgPath = WriteSvg();
        LoaderManager.Self.ContentLoader = new RenderingLibrary.Content.ContentLoader();
        SystemManagers systemManagers = new SystemManagers();
        systemManagers.Initialize();
        StandardElementsManager.Self.Initialize();

        try
        {
            GumProjectSave project = new GumProjectSave();
            StandardElementSave svgStandard = new StandardElementSave { Name = "Svg" };
            svgStandard.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Svg")!);
            svgStandard.DefaultState!.ParentContainer = svgStandard;
            project.StandardElements.Add(svgStandard);
            ScreenSave screen = new ScreenSave { Name = "TestScreen" };
            StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
            state.Variables.Add(new VariableSave { Name = "Svg1.SourceFile", Type = "string", Value = svgPath, SetsValue = true });
            screen.States.Add(state);
            screen.Instances.Add(new InstanceSave { Name = "Svg1", BaseType = "Svg", ParentContainer = screen });
            project.Screens.Add(screen);
            ObjectFinder.Self.GumProjectSave = project;

            GraphicalUiElement root = screen.ToGraphicalUiElement(systemManagers, addToManagers: false);

            GraphicalUiElement svg = root.GetGraphicalUiElementByName("Svg1")!;
            ((VectorSprite)svg.RenderableComponent).Texture.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(svgPath);
        }
    }

    [Fact]
    public void SetPropertySourceFile_MissingFile_ThrowsOnSvgRuntimeAndPlainElement()
    {
        string missing = Path.Combine(Path.GetTempPath(), "GumMissing_" + Guid.NewGuid().ToString("N") + ".svg");
        LoaderManager.Self.ContentLoader = new RenderingLibrary.Content.ContentLoader();

        SvgRuntime runtime = new SvgRuntime();
        Should.Throw<ArgumentException>(() => runtime.SetProperty("SourceFile", missing));

        GraphicalUiElement plain = new GraphicalUiElement();
        plain.SetContainedObject(new VectorSprite());
        Should.Throw<ArgumentException>(() => plain.SetProperty("SourceFile", missing));
    }

    [Fact]
    public void SourceFile_SetToNullAfterFile_ClearsTexture()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "GumSvgRuntimeTest_" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>");
        LoaderManager.Self.ContentLoader = new RenderingLibrary.Content.ContentLoader();

        try
        {
            SvgRuntime sut = new SvgRuntime();
            sut.SourceFile = svgPath;
            sut.Texture.ShouldNotBeNull();

            sut.SourceFile = null;

            sut.Texture.ShouldBeNull();
        }
        finally
        {
            File.Delete(svgPath);
        }
    }

    [Fact]
    public void SetPropertySourceFile_RealFile_LoadsTextureAndUpdatesSourceFile()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "GumSvgRuntimeTest_" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>");
        LoaderManager.Self.ContentLoader = new RenderingLibrary.Content.ContentLoader();

        try
        {
            SvgRuntime sut = new SvgRuntime();

            sut.SetProperty("SourceFile", svgPath);

            sut.Texture.ShouldNotBeNull();
            sut.SourceFile.ShouldBe(svgPath);
        }
        finally
        {
            File.Delete(svgPath);
        }
    }
}
