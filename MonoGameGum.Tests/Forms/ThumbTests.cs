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
/// Each control has its own thumb type, <see cref="SliderThumb"/> and <see cref="ScrollBarThumb"/>, so
/// one can be restyled without the other (#6808).
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
        ((SliderVisual)slider.Visual).ThumbInstance!.FormsControlAsObject.ShouldBeOfType<SliderThumb>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldBeAThumbUsingThumbTemplate()
    {
        using ThumbTemplateScope scope = new();

        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldBeOfType<ThumbVisual>();
        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject.ShouldBeOfType<ScrollBarThumb>();
    }

    [Fact]
    public void SliderThumb_ShouldUseSliderThumbTemplate_WithoutRestylingScrollBarThumb()
    {
        using ThumbTemplateScope scope = new(typeof(SliderThumb),
            new VisualTemplate((_, _) => new MarkerThumbVisual()));

        Slider slider = new();
        ScrollBar scrollBar = new();

        ((SliderVisual)slider.Visual).ThumbInstance.ShouldBeOfType<MarkerThumbVisual>();
        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldNotBeOfType<MarkerThumbVisual>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldUseScrollBarThumbTemplate_WithoutRestylingSliderThumb()
    {
        using ThumbTemplateScope scope = new(typeof(ScrollBarThumb),
            new VisualTemplate((_, _) => new MarkerThumbVisual()));

        Slider slider = new();
        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldBeOfType<MarkerThumbVisual>();
        ((SliderVisual)slider.Visual).ThumbInstance.ShouldNotBeOfType<MarkerThumbVisual>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldFallBackToStockThumb_WhenThumbTemplateIsNotButtonVisual()
    {
        using ThumbTemplateScope scope = new(
            new VisualTemplate((_, _) => new InteractiveGue(new RenderingLibrary.Graphics.InvisibleRenderable())));

        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.ShouldBeOfType<ThumbVisual>();
        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject.ShouldBeOfType<ScrollBarThumb>();
    }

    [Fact]
    public void ScrollBarThumb_ShouldBeAScrollBarThumb_WhenNoThumbTemplateIsRegistered()
    {
        ScrollBar scrollBar = new();

        ((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject.ShouldBeOfType<ScrollBarThumb>();
    }

    [Fact]
    public void SliderThumb_ShouldBeASliderThumb_WhenNoThumbTemplateIsRegistered()
    {
        Slider slider = new();

        ((SliderVisual)slider.Visual).ThumbInstance!.FormsControlAsObject.ShouldBeOfType<SliderThumb>();
    }

    /// <summary>A ButtonVisual subclass a test registers so it can tell which template built a thumb.</summary>
    private sealed class MarkerThumbVisual : ButtonVisual
    {
        public MarkerThumbVisual() : base(tryCreateFormsObject: false) { }
    }

    /// <summary>Registers a Thumb template for the test and removes it afterwards.</summary>
    private sealed class ThumbTemplateScope : IDisposable
    {
        private readonly Type _thumbType;

        public ThumbTemplateScope(VisualTemplate? template = null) : this(typeof(Thumb), template) { }

        public ThumbTemplateScope(Type thumbType, VisualTemplate? template = null)
        {
            _thumbType = thumbType;
            FrameworkElement.DefaultFormsTemplates[thumbType] = template
                ?? new VisualTemplate((_, createForms) => new ThumbVisual(tryCreateFormsObject: createForms));
        }

        public void Dispose() => FrameworkElement.DefaultFormsTemplates.Remove(_thumbType);
    }
}
