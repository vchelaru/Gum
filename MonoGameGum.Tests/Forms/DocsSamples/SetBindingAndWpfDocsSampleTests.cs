using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the binding samples on docs/code/controls/frameworkelement/setbinding.md, docs/code/about/for-wpf-users.md
/// and docs/code/controls/textbox.md.
/// </summary>
public class SetBindingAndWpfDocsSampleTests : BaseTestClass
{
    public class PlayerViewModel : ViewModel
    {
        public string PlayerName
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    public class GameViewModel : ViewModel
    {
        // Regular ViewModel events can be used in SetBinding
        public event Action EnemySpawned;

        // If the event needs to be raised externally (outside of the ViewModel)
        // then a method for raising the event is needed:
        public void RaiseEnemySpawned() => EnemySpawned?.Invoke();
    }

    class EnemyWatcher : FrameworkElement
    {
        public int SpawnCount;

        public EnemyWatcher() : base(new ContainerRuntime())
        {
            CustomInitialize();
        }

        void CustomInitialize()
        {
            SetBinding(
                nameof(HandleEnemySpawned),
                nameof(GameViewModel.EnemySpawned));
        }

        void HandleEnemySpawned()
        {
            SpawnCount++;
        }
    }

    public class MyViewModel : ViewModel
    {
        public string ButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    class UpperConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) => (value as string)?.ToUpperInvariant();
        public object? ConvertBack(object? value, Type sourceType, object? parameter) => (value as string)?.ToLowerInvariant();
    }

    class MyCustomControl : FrameworkElement
    {
        public MyCustomControl() : base(new ContainerRuntime()) { }

        string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                PushValueToViewModel();
            }
        }
    }

    class TextViewModel : ViewModel
    {
        public string Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    [Fact]
    public void SetBindingPage_TextBoxPlayerName_TwoWay()
    {
        TextBox TextBoxInstance = new();
        PlayerViewModel viewModel = new() { PlayerName = "Ada" };
        TextBoxInstance.BindingContext = viewModel;

        TextBoxInstance.SetBinding(
            nameof(TextBoxInstance.Text),
            nameof(PlayerViewModel.PlayerName));

        TextBoxInstance.Text.ShouldBe("Ada");
        TextBoxInstance.Text = "Bob";
        viewModel.PlayerName.ShouldBe("Bob");
    }

    [Fact]
    public void SetBindingPage_BindingBeforeContext_AppliesWhenContextArrives()
    {
        TextBox textBox = new();
        textBox.SetBinding(nameof(TextBox.Text), nameof(PlayerViewModel.PlayerName));

        textBox.BindingContext = new PlayerViewModel { PlayerName = "Ada" };

        textBox.Text.ShouldBe("Ada");
    }

    [Fact]
    public void SetBindingPage_EventBinding_CallsHandler()
    {
        EnemyWatcher watcher = new();
        GameViewModel viewModel = new();
        watcher.BindingContext = viewModel;

        viewModel.RaiseEnemySpawned();

        watcher.SpawnCount.ShouldBe(1);
    }

    [Fact]
    public void WpfPage_StringLambdaAndBindingObject_Work()
    {
        MyViewModel MyViewModel = new() { ButtonText = "Go", Name = "ada" };
        Button MyButton = new();
        MyButton.BindingContext = MyViewModel;
        MyButton.SetBinding(nameof(Button.Text), nameof(MyViewModel.ButtonText));
        MyButton.Text.ShouldBe("Go");

        Button lambdaButton = new() { BindingContext = MyViewModel };
        lambdaButton.SetBinding<MyViewModel>(nameof(Button.Text), vm => vm.ButtonText);
        lambdaButton.Text.ShouldBe("Go");

        var myConverter = new UpperConverter();
        TextBox MyTextBox = new() { BindingContext = MyViewModel };
        MyTextBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(MyViewModel.Name))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Converter = myConverter,
            FallbackValue = "(none)"
        });
        MyTextBox.Text.ShouldBe("ADA");
        MyTextBox.Text = "BOB";
        MyViewModel.Name.ShouldBe("bob");
    }

    [Fact]
    public void WpfPage_MistypedNames_ReportOrThrow()
    {
        Action<string>? previous = ElementSaveExtensions.ReportPropertyAssignmentError;
        List<string> reports = new();
        ElementSaveExtensions.ReportPropertyAssignmentError = reports.Add;
        try
        {
            Button myButton = new() { BindingContext = new MyViewModel() };

            // Mistyped view-model path: one [Gum] report, no exception.
            myButton.SetBinding(nameof(Button.Text), "ButtonTxt");
            reports.Count.ShouldBe(1);

            // Mistyped UI property: the page's own example.
            Exception? thrown = Record.Exception(() => myButton.SetBinding("Txt", "ButtonText"));
            thrown.ShouldBeNull("SetBinding(\"Txt\", ...) result: " + thrown?.GetType().Name + " " + thrown?.Message + " reports: " + string.Join(" | ", reports));
        }
        finally
        {
            ElementSaveExtensions.ReportPropertyAssignmentError = previous;
        }
    }

    [Fact]
    public void WpfPage_CustomPropertyWithPushValueToViewModel_IsTwoWay()
    {
        MyCustomControl control = new();
        TextViewModel viewModel = new() { Text = "a" };
        control.BindingContext = viewModel;
        control.SetBinding(nameof(MyCustomControl.Text), nameof(TextViewModel.Text));
        control.Text.ShouldBe("a");

        control.Text = "b";

        viewModel.Text.ShouldBe("b");
    }

    [Fact]
    public void WpfPage_ListBoxItemsAndVisualTemplate_ItemGetsElementAsContext()
    {
        ObservableCollection<TextViewModel> myObservableCollection = new() { new TextViewModel { Text = "one" } };
        var listBox = new ListBox();
        listBox.BindingContext = new object();
        listBox.Items = myObservableCollection;
        listBox.VisualTemplate = new VisualTemplate(() => new Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual());

        listBox.ListBoxItems.Count.ShouldBe(1);
        listBox.ListBoxItems[0].BindingContext.ShouldBe(myObservableCollection[0]);
    }

    [Fact]
    public void TextBoxPage_LostFocusBinding_UpdatesOnLostFocus()
    {
        TextViewModel viewModel = new() { Text = "start" };
        var mainPanel = new StackPanel();
        mainPanel.AddToRoot();
        mainPanel.BindingContext = viewModel;

        var label = new Label();
        label.SetBinding(nameof(label.Text), nameof(TextViewModel.Text));
        mainPanel.AddChild(label);

        var textBox = new TextBox();
        textBox.Width = 500;
        var binding = new Binding(nameof(TextViewModel.Text))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        textBox.SetBinding(nameof(textBox.Text), binding);
        mainPanel.AddChild(textBox);

        textBox.IsFocused = true;
        textBox.Text = "typed";
        label.Text.ShouldBe("start");

        textBox.IsFocused = false;
        label.Text.ShouldBe("typed");
    }
}
