using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

#if SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

// Issue #4506 — SVG gallery, shared by MonoGameGumInCode and SilkNetGumSample via
// <Compile Include ... Link>. Unlike the other shared screens there is no RAYLIB branch and the
// file is NOT linked into Samples/raylib/GumTest.csproj: raylib has no SVG support at all (see
// issue #4505), so there is nothing to mirror rather than a gap to gate.
//
// Both backends expose the same type name, Gum.GueDeriving.SvgRuntime, from different assemblies —
// SkiaGum's wraps a Skia VectorSprite, MonoGameGumShapes'/KniGumShapes' wraps an Apos.Shapes
// ShapeSvg — so this screen needs no per-backend type alias. It sticks to the properties both
// carry (SourceFile, Width/Height + their units, Rotation); the Skia runtime's color/tint
// properties have no Apos counterpart on purpose and are not used here.
//
// EXPECTED DIVERGENCE, not a bug: the last section loads a file whose colors come from a CSS
// <style> block. Apos.Shapes ignores CSS style blocks (along with `use`, `text`, clipPath, mask,
// filter and pattern), while Skia's Svg.Skia honors them, so that one drawing is expected to look
// different across the two backends. It is here precisely so the difference is visible instead of
// being discovered in a user's project.
internal class SvgScreen : FrameworkElement
{
    // Both samples resolve loose content against FileManager.RelativeDirectory, which points at the
    // MGCB Content root on MonoGame and at the loaded .gumx's folder on SilkNet — so a bare file
    // name works on both, the same convention NineSliceScreen's SourceFileName uses.
    private const string DemoSvg = "GumSvgDemo.svg";
    private const string CssStyledSvg = "gum-logo-normal.svg";

    public SvgScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.AddChild(left);
        root.AddChild(right);

        left.AddChild(BuildSection(
            "Sizes (width drives height via MaintainFileAspectRatio)",
            BuildSizeRow()));
        left.AddChild(BuildSection(
            "Rotation (0, 15, 45 degrees, around the top-left corner)",
            BuildRotationRow()));

        right.AddChild(BuildSection(
            "Absolute width and height (square box, 2:1 file - squashed)",
            BuildSquashedRow()));
        right.AddChild(BuildSection(
            "CSS <style> colors: honored on Skia, ignored by Apos.Shapes",
            BuildCssStyledRow()));
    }

    private static ContainerRuntime BuildColumn()
    {
        ContainerRuntime column = new();
        column.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        column.StackSpacing = 20;
        column.WidthUnits = DimensionUnitType.Absolute;
        column.Width = 460;
        column.HeightUnits = DimensionUnitType.RelativeToChildren;
        return column;
    }

    private static ContainerRuntime BuildSection(string label, ContainerRuntime content)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 6;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;

        TextRuntime text = new();
        text.Text = label;
        section.AddChild(text);
        section.AddChild(content);

        return section;
    }

    private static ContainerRuntime BuildRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        return row;
    }

    // Width-driven: HeightUnits stays at the runtime's MaintainFileAspectRatio default, so the
    // 2:1 viewBox should produce a half-height box at every width. A backend that ignored
    // IAspectRatio would show square-ish drawings here.
    private static ContainerRuntime BuildSizeRow()
    {
        ContainerRuntime row = BuildRow();

        foreach (var width in new float[] { 60, 120, 200 })
        {
            SvgRuntime svg = new();
            svg.SourceFile = DemoSvg;
            svg.Width = width;
            row.AddChild(svg);
        }

        return row;
    }

    private static ContainerRuntime BuildRotationRow()
    {
        ContainerRuntime row = BuildRow();

        foreach (var rotation in new float[] { 0, 15, 45 })
        {
            // Rotation pivots on the top-left corner, so give each cell a container of its own —
            // otherwise a rotated drawing overlaps the next one in the stack.
            ContainerRuntime cell = new();
            cell.WidthUnits = DimensionUnitType.Absolute;
            cell.HeightUnits = DimensionUnitType.Absolute;
            cell.Width = 150;
            cell.Height = 130;

            SvgRuntime svg = new();
            svg.SourceFile = DemoSvg;
            svg.Width = 120;
            svg.Rotation = rotation;
            svg.X = 10;
            svg.Y = 10;
            cell.AddChild(svg);

            row.AddChild(cell);
        }

        return row;
    }

    // Both dimensions absolute, with Width deliberately set to a value the file's 2:1 aspect ratio
    // does NOT agree with, so the square boxes squash the drawing. Both backends fill the box
    // exactly: Skia computes scaleX and scaleY independently in VectorSprite.Render, and
    // Apos.Shapes reaches the same result by re-opening the ShapeBatch with a stretching view
    // matrix, since its DrawSvg takes a single em size (one em = the viewBox's height) - #4509.
    private static ContainerRuntime BuildSquashedRow()
    {
        ContainerRuntime row = BuildRow();

        foreach (var size in new float[] { 40, 70, 100 })
        {
            SvgRuntime svg = new();
            svg.SourceFile = DemoSvg;
            svg.HeightUnits = DimensionUnitType.Absolute;
            svg.Height = size;
            svg.WidthUnits = DimensionUnitType.Absolute;
            svg.Width = size; // intentionally square, i.e. inconsistent with the 2:1 file
            row.AddChild(svg);
        }

        return row;
    }

    private static ContainerRuntime BuildCssStyledRow()
    {
        ContainerRuntime row = BuildRow();

        SvgRuntime svg = new();
        svg.SourceFile = CssStyledSvg;
        svg.Width = 140;
        row.AddChild(svg);

        return row;
    }
}
