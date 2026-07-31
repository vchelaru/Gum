using System.Reflection;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Themes.Tests;

/// <summary>
/// Two <see cref="CircleRuntime"/> children sized and positioned identically — one filled,
/// one stroked — render as two independently-antialiased edges at the same nominal radius,
/// producing a visible seam. <see cref="CircleRuntime"/> supports fill + stroke simultaneously
/// on a single instance (see <c>FillRadiusInset</c>, issue #2834), so any circle needing both
/// should merge onto one runtime rather than stacking two.
/// </summary>
public class CircleSeamTests
{
    public static IEnumerable<object[]> CircleVisualTypes()
    {
        yield return new object[] { typeof(Bubblegum.SliderThumbVisual), "Bubblegum.SliderThumbVisual" };
        yield return new object[] { typeof(Neon.SliderThumbVisual), "Neon.SliderThumbVisual" };
        yield return new object[] { typeof(ForestGlade.SliderThumbVisual), "ForestGlade.SliderThumbVisual" };
        yield return new object[] { typeof(Retro95.RadioButtonVisual), "Retro95.RadioButtonVisual" };
        yield return new object[] { typeof(Neon.RadioButtonVisual), "Neon.RadioButtonVisual" };
        yield return new object[] { typeof(Meadow.RadioButtonVisual), "Meadow.RadioButtonVisual" };
        yield return new object[] { typeof(Hazard.RadioButtonVisual), "Hazard.RadioButtonVisual" };
        yield return new object[] { typeof(Bubblegum.RadioButtonVisual), "Bubblegum.RadioButtonVisual" };
        yield return new object[] { typeof(ForestGlade.RadioButtonVisual), "ForestGlade.RadioButtonVisual" };
        yield return new object[] { typeof(DarkPro.RadioButtonVisual), "DarkPro.RadioButtonVisual" };
        yield return new object[] { typeof(Template.RadioButtonVisual), "Template.RadioButtonVisual" };
        yield return new object[] { typeof(Template.Variants.RadioButtonVisual), "Template.Variants.RadioButtonVisual" };
    }

    [Theory]
    [MemberData(nameof(CircleVisualTypes))]
    public void Visual_HasNoTwoCirclesSharingIdenticalBounds(Type visualType, string name)
    {
        GraphicalUiElement visual = (GraphicalUiElement)CreateWithDefaults(visualType);
        List<CircleRuntime> circles = visual.Children
            .OfType<CircleRuntime>()
            .ToList();

        var seams = circles
            .GroupBy(c => (c.X, c.XUnits, c.Y, c.YUnits, c.Width, c.WidthUnits, c.Height, c.HeightUnits, c.XOrigin, c.YOrigin))
            .Where(g => g.Count() > 1)
            .ToList();

        seams.ShouldBeEmpty(
            $"{name} has {(seams.Count > 0 ? seams[0].Count() : 0)} CircleRuntime children sharing identical bounds — " +
            "two independently-antialiased circles at the same radius render a visible seam. Merge fill + stroke onto one CircleRuntime.");
    }

    // Activator.CreateInstance(Type) requires a true zero-arg constructor; C# optional
    // parameters (the RadioButtonVisual ctors' `bool fullInstantiation = true, ...`) don't
    // qualify. Resolve the constructor and supply each parameter's default value instead.
    private static object CreateWithDefaults(Type type)
    {
        ConstructorInfo ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        object?[] args = ctor.GetParameters()
            .Select(p => p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();
        return ctor.Invoke(args);
    }
}
