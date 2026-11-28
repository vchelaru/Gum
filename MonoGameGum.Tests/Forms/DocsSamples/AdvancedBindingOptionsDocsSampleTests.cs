using System;
using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/advanced-binding-options.md and checks what the page says they do.
/// </summary>
public class AdvancedBindingOptionsDocsSampleTests : BaseTestClass
{
    public class PlayerViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    public class ItemViewModel : ViewModel
    {
        public string Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    public class TeamViewModel : ViewModel
    {
        public ObservableCollection<PlayerViewModel> Members { get; } = new();
    }

    public class MyViewModel : ViewModel
    {
        public string PlayerName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LabelText
        {
            get => Get<string>();
            set => Set(value);
        }

        public PlayerViewModel? CurrentPlayer
        {
            get => Get<PlayerViewModel?>();
            set => Set(value);
        }

        public ObservableCollection<ItemViewModel> Items { get; } = new();

        public TeamViewModel? CurrentTeam
        {
            get => Get<TeamViewModel?>();
            set => Set(value);
        }

        public bool IsActive
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float Health
        {
            get => Get<float>();
            set => Set(value);
        }
    }

    public class ScoreViewModel : ViewModel
    {
        public int Score
        {
            get => Get<int>();
            set => Set(value);
        }

        public int? OptionalScore
        {
            get => Get<int?>();
            set => Set(value);
        }

        public string Mode
        {
            get => Get<string>();
            set => Set(value);
        }

        public int ModeNumber
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    class BoolToYesNoConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter)
        {
            return value is true ? "Yes" : "No";
        }

        public object? ConvertBack(object? value, Type sourceType, object? parameter)
        {
            return value is string s && s.Equals("Yes",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LambdaBinding_Typed_ShowsViewModelValue()
    {
        TextBox textBox = new();
        MyViewModel viewModel = new() { PlayerName = "Ada" };
        textBox.BindingContext = viewModel;

        textBox.SetBinding<MyViewModel>(
            nameof(TextBox.Text),
            vm => vm.PlayerName);

        textBox.Text.ShouldBe("Ada");
    }

    [Fact]
    public void LambdaBinding_Parameterless_UsesBindingContextNotCapturedLocal()
    {
        TextBox textBox = new();
        MyViewModel vm = new() { PlayerName = "Captured" };
        MyViewModel context = new() { PlayerName = "Context" };
        textBox.BindingContext = context;

        textBox.SetBinding(
            nameof(TextBox.Text),
            () => vm.PlayerName);

        // The lambda supplies only the path "PlayerName"; the value comes from BindingContext.
        textBox.Text.ShouldBe("Context");
    }

    [Fact]
    public void LambdaBinding_ParameterlessSample_ShowsViewModelValue()
    {
        TextBox textBox = new();
        MyViewModel vm = new();
        vm.PlayerName = "Ada";
        textBox.BindingContext = vm;
        textBox.SetBinding(
            nameof(TextBox.Text),
            () => vm.PlayerName);

        textBox.Text.ShouldBe("Ada");
    }

    [Fact]
    public void StringAndBindingObject_OneWay_DoesNotWriteBack()
    {
        Label label = new();
        MyViewModel viewModel = new() { LabelText = "Hello" };
        label.BindingContext = viewModel;
        Binding binding = new(nameof(MyViewModel.LabelText))
        {
            Mode = BindingMode.OneWay
        };

        label.SetBinding(
            nameof(label.Text),
            binding);
        label.Text.ShouldBe("Hello");
        label.Text = "Changed";

        viewModel.LabelText.ShouldBe("Hello");
    }

    [Fact]
    public void MissingStringPath_DoesNotThrow_ShowsDefault()
    {
        Label label = new();
        label.Text = "Before";
        label.BindingContext = new MyViewModel();

        Should.NotThrow(() => label.SetBinding(nameof(label.Text), "LabelTxt"));
    }

    [Fact]
    public void NestedPath_ReplacingIntermediate_Updates()
    {
        Label label = new();
        MyViewModel viewModel = new() { CurrentPlayer = new PlayerViewModel { Name = "Ada" } };
        label.BindingContext = viewModel;
        Binding binding = new("CurrentPlayer.Name");
        label.SetBinding(nameof(Label.Text), binding);
        label.Text.ShouldBe("Ada");

        viewModel.CurrentPlayer = new PlayerViewModel { Name = "Bob" };

        label.Text.ShouldBe("Bob");
    }

    [Fact]
    public void NestedPath_Lambda_ShowsValue()
    {
        Label label = new();
        label.BindingContext = new MyViewModel { CurrentPlayer = new PlayerViewModel { Name = "Ada" } };

        label.SetBinding<MyViewModel>(
            nameof(Label.Text),
            vm => vm.CurrentPlayer.Name);

        label.Text.ShouldBe("Ada");
    }

    [Fact]
    public void IndexPaths_StringNestedAndLambda_ShowValuesAndFollowCollection()
    {
        MyViewModel viewModel = new();
        viewModel.Items.Add(new ItemViewModel { Text = "First" });
        TeamViewModel team = new();
        team.Members.Add(new PlayerViewModel { Name = "Ada" });
        viewModel.CurrentTeam = team;

        TextBox first = new() { BindingContext = viewModel };
        first.SetBinding(
            nameof(TextBox.Text),
            new Binding("Items[0].Text"));
        TextBox member = new() { BindingContext = viewModel };
        member.SetBinding(
            nameof(TextBox.Text),
            new Binding("CurrentTeam.Members[0].Name"));
        TextBox lambda = new() { BindingContext = viewModel };
        lambda.SetBinding<MyViewModel>(
            nameof(TextBox.Text),
            vm => vm.Items[0].Text);

        first.Text.ShouldBe("First");
        member.Text.ShouldBe("Ada");
        lambda.Text.ShouldBe("First");

        viewModel.Items[0] = new ItemViewModel { Text = "Replaced" };
        first.Text.ShouldBe("Replaced");
    }

    [Fact]
    public void IndexPath_OutOfRange_UsesFallbackValue()
    {
        MyViewModel viewModel = new();
        TextBox textBox = new() { BindingContext = viewModel };

        textBox.SetBinding(nameof(TextBox.Text), new Binding("Items[0].Text") { FallbackValue = "none" });

        textBox.Text.ShouldBe("none");
    }

    [Fact]
    public void DefaultUpdateTriggers_LostFocus_DelaysSourceUpdate()
    {
        try
        {
            Binding.DefaultUpdateTriggers[(typeof(TextBox), nameof(TextBox.Text))] = UpdateSourceTrigger.LostFocus;
            MyViewModel viewModel = new() { PlayerName = "Ada" };
            TextBox textBox = new() { BindingContext = viewModel };
            textBox.SetBinding(nameof(TextBox.Text), nameof(MyViewModel.PlayerName));

            textBox.Text = "Bob";

            viewModel.PlayerName.ShouldBe("Ada");
        }
        finally
        {
            Binding.DefaultUpdateTriggers.Clear();
        }
    }

    [Fact]
    public void Converter_ConvertsAndConvertsBack()
    {
        Label label = new();
        MyViewModel viewModel = new() { IsActive = true };
        label.BindingContext = viewModel;
        Binding binding = new(nameof(MyViewModel.IsActive))
        {
            Converter = new BoolToYesNoConverter()
        };
        label.SetBinding(nameof(Label.Text), binding);
        label.Text.ShouldBe("Yes");

        label.Text = "No";

        viewModel.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void StringFormat_FormatsAndDoesNotParseBack()
    {
        Label label = new();
        MyViewModel viewModel = new() { Health = 1234 };
        label.BindingContext = viewModel;
        Binding binding = new(nameof(MyViewModel.Health))
        {
            StringFormat = "HP: {0:N0}"
        };
        label.SetBinding(nameof(Label.Text), binding);
        label.Text.ShouldBe(string.Format("HP: {0:N0}", 1234f));

        label.Text = "HP: 5";

        viewModel.Health.ShouldBe(1234);
    }

    [Fact]
    public void StringFormat_WithoutPlaceholder_Throws()
    {
        Label label = new();
        label.BindingContext = new MyViewModel();
        string interpolated = $"HP: {0:N0}";

        Should.Throw<Exception>(() => label.SetBinding(nameof(Label.Text),
            new Binding(nameof(MyViewModel.Health)) { StringFormat = interpolated }));
    }

    [Fact]
    public void FallbackValue_NullIntermediate_ShowsFallback()
    {
        Label label = new();
        label.BindingContext = new MyViewModel();
        Binding binding = new("CurrentPlayer.Name")
        {
            FallbackValue = "No player selected"
        };
        label.SetBinding(nameof(Label.Text), binding);

        label.Text.ShouldBe("No player selected");
    }

    [Fact]
    public void ImplicitConversion_IntSource_ConvertsBothWaysAndSkipsFailures()
    {
        ScoreViewModel vm = new() { Score = 8 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(ScoreViewModel.Score));
        textBox.Text.ShouldBe("8");

        vm.Score = 12;
        textBox.Text.ShouldBe("12");

        textBox.Text = "42";
        vm.Score.ShouldBe(42);

        textBox.Text = "hello";
        vm.Score.ShouldBe(42);

        textBox.Text = "";
        vm.Score.ShouldBe(42);
    }

    [Fact]
    public void ImplicitConversion_NullableSource_EmptyStringLeavesSource()
    {
        ScoreViewModel vm = new() { OptionalScore = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(ScoreViewModel.OptionalScore));

        textBox.Text = "";

        vm.OptionalScore.ShouldBe(5);
    }

    [Fact]
    public void ImplicitConversion_StringAndIntToEnum_MatchNameAnyCaseAndValue()
    {
        ScoreViewModel vm = new() { Mode = "multiple", ModeNumber = (int)SelectionMode.Multiple };
        ListBox byName = new() { BindingContext = vm };
        ListBox byNumber = new() { BindingContext = vm };

        byName.SetBinding(nameof(ListBox.SelectionMode), nameof(ScoreViewModel.Mode));
        byNumber.SetBinding(nameof(ListBox.SelectionMode), nameof(ScoreViewModel.ModeNumber));

        byName.SelectionMode.ShouldBe(SelectionMode.Multiple);
        byNumber.SelectionMode.ShouldBe(SelectionMode.Multiple);
        Should.Throw<Exception>(() => vm.Mode = "Bogus");
    }
}
