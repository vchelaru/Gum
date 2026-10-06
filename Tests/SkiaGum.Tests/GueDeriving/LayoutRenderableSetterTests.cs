using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using RenderingLibrary.Graphics;
using SkiaGum;
using SkiaGum.Content;
using SkiaSharp;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Setters that change what a Skia renderable reports as its size must re-run layout.
/// </summary>
public class LayoutRenderableSetterTests
{
    public LayoutRenderableSetterTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    static TextRuntime CreateText(string text)
    {
        TextRuntime sut = new();
        sut.WidthUnits = DimensionUnitType.RelativeToChildren;
        sut.HeightUnits = DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Height = 0;
        sut.Text = text;
        return sut;
    }

    [Fact]
    public void FontSize_ShouldResizeRelativeToChildrenText()
    {
        TextRuntime sut = CreateText("Hello");
        float widthBefore = sut.AbsoluteWidth;

        sut.FontSize = 40;

        sut.AbsoluteWidth.ShouldBeGreaterThan(widthBefore);
    }

    [Fact]
    public void FontSize_ShouldResizeOnResume_WhenSetWhileSuspended()
    {
        TextRuntime sut = CreateText("Hello");
        float widthBefore = sut.AbsoluteWidth;

        sut.SuspendLayout();
        sut.FontSize = 40;
        sut.AbsoluteWidth.ShouldBe(widthBefore);
        sut.ResumeLayout();

        sut.AbsoluteWidth.ShouldBeGreaterThan(widthBefore);
    }

    [Fact]
    public void Typeface_ShouldResizeRelativeToChildrenText()
    {
        TextRuntime sut = CreateText("Hello World");
        Text text = sut.RenderableComponent.ShouldBeOfType<Text>();
        float widthBefore = sut.AbsoluteWidth;

        sut.Typeface = SKTypeface.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf"));

        sut.AbsoluteWidth.ShouldNotBe(widthBefore);
        sut.AbsoluteWidth.ShouldBe(text.WrappedTextWidth);
    }

    [Fact]
    public void BoldWeight_ShouldResizeRelativeToChildrenText()
    {
        TextRuntime sut = CreateText("Hello World");
        Text text = sut.RenderableComponent.ShouldBeOfType<Text>();
        float widthBefore = sut.AbsoluteWidth;

        sut.BoldWeight = 3;

        sut.AbsoluteWidth.ShouldNotBe(widthBefore);
        sut.AbsoluteWidth.ShouldBe(text.WrappedTextWidth);
    }

    [Fact]
    public void MaxNumberOfLines_ShouldResizeRelativeToChildrenText()
    {
        TextRuntime sut = CreateText("Line1\nLine2\nLine3");
        float heightBefore = sut.AbsoluteHeight;

        sut.MaxNumberOfLines = 1;

        sut.AbsoluteHeight.ShouldBeLessThan(heightBefore);
    }

    [Fact]
    public void SpriteImage_ShouldResizePercentageOfSourceFileSprite()
    {
        SpriteRuntime sut = new();
        sut.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        sut.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        using SKBitmap bitmap = new(200, 80);
        using SKImage image = SKImage.FromBitmap(bitmap);

        sut.Image = image;

        sut.AbsoluteWidth.ShouldBe(200);
        sut.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void NineSliceSourceFile_ShouldResizePercentageOfSourceFileNineSlice()
    {
        IContentLoader? originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = new MockContentLoader(200, 80);
        try
        {
            NineSliceRuntime sut = new();
            sut.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
            sut.HeightUnits = DimensionUnitType.PercentageOfSourceFile;

            sut.SetProperty("SourceFile", "frame.png");

            sut.AbsoluteWidth.ShouldBe(200);
            sut.AbsoluteHeight.ShouldBe(80);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
        }
    }

    [Fact]
    public void SvgSourceFile_ShouldResizeMaintainFileAspectRatioHeight()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "GumSvgLayoutTest_" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"><rect width=\"20\" height=\"10\" fill=\"red\"/></svg>");
        IContentLoader? originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = new EmbeddedResourceContentLoader();
        try
        {
            SvgRuntime sut = new();
            sut.AbsoluteHeight.ShouldBe(100);

            sut.SourceFile = svgPath;

            sut.AbsoluteHeight.ShouldBe(50, tolerance: 0.01f);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
            File.Delete(svgPath);
        }
    }
}
