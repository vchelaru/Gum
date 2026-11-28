using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/README.md (Binding and MVVM) and checks what the page says.
/// </summary>
public class BindingAndMvvmDocsSampleTests : BaseTestClass
{
    class ExampleViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    class PlayerViewModel : ViewModel
    {
        private float _health = 100;

        public float Health
        {
            get => _health;
            set => Set(ref _health, value);
        }
    }

    [Fact]
    public void QuickExample_TypingInTextBox_UpdatesLabel()
    {
        var panel = new StackPanel();
        panel.AddToRoot();
        panel.Anchor(Gum.Wireframe.Anchor.Center);

        var textBox = new TextBox();
        panel.AddChild(textBox);
        textBox.Width = 250;

        var mirrorLabel = new Label();
        panel.AddChild(mirrorLabel);

        var viewModel = new ExampleViewModel();
        panel.BindingContext = viewModel;

        textBox.SetBinding(
            nameof(TextBox.Text),
            nameof(ExampleViewModel.Name));

        mirrorLabel.SetBinding(
            nameof(Label.Text),
            nameof(ExampleViewModel.Name));

        textBox.Text = "Ada";

        viewModel.Name.ShouldBe("Ada");
        mirrorLabel.Text.ShouldBe("Ada");
    }

    [Fact]
    public void BackingField_SetToStartingValue_RaisesNothing()
    {
        PlayerViewModel viewModel = new();
        List<string?> raised = new();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.Health = 100;
        raised.ShouldBeEmpty();

        viewModel.Health = 50;
        raised.ShouldBe(new[] { nameof(PlayerViewModel.Health) });
    }
}
