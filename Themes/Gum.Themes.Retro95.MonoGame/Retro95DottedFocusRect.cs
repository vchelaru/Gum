using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Retro95;

/// <summary>
/// The canonical Win95 1-pixel dotted focus rectangle, built from many 1×1
/// <see cref="ColoredRectangleRuntime"/> dots spaced 2 px apart. The runtime has no
/// dotted-stroke primitive, so we materialize each dot.
/// <para>
/// Owns its own <see cref="ContainerRuntime"/> wrapper which is added to the host visual
/// in the constructor. The wrapper fills the host (or a manually-set Width/Height) and
/// is positioned at <c>PixelsFromMiddle / Center</c> by default — callers can repoint
/// it (e.g. CheckBox/RadioButton, which wrap only the label area) before showing.
/// </para>
/// <para>
/// Call <see cref="Show"/> from the host's focused-state callback; call <see cref="Hide"/>
/// from non-focused states. <see cref="Show"/> regenerates the dots from the wrapper's
/// current size so the indicator reflects size changes the consumer made after
/// construction. Cheap (constant time per state change relative to dot count).
/// </para>
/// </summary>
public sealed class Retro95DottedFocusRect
{
    private const float DotSize = 1f;
    /// <summary>Pixels between dot centers. 2 = 1 px dot + 1 px gap, the Win95 pattern.</summary>
    private const float DotPitch = 2f;

    private readonly ContainerRuntime _container;
    private readonly List<ColoredRectangleRuntime> _dots = new List<ColoredRectangleRuntime>();
    private readonly float _inset;

    /// <summary>The wrapper container holding the dots. Adjust its X/Y/Width/Height
    /// before <see cref="Show"/> to scope the focus rect to a sub-region of the host.</summary>
    public ContainerRuntime Container => _container;

    /// <summary>Creates the dotted focus rect attached to <paramref name="host"/>. The
    /// container fills the host by default; reposition / resize before <see cref="Show"/>.</summary>
    /// <param name="host">Visual that owns the focus rect.</param>
    /// <param name="inset">Pixels in from the container edge to draw the dotted line.</param>
    public Retro95DottedFocusRect(GraphicalUiElement host, float inset = 0f)
    {
        _inset = inset;
        _container = new ContainerRuntime();
        _container.Name = "Retro95FocusRect";
        // ContainerRuntime ctor sets HasEvents=true; we want this purely visual.
        _container.HasEvents = false;
        _container.X = 0; _container.Y = 0;
        _container.XUnits = GeneralUnitType.PixelsFromMiddle;
        _container.YUnits = GeneralUnitType.PixelsFromMiddle;
        _container.XOrigin = HorizontalAlignment.Center;
        _container.YOrigin = VerticalAlignment.Center;
        _container.Width = 0; _container.Height = 0;
        _container.WidthUnits = DimensionUnitType.RelativeToParent;
        _container.HeightUnits = DimensionUnitType.RelativeToParent;
        _container.Visible = false;
        host.AddChild(_container);
    }

    public void Show()
    {
        RegenerateDots();
        _container.Visible = true;
    }

    public void Hide()
    {
        _container.Visible = false;
    }

    private void RegenerateDots()
    {
        foreach (ColoredRectangleRuntime dot in _dots)
        {
            dot.Parent = null;
        }
        _dots.Clear();

        // GetAbsoluteWidth() / GetAbsoluteHeight() resolves whatever Units the
        // container is using (Absolute, RelativeToParent, RelativeToChildren)
        // into pixels, so this helper works regardless of how the host sized us.
        float w = _container.GetAbsoluteWidth() - (_inset * 2f);
        float h = _container.GetAbsoluteHeight() - (_inset * 2f);
        if (w <= 0 || h <= 0) return;

        int horizontalCount = (int)(w / DotPitch);
        for (int i = 0; i < horizontalCount; i++)
        {
            float x = -w / 2f + i * DotPitch;
            AddDot(x, 0, top: true, vertical: false);
            AddDot(x, 0, top: false, vertical: false);
        }

        int verticalCount = (int)((h - DotPitch * 2f) / DotPitch);
        for (int i = 0; i < verticalCount; i++)
        {
            float y = -h / 2f + DotPitch + i * DotPitch;
            AddDot(0, y, top: true, vertical: true);
            AddDot(0, y, top: false, vertical: true);
        }
    }

    private void AddDot(float xOffset, float yOffset, bool top, bool vertical)
    {
        ColoredRectangleRuntime dot = new ColoredRectangleRuntime();
        dot.Name = "Retro95FocusDot";
        dot.Color = Retro95Colors.Text;
        dot.Width = DotSize;
        dot.Height = DotSize;
        dot.WidthUnits = DimensionUnitType.Absolute;
        dot.HeightUnits = DimensionUnitType.Absolute;

        if (!vertical)
        {
            dot.X = xOffset;
            dot.XUnits = GeneralUnitType.PixelsFromMiddle;
            dot.XOrigin = HorizontalAlignment.Left;
            dot.Y = top ? _inset : -_inset;
            dot.YUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            dot.YOrigin = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        }
        else
        {
            dot.X = top ? _inset : -_inset;
            dot.XUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            dot.XOrigin = top ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            dot.Y = yOffset;
            dot.YUnits = GeneralUnitType.PixelsFromMiddle;
            dot.YOrigin = VerticalAlignment.Top;
        }

        _container.AddChild(dot);
        _dots.Add(dot);
    }
}
