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
}
