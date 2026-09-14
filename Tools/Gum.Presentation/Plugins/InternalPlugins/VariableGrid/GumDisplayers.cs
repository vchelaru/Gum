namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Framework-neutral identities of the Gum-specific Variables tab editors (the unit, alignment, and
/// origin toggles, the color and corner-radius composites, and the remove-variable row). Assigned to
/// <c>PreferredDisplayer</c> by the headless grid code; each head registers its own control for each
/// key in its <see cref="WpfDataUi.DisplayerRegistry"/> (see <see cref="IVariableGridHead"/>).
/// </summary>
public static class GumDisplayers
{
    /// <summary>A color swatch for the Red/Green/Blue composite.</summary>
    public sealed class Color { private Color() { } }

    /// <summary>The linked/unlinked corner-radius composite.</summary>
    public sealed class CornerRadius { private CornerRadius() { } }

    /// <summary>A button that removes the row's variable from its category.</summary>
    public sealed class RemoveButton { private RemoveButton() { } }

    /// <summary>Toggle buttons for <c>TextOverflowVerticalMode</c>.</summary>
    public sealed class TextOverflowVerticalMode { private TextOverflowVerticalMode() { } }

    /// <summary>Toggle buttons for <c>TextOverflowHorizontalMode</c>.</summary>
    public sealed class TextOverflowHorizontalMode { private TextOverflowHorizontalMode() { } }

    /// <summary>Toggle buttons for <c>ChildrenLayout</c>.</summary>
    public sealed class ChildrenLayout { private ChildrenLayout() { } }

    /// <summary>Toggle buttons for <c>WidthUnits</c>.</summary>
    public sealed class WidthUnits { private WidthUnits() { } }

    /// <summary>Toggle buttons for <c>HeightUnits</c>.</summary>
    public sealed class HeightUnits { private HeightUnits() { } }

    /// <summary>Toggle buttons for <c>XUnits</c> and the horizontal gradient units.</summary>
    public sealed class XUnits { private XUnits() { } }

    /// <summary>Toggle buttons for <c>YUnits</c> and the vertical gradient units.</summary>
    public sealed class YUnits { private YUnits() { } }

    /// <summary>Toggle buttons for a text <c>VerticalAlignment</c>.</summary>
    public sealed class TextVerticalAlignment { private TextVerticalAlignment() { } }

    /// <summary>Toggle buttons for <c>YOrigin</c>.</summary>
    public sealed class YOrigin { private YOrigin() { } }

    /// <summary>Toggle buttons for a text <c>HorizontalAlignment</c>.</summary>
    public sealed class TextHorizontalAlignment { private TextHorizontalAlignment() { } }

    /// <summary>Toggle buttons for <c>XOrigin</c>.</summary>
    public sealed class XOrigin { private XOrigin() { } }
}
