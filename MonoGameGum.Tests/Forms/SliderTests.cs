using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Gum.GueDeriving;
using MonoGameGum.Input;
using Gum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class SliderTests : BaseTestClass
{
    [Fact]
    public void UpdateState_ShouldNotThrow_WhenButtonTemplateIsNotButtonVisual()
    {
        // Mirrors a real crash: if an app overrides FrameworkElement.DefaultFormsTemplates[typeof(Button)]
        // with a visual that isn't a V3 ButtonVisual (e.g. a fully custom InteractiveGue - a documented
        // pattern for customizing button appearance), any composite control that builds an internal
        // Button sub-part - like Slider's thumb - ends up with a null ThumbInstance (the "as ButtonVisual"
        // cast in SliderVisual's constructor fails), and SetValuesForState used to NRE dereferencing it
        // unguarded whenever the slider's enabled/focused state changed.
        var previousSliderTemplate = FrameworkElement.DefaultFormsTemplates[typeof(Slider)];
        var previousButtonTemplate = FrameworkElement.DefaultFormsTemplates[typeof(Button)];
        try
        {
            FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
                new VisualTemplate((_, createForms) => new SliderVisual(tryCreateFormsObject: createForms));
            FrameworkElement.DefaultFormsTemplates[typeof(Button)] = new VisualTemplate((_, createForms) =>
            {
                var customButtonVisual = new InteractiveGue(new RenderingLibrary.Graphics.InvisibleRenderable());
                var textInstance = new TextRuntime { Name = "TextInstance" };
                customButtonVisual.Children.Add(textInstance);
                return customButtonVisual;
            });

            Slider slider = new();
            var visual = (SliderVisual)slider.Visual;
            visual.ThumbInstance.ShouldBeNull("because the overridden Button template isn't a ButtonVisual");

            Should.NotThrow(() => slider.IsEnabled = false);
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates[typeof(Slider)] = previousSliderTemplate;
            FrameworkElement.DefaultFormsTemplates[typeof(Button)] = previousButtonTemplate;
        }
    }

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        Slider slider = new();
        InteractiveGue visual = slider.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        foreach (var child in children)
        {
            if (child != slider.Track)
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

        slider.Track!.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Slider sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Value_ShouldBeLimited_WhenOutsideOfMinimumAndMaximum()
    {
        Slider slider = new ()
        {
            Minimum = 0,
            Maximum = 100
        };
        slider.Value = -10;
        slider.Value.ShouldBe(0);
        slider.Value = 110;
        slider.Value.ShouldBe(100);
    }

    [Fact]
    public void Value_ShouldBeAdjusted_WhenMinimumAndMaximumChange()
    {
        Slider slider = new ()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        slider.Value = 10;
        slider.Minimum = 25;
        slider.Value.ShouldBe(25);

        slider.Value = 90;
        slider.Maximum = 75;
        slider.Maximum.ShouldBe(75);
    }

    [Fact]
    public void IsFocused_ShouldUpdateState()
    {
        Slider slider = new();

        var visual = slider.Visual;

        var focusedState = visual.Categories[Slider.SliderCategoryName]
            .States.First(item => item.Name == FrameworkElement.FocusedStateName);

        focusedState.Clear();
        bool wasSet = false;
        focusedState.Apply = () =>
        {
            wasSet = true;
        };

        slider.IsFocused = true;

        wasSet.ShouldBeTrue();
    }

    [Fact]
    public void Slider_RightArrowPressed_IncreasesValue()
    {
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 100,
            SmallChange = 5,
            Value = 50
        };

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Right)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        slider.OnFocusUpdate();

        slider.Value.ShouldBe(55);
    }
}
