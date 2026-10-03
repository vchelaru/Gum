using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Pins the data-driven path a loaded .gumx uses for the <c>ColorOperation</c> variable
/// (<c>ApplyState</c> to <c>SetProperty(string, object)</c>) on Sprite and NineSlice (#4880).
/// </summary>
public class DataDrivenColorOperationPropertyTests : BaseTestClass
{
    [Theory]
    [InlineData(ColorOperation.Add)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    public void SetProperty_ColorOperation_OnSprite_AppliesEnumValue(ColorOperation operation)
    {
        SpriteRuntime sut = new();

        sut.SetProperty("ColorOperation", operation);

        sut.ColorOperation.ShouldBe(operation);
    }

    [Theory]
    [InlineData(ColorOperation.Add)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    public void SetProperty_ColorOperation_OnNineSlice_AppliesEnumValue(ColorOperation operation)
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("ColorOperation", operation);

        sut.ColorOperation.ShouldBe(operation);
    }

    [Fact]
    public void SetProperty_ColorOperation_OnSprite_AppliesRawIntegerFromFile()
    {
        // .gumx stores enum variables as their underlying int, and the runtime does not coerce them.
        SpriteRuntime sut = new();

        sut.SetProperty("ColorOperation", (int)ColorOperation.Add);

        sut.ColorOperation.ShouldBe(ColorOperation.Add);
    }

    [Fact]
    public void SetProperty_ColorOperation_OnNineSlice_AppliesRawIntegerFromFile()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("ColorOperation", (int)ColorOperation.ColorTextureAlpha);

        sut.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
    }

    [Fact]
    public void LoadedGumx_ShouldApplyColorOperationToSpriteAndNineSliceInstances()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "GumColorOpTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            StandardElementsManager.Self.Initialize();

            GumProjectSave project = new();
            StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

            ScreenSave screen = new ScreenSave { Name = "TestScreen" };
            StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
            screen.States.Add(screenDefault);
            AddInstance(screen, "SpriteInstance", "Sprite", ColorOperation.Add);
            AddInstance(screen, "NineSliceInstance", "NineSlice", ColorOperation.ColorTextureAlpha);
            project.Screens.Add(screen);
            project.ScreenReferences.Add(new ElementReference { Name = "TestScreen", ElementType = ElementType.Screen });

            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.StandardSubfolder));
            string gumxPath = Path.Combine(tempDirectory, "Test." + GumProjectSave.ProjectExtension);
            project.Save(gumxPath, saveElements: true);

            GumProjectSave? loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);
            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            // GumService calls Initialize after loading, which is where enum ints are coerced.
            loaded.Initialize();
            ObjectFinder.Self.GumProjectSave = loaded;

            GraphicalUiElement screenGue = loaded.Screens.First(s => s.Name == "TestScreen").ToGraphicalUiElement();

            // Instances loaded from a file are plain GraphicalUiElements wrapping the renderable.
            Sprite sprite = (Sprite)screenGue.GetGraphicalUiElementByName("SpriteInstance")!.RenderableComponent;
            NineSlice nineSlice = (NineSlice)screenGue.GetGraphicalUiElementByName("NineSliceInstance")!.RenderableComponent;
            sprite.ColorOperation.ShouldBe(ColorOperation.Add);
            nineSlice.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static void AddInstance(ScreenSave screen, string name, string baseType, ColorOperation operation)
    {
        screen.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = name + ".ColorOperation",
            Type = "ColorOperation",
            Value = operation,
            SetsValue = true,
        });
    }
}
