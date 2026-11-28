// Sample code is copied from the docs as written, and the docs samples do not use nullable annotations.
#nullable disable warnings
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/binding-custom-view-properties.md.
/// </summary>
public class BindingCustomViewPropertiesDocsSampleTests : BaseTestClass
{
    public class ButtonWithSubtext : Button
    {
        TextRuntime _textRuntime;

        public string Subtext
        {
            get => _textRuntime.Text;
            set
            {
                if (_textRuntime.Text != value)
                {
                    _textRuntime.Text = value;
                }
            }
        }

        public ButtonWithSubtext() : base()
        {
            ButtonVisual visual = (ButtonVisual)Visual;

            _textRuntime = new TextRuntime();
            this.AddChild(_textRuntime);
            _textRuntime.Anchor(Gum.Wireframe.Anchor.Center);
            _textRuntime.Y = 20;
            _textRuntime.Color = Color.Yellow;
        }
    }

    class ExampleViewModel : ViewModel
    {
        public int NumberOfClicks
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(NumberOfClicks))]
        public string ClicksDisplay => $"{NumberOfClicks} click(s)";
    }

    [Fact]
    public void Subtext_BoundToDependsOn_UpdatesOnClick()
    {
        ButtonWithSubtext button = new ();
        button.AddToRoot();
        button.Anchor(Anchor.Center);
        button.Text = "Click me please";

        ExampleViewModel viewModel = new ();
        button.BindingContext = viewModel;
        button.SetBinding(
            nameof(button.Subtext),
            nameof(viewModel.ClicksDisplay));

        button.Click += (_, _) =>
        {
            viewModel.NumberOfClicks++;
        };

        button.Subtext.ShouldBe("0 click(s)");
        button.PerformClick();
        button.Subtext.ShouldBe("1 click(s)");
    }
}
