using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Per-side border scale overrides on NineSliceRuntime (#6983), mirroring how the Rectangle's
// CustomRadius* overrides behave: null falls back to BorderScale.
public class NineSliceBorderScaleTests : BaseTestClass
{
    private static readonly string[] Sides = { "Left", "Top", "Right", "Bottom" };

    [Fact]
    public void CustomBorderScales_ShouldDefaultToNull()
    {
        NineSliceRuntime sut = new();

        sut.CustomBorderScaleLeft.ShouldBeNull();
        sut.CustomBorderScaleTop.ShouldBeNull();
        sut.CustomBorderScaleRight.ShouldBeNull();
        sut.CustomBorderScaleBottom.ShouldBeNull();
    }

    [Fact]
    public void CustomBorderScales_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.CustomBorderScaleLeft = 1f;
        sut.CustomBorderScaleTop = 2f;
        sut.CustomBorderScaleRight = 3f;
        sut.CustomBorderScaleBottom = 0f;

        ((NineSlice)sut.RenderableComponent).CustomBorderScaleLeft.ShouldBe(1f);
        ((NineSlice)sut.RenderableComponent).CustomBorderScaleTop.ShouldBe(2f);
        ((NineSlice)sut.RenderableComponent).CustomBorderScaleRight.ShouldBe(3f);
        ((NineSlice)sut.RenderableComponent).CustomBorderScaleBottom.ShouldBe(0f);
    }

    [Fact]
    public void CustomBorderScales_ShouldNotifyPropertyChanged()
    {
        NineSliceRuntime sut = new();
        List<string?> changed = new();
        sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        sut.CustomBorderScaleLeft = 1f;
        sut.CustomBorderScaleTop = 1f;
        sut.CustomBorderScaleRight = 1f;
        sut.CustomBorderScaleBottom = 1f;

        changed.ShouldContain(nameof(NineSliceRuntime.CustomBorderScaleLeft));
        changed.ShouldContain(nameof(NineSliceRuntime.CustomBorderScaleTop));
        changed.ShouldContain(nameof(NineSliceRuntime.CustomBorderScaleRight));
        changed.ShouldContain(nameof(NineSliceRuntime.CustomBorderScaleBottom));
    }

    [Fact]
    public void SetProperty_CustomBorderScales_ShouldForwardToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("CustomBorderScaleLeft", 1.5f);
        sut.SetProperty("CustomBorderScaleTop", 2.5f);
        sut.SetProperty("CustomBorderScaleRight", 0f);
        sut.SetProperty("CustomBorderScaleBottom", 4f);

        sut.CustomBorderScaleLeft.ShouldBe(1.5f);
        sut.CustomBorderScaleTop.ShouldBe(2.5f);
        sut.CustomBorderScaleRight.ShouldBe(0f);
        sut.CustomBorderScaleBottom.ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_CustomBorderScaleNull_ShouldClearTheOverride()
    {
        NineSliceRuntime sut = new();
        sut.CustomBorderScaleLeft = 3f;
        sut.CustomBorderScaleTop = 3f;
        sut.CustomBorderScaleRight = 3f;
        sut.CustomBorderScaleBottom = 3f;

        sut.SetProperty("CustomBorderScaleLeft", null);
        sut.SetProperty("CustomBorderScaleTop", null);
        sut.SetProperty("CustomBorderScaleRight", null);
        sut.SetProperty("CustomBorderScaleBottom", null);

        sut.CustomBorderScaleLeft.ShouldBeNull();
        sut.CustomBorderScaleTop.ShouldBeNull();
        sut.CustomBorderScaleRight.ShouldBeNull();
        sut.CustomBorderScaleBottom.ShouldBeNull();
    }

    [Fact]
    public void SetProperty_CustomBorderScale_ShouldLeaveBorderScaleAlone()
    {
        NineSliceRuntime sut = new();
        sut.BorderScale = 2f;

        sut.SetProperty("CustomBorderScaleLeft", 5f);

        sut.BorderScale.ShouldBe(2f);
    }

    // Reads the destination size of each of the nine sections after the sprite sizes are refreshed,
    // so a mapping mistake (a side reading another side's override) is caught without a graphics device.
    private static List<(float Width, float Height)> SectionSizes(NineSlice nineSlice)
    {
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(NineSlice).GetField("_fullOutsideTexturePixelWidth", flags)!.SetValue(nineSlice, 1);
        typeof(NineSlice).GetField("_fullOutsideTexturePixelHeight", flags)!.SetValue(nineSlice, 1);
        typeof(NineSlice).GetMethod("RefreshSpriteDimensions", flags)!.Invoke(nineSlice, null);
        System.Collections.IList sprites = (System.Collections.IList)typeof(NineSlice).GetField("mSprites", flags)!.GetValue(nineSlice)!;
        List<(float, float)> sizes = new();
        foreach (Sprite sprite in sprites)
        {
            sizes.Add((sprite.Width, sprite.Height));
        }
        return sizes;
    }

    private static NineSlice Create(float? left, float? top, float? right, float? bottom) => new()
    {
        Width = 100, Height = 100, BorderScale = 5f,
        CustomBorderScaleLeft = left, CustomBorderScaleTop = top,
        CustomBorderScaleRight = right, CustomBorderScaleBottom = bottom,
    };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SectionSizes_LoneOverride_ReachesOnlyItsOwnSide(int sideIndex)
    {
        float?[] overrides = new float?[4];
        overrides[sideIndex] = 20f;
        NineSlice sut = Create(overrides[0], overrides[1], overrides[2], overrides[3]);

        List<(float Width, float Height)> sizes = SectionSizes(sut);

        // sideIndex: 0 left, 1 top, 2 right, 3 bottom.
        (float Width, float Height) topLeft = sizes[(int)NineSliceSections.TopLeft];
        (float Width, float Height) topRight = sizes[(int)NineSliceSections.TopRight];
        (float Width, float Height) bottomLeft = sizes[(int)NineSliceSections.BottomLeft];
        topLeft.Width.ShouldBe(sideIndex == 0 ? 20f : 5f);
        topLeft.Height.ShouldBe(sideIndex == 1 ? 20f : 5f);
        topRight.Width.ShouldBe(sideIndex == 2 ? 20f : 5f);
        bottomLeft.Height.ShouldBe(sideIndex == 3 ? 20f : 5f);
    }

    [Fact]
    public void SectionSizes_NegativeOverride_BehavesAsZero()
    {
        List<(float Width, float Height)> sizes = SectionSizes(Create(-5f, null, null, null));

        sizes[(int)NineSliceSections.TopLeft].Width.ShouldBe(0f);
        sizes[(int)NineSliceSections.Center].Width.ShouldBe(95f);
    }
}
