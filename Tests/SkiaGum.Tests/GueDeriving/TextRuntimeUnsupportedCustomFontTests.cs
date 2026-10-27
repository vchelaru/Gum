using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// #6499: Skia renders only .ttf/.otf custom fonts, so a .fnt CustomFontFile reports an error naming
/// the element and the file, once per element and file, and the text keeps drawing with the default font.
/// </summary>
public class TextRuntimeUnsupportedCustomFontTests : BaseTestClass
{
    private readonly List<string> _reported = new();
    private readonly Action<string> _handler;

    public TextRuntimeUnsupportedCustomFontTests()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);
        // Initialize defaults to ThrowException (#6889); these tests are about the reported (silent) case.
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        _handler = message => _reported.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _handler;
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _handler;
        ObjectFinder.Self.GumProjectSave = null;
        // Restores MissingFileBehavior (and the rest of the process-wide state) to what the previous test left.
        base.Dispose();
    }

    [Fact]
    public void CustomFontFile_Fnt_FromProjectLoad_ReportsOnceNamingInstanceAndFile()
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        StandardElementSave textStandard = new StandardElementSave { Name = "Text" };
        textStandard.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Text")!);
        textStandard.DefaultState!.ParentContainer = textStandard;
        project.StandardElements.Add(textStandard);
        ScreenSave screen = new ScreenSave { Name = "FntScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        state.Variables.Add(new VariableSave { Name = "Label1.UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Label1.CustomFontFile", Type = "string", Value = "Fonts/ProjectFont.fnt", SetsValue = true });
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Label1", BaseType = "Text", ParentContainer = screen });
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement root = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        root.UpdateLayout();
        root.UpdateLayout();

        string message = _reported.ShouldHaveSingleItem();
        message.ShouldContain("Label1");
        message.ShouldContain("Fonts/ProjectFont.fnt");
        message.ShouldContain(".ttf");
        message.ShouldContain(".otf");
    }

    [Fact]
    public void CustomFontFile_Fnt_SetInCode_ReportsOnceNamingElementAndFile()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.UseCustomFont = true;
        sut.CustomFontFile = "Fonts/MyFont.fnt";
        sut.FontSize = 30;
        sut.IsBold = true;
        sut.UpdateLayout();

        string message = _reported.ShouldHaveSingleItem();
        message.ShouldBe(
            "Text TitleText uses CustomFontFile Fonts/MyFont.fnt, which is not a TrueType or OpenType font Skia can render. " +
            "Skia needs a .ttf or .otf custom font file; this text draws with the default font instead.");
    }

    [Fact]
    public void CustomFontFile_Fnt_SetInCode_KeepsDefaultFontRendering()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.UseCustomFont = true;
        sut.CustomFontFile = "Fonts/MyFont.fnt";

        sut.RenderableComponent.ShouldBeOfType<Text>().FontName.ShouldBe(sut.Font);
    }

    [Fact]
    public void CustomFontFile_SecondFntOnSameElement_ReportsAgain()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.UseCustomFont = true;
        sut.CustomFontFile = "Fonts/First.fnt";
        sut.CustomFontFile = "Fonts/Second.fnt";
        sut.CustomFontFile = "Fonts/First.fnt";

        _reported.Count.ShouldBe(2);
        _reported[0].ShouldContain("Fonts/First.fnt");
        _reported[1].ShouldContain("Fonts/Second.fnt");
    }

    [Fact]
    public void CustomFontFile_SameFntOnTwoElements_ReportsForEach()
    {
        TextRuntime first = new() { Name = "First" };
        TextRuntime second = new() { Name = "Second" };

        first.UseCustomFont = true;
        first.CustomFontFile = "Fonts/Shared.fnt";
        second.UseCustomFont = true;
        second.CustomFontFile = "Fonts/Shared.fnt";

        _reported.Count.ShouldBe(2);
        _reported[0].ShouldContain("First");
        _reported[1].ShouldContain("Second");
    }

    [Fact]
    public void CustomFontFile_Fnt_WithUseCustomFontFalse_DoesNotReport()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.CustomFontFile = "Fonts/MyFont.fnt";
        sut.UseCustomFont = false;

        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void UseCustomFont_WithNoCustomFontFile_DoesNotReport()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.UseCustomFont = true;

        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void CustomFontFile_MissingTtf_ReportsNotFoundOnce()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.UseCustomFont = true;
        sut.CustomFontFile = "Fonts/DoesNotExist.ttf";
        sut.FontSize = 30;

        string message = _reported.ShouldHaveSingleItem();
        message.ShouldStartWith(
            "Text TitleText uses CustomFontFile Fonts/DoesNotExist.ttf, which was not found; " +
            "this text draws with the default font instead.");
        message.ShouldNotContain("Skia needs");
    }

    [Fact]
    public void CustomFontFile_MissingTtf_WithThrowException_Throws()
    {
        TextRuntime sut = new() { Name = "TitleText" };
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        sut.UseCustomFont = true;

        Should.Throw<FileNotFoundException>(() => sut.CustomFontFile = "Fonts/DoesNotExist.ttf")
            .Message.ShouldContain("Fonts/DoesNotExist.ttf");
    }

    [Fact]
    public void CustomFontFile_CorruptTtf_ReportsNotRenderable()
    {
        string corruptPath = Path.Combine(Path.GetTempPath(), "GumCorruptFont_" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(corruptPath, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        try
        {
            TextRuntime sut = new() { Name = "TitleText" };

            sut.UseCustomFont = true;
            sut.CustomFontFile = corruptPath;

            string message = _reported.ShouldHaveSingleItem();
            message.ShouldBe(
                $"Text TitleText uses CustomFontFile {corruptPath}, which is not a TrueType or OpenType font Skia can render. " +
                "Skia needs a .ttf or .otf custom font file; this text draws with the default font instead.");
        }
        finally
        {
            File.Delete(corruptPath);
        }
    }

    [Fact]
    public void Font_MissingTtfPath_ReportsNotFoundNamingFont()
    {
        TextRuntime sut = new() { Name = "TitleText" };

        sut.Font = "Fonts/DoesNotExist.ttf";

        _reported.ShouldHaveSingleItem().ShouldStartWith(
            "Text TitleText uses Font Fonts/DoesNotExist.ttf, which was not found; " +
            "this text draws with the default font instead.");
    }
}
