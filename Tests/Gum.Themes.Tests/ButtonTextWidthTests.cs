using System.Reflection;
using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;

namespace Gum.Themes.Tests;

/// <summary>
/// #7639: every button's text instance is RelativeToParent with a width of 0, or slightly
/// negative where the button reserves margins, so text wider than the button wraps instead of
/// overflowing. The Editor theme is exempt: its buttons are sized to their content
/// (WidthUnits RelativeToChildren), so there is no fixed width to wrap against.
/// </summary>
public class ButtonTextWidthTests
{
    private const float MostNegativeAllowedWidth = -16;

    public static IEnumerable<object[]> ButtonVisualTypes()
    {
        yield return new object[] { typeof(Gum.Forms.DefaultVisuals.V3.ButtonVisual) };
        yield return new object[] { typeof(Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual) };
        yield return new object[] { typeof(Bubblegum.ButtonVisual) };
        yield return new object[] { typeof(Bubblegum.ToggleButtonVisual) };
        yield return new object[] { typeof(DarkPro.ButtonVisual) };
        yield return new object[] { typeof(DarkPro.ToggleButtonVisual) };
        yield return new object[] { typeof(ForestGlade.ButtonVisual) };
        yield return new object[] { typeof(ForestGlade.ToggleButtonVisual) };
        yield return new object[] { typeof(Hazard.ButtonVisual) };
        yield return new object[] { typeof(Hazard.ToggleButtonVisual) };
        yield return new object[] { typeof(Meadow.ButtonVisual) };
        yield return new object[] { typeof(Meadow.ToggleButtonVisual) };
        yield return new object[] { typeof(Neon.ButtonVisual) };
        yield return new object[] { typeof(Neon.ToggleButtonVisual) };
        yield return new object[] { typeof(Retro95.ButtonVisual) };
        yield return new object[] { typeof(Retro95.ToggleButtonVisual) };
        yield return new object[] { typeof(Template.ButtonVisual) };
        yield return new object[] { typeof(Template.ToggleButtonVisual) };
        yield return new object[] { typeof(Template.Variants.ButtonVisual) };
    }

    [Theory]
    [MemberData(nameof(ButtonVisualTypes))]
    public void TextInstance_ShouldBeRelativeToParentWithZeroOrSlightlyNegativeWidth(Type visualType)
    {
        object visual = CreateWithDefaults(visualType);
        TextRuntime text = (TextRuntime)visualType.GetProperty("TextInstance")!.GetValue(visual)!;

        text.WidthUnits.ShouldBe(DimensionUnitType.RelativeToParent, visualType.FullName);
        text.Width.ShouldBeLessThanOrEqualTo(0, visualType.FullName);
        text.Width.ShouldBeGreaterThanOrEqualTo(MostNegativeAllowedWidth, visualType.FullName);
    }

    private static object CreateWithDefaults(Type type)
    {
        ConstructorInfo ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        object?[] args = ctor.GetParameters()
            .Select(p => p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();
        return ctor.Invoke(args);
    }
}
