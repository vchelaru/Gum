using Gum.DataTypes;

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// A thin, inert horizontal line intended as a ListBox / ItemsControl decoration (see
/// <c>ItemsControl.AddDecoration</c> and the <c>InsertDecoration*</c> methods). It is a plain
/// visual with no forms control, so it is never added to Items or ListBoxItems and can never be
/// selected. By default it fills the parent's width, is 2 pixels tall, and is a muted gray; tune it
/// via the inherited <see cref="RectangleRuntime"/> members (the <c>Fill*</c> channels, Height,
/// etc.). See issue #3305.
/// </summary>
public class ListBoxSeparator : RectangleRuntime
{
    public ListBoxSeparator()
    {
        // Fill the parent's width (Width 0 with RelativeToParent == match parent), 2px tall.
        WidthUnits = DimensionUnitType.RelativeToParent;
        Width = 0;
        HeightUnits = DimensionUnitType.Absolute;
        Height = 2;

        // A filled bar, no outline.
        StrokeWidth = 0;
        IsFilled = true;
        FillRed = 128;
        FillGreen = 128;
        FillBlue = 128;
        FillAlpha = 255;
    }
}
