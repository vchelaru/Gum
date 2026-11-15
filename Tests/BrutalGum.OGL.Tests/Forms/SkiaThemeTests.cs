using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Themes.Neon;
using Shouldly;

namespace BrutalGum.Tests.Forms;

/// <summary>
/// A <c>Gum.Themes.*.Skia</c> package is built against SkiaGum, not a host, so it applies in a BRUTAL app:
/// its visuals derive from the same SkiaGum Forms types that BrutalGum.OGL uses.
/// </summary>
public class SkiaThemeTests : BaseTestClass
{
    private readonly Dictionary<Type, VisualTemplate> _previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
    private readonly Styling _previousStyle = Styling.ActiveStyle;

    public SkiaThemeTests()
    {
        // Apply fills the active style in place; give it a fresh one so Dispose can put the old one back.
        Styling.ActiveStyle = new Styling(spriteSheet: null);
    }

    public override void Dispose()
    {
        FrameworkElement.DefaultFormsTemplates.Clear();
        foreach (KeyValuePair<Type, VisualTemplate> pair in _previousTemplates)
        {
            FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
        }
        Styling.ActiveStyle = _previousStyle;
        base.Dispose();
    }

    [Fact]
    public void Apply_NeonSkiaTheme_ButtonUsesThemeVisual()
    {
        NeonTheme.Apply();

        Button button = new();

        button.Visual.ShouldBeOfType<Gum.Themes.Neon.ButtonVisual>();
    }
}
