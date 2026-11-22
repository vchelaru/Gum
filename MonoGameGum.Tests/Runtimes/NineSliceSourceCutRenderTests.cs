using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// A different source cut on each side (#8354): the corners and edges keep their texels and size,
// only the middle stretches.
public class NineSliceSourceCutRenderTests : BaseTestClass
{
    // Fabricates a Texture2D (its constructor needs a GraphicsDevice) with the size fields set.
    private static Texture2D MakeTexture(int width, int height)
    {
        Type type = typeof(Texture2D);
        Texture2D texture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        type.GetField("width", flags)!.SetValue(texture, width);
        type.GetField("height", flags)!.SetValue(texture, height);
        return texture;
    }

    private static NineSlice Create(int textureWidth, int textureHeight, float width, float height)
    {
        NineSlice nineSlice = new() { Width = width, Height = height };
        nineSlice.SetSingleTexture(MakeTexture(textureWidth, textureHeight));
        return nineSlice;
    }

    private static void ShouldBe(Sprite section, int srcX, int srcY, int srcWidth, int srcHeight, float width, float height)
    {
        section.SourceRectangle.ShouldBe(new System.Drawing.Rectangle(srcX, srcY, srcWidth, srcHeight));
        section.Width.ShouldBe(width);
        section.Height.ShouldBe(height);
    }

    [Fact]
    public void Refresh_PerSideCut_ShouldKeepCornersAndEdgesAtTheirOwnSize()
    {
        // 12x16 texture cut at left 6, top 8, right 2, bottom 3 leaves a 4x5 middle; drawn at 40x26.
        NineSlice sut = Create(12, 16, 40, 26);
        sut.CustomFrameTextureCoordinateLeft = 6;
        sut.CustomFrameTextureCoordinateTop = 8;
        sut.CustomFrameTextureCoordinateRight = 2;
        sut.CustomFrameTextureCoordinateBottom = 3;

        sut.RefreshSections();

        ShouldBe(sut.GetSection(NineSliceSections.TopLeft), 0, 0, 6, 8, 6, 8);
        ShouldBe(sut.GetSection(NineSliceSections.Top), 6, 0, 4, 8, 32, 8);
        ShouldBe(sut.GetSection(NineSliceSections.TopRight), 10, 0, 2, 8, 2, 8);
        ShouldBe(sut.GetSection(NineSliceSections.Left), 0, 8, 6, 5, 6, 15);
        ShouldBe(sut.GetSection(NineSliceSections.Center), 6, 8, 4, 5, 32, 15);
        ShouldBe(sut.GetSection(NineSliceSections.Right), 10, 8, 2, 5, 2, 15);
        ShouldBe(sut.GetSection(NineSliceSections.BottomLeft), 0, 13, 6, 3, 6, 3);
        ShouldBe(sut.GetSection(NineSliceSections.Bottom), 6, 13, 4, 3, 32, 3);
        ShouldBe(sut.GetSection(NineSliceSections.BottomRight), 10, 13, 2, 3, 2, 3);
    }

    [Fact]
    public void Refresh_PerSideCutWithBorderScale_ShouldScaleThicknessButNotTheCut()
    {
        NineSlice sut = Create(12, 16, 40, 26);
        sut.CustomFrameTextureCoordinateLeft = 6;
        sut.CustomFrameTextureCoordinateRight = 2;
        sut.CustomFrameTextureCoordinateWidth = 3;
        sut.CustomBorderScaleLeft = 2;

        sut.RefreshSections();

        // Left: 6 texels drawn 12 wide. Right: 2 texels at scale 1. Top/bottom fall back to the width of 3.
        ShouldBe(sut.GetSection(NineSliceSections.TopLeft), 0, 0, 6, 3, 12, 3);
        ShouldBe(sut.GetSection(NineSliceSections.Top), 6, 0, 4, 3, 26, 3);
        ShouldBe(sut.GetSection(NineSliceSections.TopRight), 10, 0, 2, 3, 2, 3);
        ShouldBe(sut.GetSection(NineSliceSections.Center), 6, 3, 4, 10, 26, 20);
    }

    [Fact]
    public void Refresh_NoCutSet_ShouldCutAThirdOfEachAxis()
    {
        NineSlice sut = Create(12, 16, 40, 26);

        sut.RefreshSections();

        ShouldBe(sut.GetSection(NineSliceSections.TopLeft), 0, 0, 4, 5, 4, 5);
        ShouldBe(sut.GetSection(NineSliceSections.Top), 4, 0, 4, 5, 32, 5);
        ShouldBe(sut.GetSection(NineSliceSections.TopRight), 8, 0, 4, 5, 4, 5);
        ShouldBe(sut.GetSection(NineSliceSections.Center), 4, 5, 4, 6, 32, 16);
        ShouldBe(sut.GetSection(NineSliceSections.BottomRight), 8, 11, 4, 5, 4, 5);
    }

    [Fact]
    public void Runtime_CustomFrameTextureCoordinateSides_ShouldForwardAndNotify()
    {
        NineSliceRuntime sut = new();
        List<string?> changed = new();
        sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        sut.CustomFrameTextureCoordinateLeft = 6;
        sut.CustomFrameTextureCoordinateTop = 8;
        sut.CustomFrameTextureCoordinateRight = 2;
        sut.CustomFrameTextureCoordinateBottom = 3;

        NineSlice contained = (NineSlice)sut.RenderableComponent;
        contained.CustomFrameTextureCoordinateLeft.ShouldBe(6f);
        contained.CustomFrameTextureCoordinateTop.ShouldBe(8f);
        contained.CustomFrameTextureCoordinateRight.ShouldBe(2f);
        contained.CustomFrameTextureCoordinateBottom.ShouldBe(3f);
        changed.ShouldBe(new string?[]
        {
            nameof(NineSliceRuntime.CustomFrameTextureCoordinateLeft),
            nameof(NineSliceRuntime.CustomFrameTextureCoordinateTop),
            nameof(NineSliceRuntime.CustomFrameTextureCoordinateRight),
            nameof(NineSliceRuntime.CustomFrameTextureCoordinateBottom),
        });
    }

    [Fact]
    public void Refresh_EverySideZero_ShouldDrawOnlyTheCenter()
    {
        NineSlice sut = Create(12, 16, 40, 26);
        sut.CustomFrameTextureCoordinateWidth = 4;
        sut.CustomFrameTextureCoordinateLeft = 0;
        sut.CustomFrameTextureCoordinateTop = 0;
        sut.CustomFrameTextureCoordinateRight = 0;
        sut.CustomFrameTextureCoordinateBottom = 0;

        sut.RefreshSections();

        ShouldBe(sut.GetSection(NineSliceSections.Center), 0, 0, 12, 16, 40, 26);
    }

    [Fact]
    public void SetProperty_CustomFrameTextureCoordinateSides_ShouldForwardToRuntime()
    {
        NineSliceRuntime sut = new();
        System.Collections.Generic.List<string?> changed = new();
        sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        sut.SetProperty("CustomFrameTextureCoordinateLeft", 6f);
        sut.SetProperty("CustomFrameTextureCoordinateTop", 0f);
        sut.SetProperty("CustomFrameTextureCoordinateRight", 2.5f);
        sut.SetProperty("CustomFrameTextureCoordinateBottom", 3f);

        sut.CustomFrameTextureCoordinateLeft.ShouldBe(6f);
        sut.CustomFrameTextureCoordinateTop.ShouldBe(0f);
        sut.CustomFrameTextureCoordinateRight.ShouldBe(2.5f);
        sut.CustomFrameTextureCoordinateBottom.ShouldBe(3f);
        // Through the runtime, not reflection on the renderable, so bindings hear the change.
        changed.ShouldContain("CustomFrameTextureCoordinateLeft");
        changed.ShouldContain("CustomFrameTextureCoordinateBottom");

        sut.SetProperty("CustomFrameTextureCoordinateLeft", null!);

        sut.CustomFrameTextureCoordinateLeft.ShouldBeNull();
    }
}
