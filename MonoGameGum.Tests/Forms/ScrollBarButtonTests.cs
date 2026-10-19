using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The scroll bar's up and down arrows are <see cref="ScrollBarButton"/>s: buttons that a theme can
/// restyle on their own, by registering a template for <see cref="ScrollBarButton"/>, without
/// restyling every Button (#6165).
/// </summary>
public class ScrollBarButtonTests : BaseTestClass
{
    [Fact]
    public void Constructor_ShouldLookLikeAButton_WhenNoTemplateIsRegistered()
    {
        ScrollBarButton button = new();

        button.Visual.ShouldBeOfType<ButtonVisual>();
        button.Visual.FormsControlAsObject.ShouldBeOfType<ScrollBarButton>();
    }

    [Fact]
    public void ScrollBarArrows_ShouldBeScrollBarButtons_WhenNoTemplateIsRegistered()
    {
        ScrollBar scrollBar = new();
        ScrollBarVisual visual = (ScrollBarVisual)scrollBar.Visual;

        visual.UpButtonInstance!.FormsControlAsObject.ShouldBeOfType<ScrollBarButton>();
        visual.DownButtonInstance!.FormsControlAsObject.ShouldBeOfType<ScrollBarButton>();
    }

    [Fact]
    public void ScrollBarArrows_ShouldUseScrollBarButtonTemplate_WhenRegistered()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBarButton)] =
            new VisualTemplate((_, createForms) => new ArrowVisual(tryCreateFormsObject: createForms));
        try
        {
            ScrollBar scrollBar = new();
            ScrollBarVisual visual = (ScrollBarVisual)scrollBar.Visual;

            visual.UpButtonInstance.ShouldBeOfType<ArrowVisual>();
            visual.DownButtonInstance.ShouldBeOfType<ArrowVisual>();
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Remove(typeof(ScrollBarButton));
        }
    }

    private class ArrowVisual : ButtonVisual
    {
        public ArrowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
            : base(fullInstantiation, tryCreateFormsObject: false)
        {
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ScrollBarButton(this);
            }
        }
    }
}
