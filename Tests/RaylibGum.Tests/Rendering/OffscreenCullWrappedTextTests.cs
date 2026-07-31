using Gum.DataTypes;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #4144: raylib's <c>Renderer</c> does not route through the shared
/// <see cref="HierarchicalOrderer"/> — it has its own separate recursive draw walk
/// (<c>DrawGumRecursively</c>) with its own copy of the off-screen cull check (#2998). A
/// multi-line Forms TextBox scrolls its Text by moving Y while Height stays fixed to the visible
/// box, so its declared bounds can drift entirely outside an on-screen clip even though its
/// actual wrapped content (<see cref="IWrappedText.WrappedTextHeight"/>) still overlaps it. These
/// tests drive a real raylib draw pass (mirroring <see cref="RendererDrawCallCountTests"/>) to
/// prove the cull still draws such a Text instead of skipping it.
///
/// clipParent is deliberately NOT at Y=0: GetScissorRectangleFor clamps its result to the
/// camera's overall client bounds (0..600 here), and a clip sitting exactly at that boundary
/// makes an off-clip renderable's declared bounds collapse to a degenerate rect AT the same
/// boundary — which then spuriously reads as "not fully outside" regardless of any fix. Placing
/// the clip in the middle of the camera avoids that false negative.
/// </summary>
public class OffscreenCullWrappedTextTests : BaseTestClass
{
    private static int DrawAndCountDrawCalls()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
    }

    [Fact]
    public void Draw_ScrolledWrappedTextInsideOnScreenClip_IsNotCulled()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime clipParent = new();
        clipParent.Y = 200;
        clipParent.Width = 200;
        clipParent.WidthUnits = DimensionUnitType.Absolute;
        clipParent.Height = 150;
        clipParent.HeightUnits = DimensionUnitType.Absolute;
        clipParent.ClipsChildren = true;

        TextRuntime text = new();
        text.Width = 190;
        text.WidthUnits = DimensionUnitType.Absolute;
        text.Height = 150; // fixed to the visible box, like TextInstance's RelativeToParent Height
        text.HeightUnits = DimensionUnitType.Absolute;
        text.Text = string.Join(" ", Enumerable.Range(1, 60).Select(i => $"word{i}")); // wraps to many lines
        text.Y = -300; // scrolled past many wrapped lines (absolute Y: 200 - 300 = -100)
        clipParent.Children.Add(text);

        GumService.Default.Root.Children.Add(clipParent);
        GumService.Default.Root.UpdateLayout();

        int withScrolledText = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        (withScrolledText - baseline).ShouldBeGreaterThan(0,
            "because the Text's actual wrapped content still overlaps the on-screen clip even though its own declared (fixed-Height) bounds do not");
    }

    // Regression guard: an ordinary (non-text) child genuinely scrolled off an on-screen clip —
    // the ListBox/ScrollViewer case the cull exists for — must still be culled.
    [Fact]
    public void Draw_ScrolledPlainRectangleOutsideOnScreenClip_IsStillCulled()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime clipParent = new();
        clipParent.Y = 200;
        clipParent.Width = 200;
        clipParent.WidthUnits = DimensionUnitType.Absolute;
        clipParent.Height = 150;
        clipParent.HeightUnits = DimensionUnitType.Absolute;
        clipParent.ClipsChildren = true;

        ColoredRectangleRuntime scrolledOffItem = new();
        scrolledOffItem.Width = 190;
        scrolledOffItem.Height = 80;
        scrolledOffItem.Y = 250; // well past the 150px-tall clip band (absolute Y: 200 + 250 = 450)
        clipParent.Children.Add(scrolledOffItem);

        GumService.Default.Root.Children.Add(clipParent);
        GumService.Default.Root.UpdateLayout();

        int withScrolledOffItem = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        (withScrolledOffItem - baseline).ShouldBe(0,
            "because a plain renderable genuinely scrolled off the on-screen clip must still be culled");
    }
}
