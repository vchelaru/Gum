using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

#pragma warning disable CS0618 // legacy obsolete "Color" is the exact case under test

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Pins the raylib branch of <c>CustomSetPropertyOnRenderable</c>'s legacy "Color" dispatch for
/// <see cref="RectangleRuntime"/>/<see cref="CircleRuntime"/> (#4176 follow-up). The v3 shape
/// properties (IsFilled/FillRed/etc.) were fixed to dispatch on raylib in the first #4176 pass, but
/// "Color" stayed excluded because it constructed an XNA-typed <c>Color</c> the raylib runtime
/// can't hold — a gap only found by testing the fix against a real project (Airpig) that still uses
/// the pre-v3 "Color" property. Fixed by branching the conversion per backend
/// (<c>System.Drawing.Color</c> -&gt; <see cref="Raylib_cs.Color"/> via <c>ColorExtensions.ToRaylib</c>),
/// mirroring the existing raylib Sprite/NineSlice/Text "Color" cases in the same dispatcher.
/// </summary>
public class CustomSetPropertyOnRenderableRectangleCircleColorTests : BaseTestClass
{
    [Fact]
    public void SetPropertyOnRenderable_ColorOnRectangleRuntime_AppliesToStrokeColor()
    {
        RectangleRuntime rectangleRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)rectangleRuntime.RenderableComponent!;
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(255, 10, 200, 30);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, rectangleRuntime, "Color", drawingColor);

        rectangleRuntime.Color.ShouldBe(new Color(10, 200, 30, 255));
    }

    [Fact]
    public void SetPropertyOnRenderable_ColorOnCircleRuntime_AppliesToStrokeColor()
    {
        CircleRuntime circleRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)circleRuntime.RenderableComponent!;
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(255, 40, 50, 60);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, circleRuntime, "Color", drawingColor);

        circleRuntime.Color.ShouldBe(new Color(40, 50, 60, 255));
    }

    // #4169: DropshadowColor is backend-typed the same as "Color" above, and had no dispatch
    // case at all (not just a raylib gap) - see CustomSetPropertyOnRenderable.Dropshadow* cases.
    [Fact]
    public void SetPropertyOnRenderable_DropshadowColorOnRectangleRuntime_AppliesValue()
    {
        RectangleRuntime rectangleRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)rectangleRuntime.RenderableComponent!;
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(200, 70, 80, 90);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, rectangleRuntime, "DropshadowColor", drawingColor);

        rectangleRuntime.DropshadowColor.ShouldBe(new Color(70, 80, 90, 200));
    }

    [Fact]
    public void SetPropertyOnRenderable_DropshadowColorOnCircleRuntime_AppliesValue()
    {
        CircleRuntime circleRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)circleRuntime.RenderableComponent!;
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(210, 15, 25, 35);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, circleRuntime, "DropshadowColor", drawingColor);

        circleRuntime.DropshadowColor.ShouldBe(new Color(15, 25, 35, 210));
    }
}
