using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Slider and ScrollBar thumbs are <see cref="Thumb"/>s: buttons that a theme can restyle on their
/// own, by registering a template for <see cref="Thumb"/>, without restyling every Button (#6165).
/// </summary>
public class ThumbTests : BaseTestClass
{
    [Fact]
    public void Constructor_ShouldLookLikeAButton_WhenNoThumbTemplateIsRegistered()
    {
        Thumb thumb = new();

        thumb.Visual.ShouldBeOfType<ButtonVisual>();
        thumb.Visual.FormsControlAsObject.ShouldBeOfType<Thumb>();
    }

    [Fact]
    public void Constructor_ShouldUseThumbTemplate_WhenRegistered()
    {
        using ThumbTemplateScope scope = new();

        Thumb thumb = new();

        thumb.Visual.ShouldBeOfType<ThumbVisual>();
    }

    [Fact]
    public void SliderThumb_ShouldBeAThumbUsingThumbTemplate()
    {
        using ThumbTemplateScope scope = new();

        Slider slider = new();

        ((SliderVisual)slider.Visual).ThumbInstance.ShouldBeOfType<ThumbVisual>();
        ((SliderVisual)slider.Visual).ThumbInstance!.FormsControlAsObject.ShouldBeOfType<Thumb>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldBeAThumbUsingThumbTemplate()
    {
        using ThumbTemplateScope scope = new();

        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldBeOfType<ThumbVisual>();
        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject.ShouldBeOfType<Thumb>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldFallBackToStockThumb_WhenThumbTemplateIsNotButtonVisual()
    {
        using ThumbTemplateScope scope = new(
            new VisualTemplate((_, _) => new InteractiveGue(new RenderingLibrary.Graphics.InvisibleRenderable())));

        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldBeOfType<ThumbVisual>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldBeAThumb_WhenNoThumbTemplateIsRegistered()
    {
        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject.ShouldBeOfType<Thumb>();
    }

    /// <summary>Registers a Thumb template for the test and removes it afterwards.</summary>
    private sealed class ThumbTemplateScope : IDisposable
    {
        public ThumbTemplateScope(VisualTemplate? template = null)
        {
            FrameworkElement.DefaultFormsTemplates[typeof(Thumb)] = template
                ?? new VisualTemplate((_, createForms) => new ThumbVisual(tryCreateFormsObject: createForms));
        }

        public void Dispose() => FrameworkElement.DefaultFormsTemplates.Remove(typeof(Thumb));
    }
}
