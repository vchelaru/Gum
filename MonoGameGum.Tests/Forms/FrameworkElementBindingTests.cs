using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.Data;
using RenderingLibrary;
using Shouldly;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class FrameworkElementBindingTests
{
    public FrameworkElementBindingTests()
    {
    }

    [Fact]
    public void LegacySetBinding_UsingStringParameter()
    {
        // Arrange
        TestViewModel vm = new() { Text = "Hello World!" };
        TextBox element = new() { BindingContext = vm };

        // Act
        element.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        // Assert
        element.IsDataBound(nameof(TextBox.Text)).ShouldBe(true);
        element.Text.ShouldBe(vm.Text);
    }

    [Fact]
    public void SetBinding_WithoutExplicitContext_PullsFromParent()
    {
        // Arrange
        StackPanel stackPanel = new();
        TestViewModel vm = new(){ Text = "1234"};
        stackPanel.BindingContext = vm;

        TextBox textBox = new();
        stackPanel.AddChild(textBox);

        // Act
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        // Assert
        textBox.BindingContext.ShouldBe(vm);
        textBox.Text.ShouldBe("1234");

    }

    [Fact]
    public void SetBinding_ToBindingContext_SwapBranchNode()
    {
        // Arrange
        StackPanel stackPanel = new();
        TestViewModel root = new() { Text = "Root" };
        TestViewModel child = new() { Text = "Child" };
        TestViewModel swapped = new() { Text = "SwappedChild" };

        stackPanel.BindingContext = root;
        root.Child = child;

        TextBox textBox = new();
        textBox.SetBinding(nameof(TextBox.BindingContext), nameof(TestViewModel.Child));
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        stackPanel.AddChild(textBox);

        // Act
        root.Child = swapped;

        // Assert
        textBox.BindingContext.ShouldBe(swapped);
        textBox.Text.ShouldBe("SwappedChild");
    }

    [Fact]
    public void SetBinding_ToBindingContext_SwapRoot()
    {
        // Arrange
        StackPanel root = new();
        TestViewModel foo = new() { Text = "FooParent", Child = new() { Text = "Foo" } };
        TestViewModel bar = new() { Text = "BarParent", Child = new() { Text = "Bar" } };

        root.BindingContext = foo;
        TextBox textBox = new();
        textBox.SetBinding(nameof(TextBox.BindingContext), nameof(TestViewModel.Child));
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        root.AddChild(textBox);

        // Act
        root.BindingContext = bar;

        // Assert
        textBox.BindingContext.ShouldBe(bar.Child);
        textBox.Text.ShouldBe("Bar");
    }

    [Fact]
    public void Mode_TwoWay_ByDefault()
    {
        // Arrange
        CheckBox checkBox = new();
        TestViewModel vm = new();
        checkBox.BindingContext = vm;
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));

        // Act / Assert
        checkBox.IsChecked = true;
        vm.IsChecked.ShouldBe(true);

        vm.IsChecked = false;
        checkBox.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void Mode_OneWay_OnlyUpdatesTarget()
    {
        // Arrange
        CheckBox checkBox = new();
        TestViewModel vm = new();
        checkBox.BindingContext = vm;

        Binding binding = new(nameof(TestViewModel.IsChecked))
        {
            Mode = BindingMode.OneWay
        };
        checkBox.SetBinding(nameof(CheckBox.IsChecked), binding);

        // Act / Assert
        checkBox.IsChecked = true;
        vm.IsChecked.ShouldBe(false);

        checkBox.IsChecked = false;
        vm.IsChecked = true;
        checkBox.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void Mode_OneWayToSource_OnlyUpdatesSource()
    {
        // Arrange
        CheckBox checkBox = new();
        TestViewModel vm = new();
        checkBox.BindingContext = vm;

        Binding binding = new(nameof(TestViewModel.IsChecked))
        {
            Mode = BindingMode.OneWayToSource
        };
        checkBox.SetBinding(nameof(CheckBox.IsChecked), binding);

        // Act / Assert
        vm.IsChecked = true;
        checkBox.IsChecked.ShouldBe(false);

        vm.IsChecked = false;
        checkBox.IsChecked = true;
        vm.IsChecked.ShouldBe(true);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SetBinding_DeepInheritance(int depth)
    {
        // Arrange
        TestViewModel vm = new();
        StackPanel root = new();
        StackPanel last = new();
        root.AddChild(last);

        for (int d = 0; d <= depth; d++)
        {
            StackPanel next = new();
            last.AddChild(next);
            (last, _) = (next, last);
        }

        root.BindingContext = vm;

        CheckBox checkBox = new();
        last.AddChild(checkBox);

        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));
        checkBox.SetBinding(nameof(CheckBox.Text), nameof(TestViewModel.Text));

        // Act / Assert
        checkBox.IsChecked = true;
        vm.IsChecked.ShouldBe(true);

        vm.Text = "foo";
        checkBox.Text.ShouldBe("foo");
    }

    [Fact]
    public void ListBoxItemBinding_ShouldSetBindingContextOnListBoxItems()
    {
        // Arrange
        ListBox listBox = new();
        TestViewModel vm = new()
        {
            Items =
            [
                new() { Text = "Item 1" },
                new() { Text = "Item 2" }
            ]
        };

        listBox.BindingContext = vm;

        // Act
        listBox.SetBinding(nameof(ListBox.Items), nameof(TestViewModel.Items));

        // Assert
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems.Select(i => i.BindingContext).ShouldBe(vm.Items);
    }

    [Fact]
    public void ComplexPaths()
    {
        TestViewModel vm = new()
        {
            Child = new()
            {
                Child = new()
                {
                    Child = new()
                    {
                        Text = "Hello World!"
                    }
                }
            }
        };

        TextBox element = new() { BindingContext = vm };

        Binding binding = new ("Child.Child.Child.Text");

        // Initial target update from source binding
        element.SetBinding(nameof(TextBox.Text), binding);
        element.Text.ShouldBe("Hello World!");

        // Update source from target
        element.Text = "FromUI";
        vm.Child.Child.Child.Text.ShouldBe("FromUI");

        // Update target from source
        vm.Child.Child.Child.Text = "FromVM";
        element.Text.ShouldBe("FromVM");

        // Replace a middle node in the source path
        vm.Child.Child = new TestViewModel { Child = new TestViewModel { Text = "SwappedNestedPath" } };
        element.Text.ShouldBe("SwappedNestedPath");
    }

    [Fact]
    public void SourcePathResolutionFailure_UsesFallback()
    {
        // Arrange
        TestViewModel vm = new()
        {
            Child = new()
            {
                Child = new()
                {
                    Text = "HelloWorld"
                }
            }
        };

        TextBox element = new() { BindingContext = vm };
        Binding binding = new("Child.Child.Text")
        {
            FallbackValue = "Fallback"
        };

        element.SetBinding(nameof(TextBox.Text), binding);

        // Act
        vm.Child.Child = null;

        // Assert
        element.Text.ShouldBe("Fallback");
    }

    [Fact]
    public void UpdateSourceTrigger_LostFocus()
    {
        // Arrange
        TestViewModel vm = new() { Text = "Initial" };

        TextBox element = new() { BindingContext = vm };

        Binding binding = new(nameof(TestViewModel.Text))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        element.SetBinding(nameof(TextBox.Text), binding);
        element.IsFocused = true;
        element.Text = "FromUI";

        // Act / Assert
        vm.Text.ShouldBe("Initial");
        element.IsFocused = false;
        vm.Text.ShouldBe("FromUI");
    }

    [Theory]
    [InlineData("Yes", true)]
    [InlineData("No", false)]
    public void Binding_Converter_ToSource(string targetValue, bool expectedSourceValue)
    {
        // Arrange
        TestViewModel vm = new();
        TextBox element = new() { BindingContext = vm };
        Binding binding = new(nameof(TestViewModel.IsChecked))
        {
            Converter = new TestStringBoolConverter(),
        };
        element.SetBinding(nameof(TextBox.Text), binding);

        // Act
        element.Text = targetValue;

        // Assert
        vm.IsChecked.ShouldBe(expectedSourceValue);
    }

    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    public void Binding_Converter_FromSource(bool sourceValue, string expectedTargetValue)
    {
        // Arrange
        TestViewModel vm = new() { IsChecked = sourceValue };
        TextBox element = new() { BindingContext = vm };
        Binding binding = new(nameof(TestViewModel.IsChecked))
        {
            Converter = new TestStringBoolConverter(),
        };

        // Act
        element.SetBinding(nameof(TextBox.Text), binding);

        // Assert
        element.Text.ShouldBe(expectedTargetValue);
    }

    [Fact]
    public void Binding_TargetToSource_InvalidCast_LeavesSourceUnchanged()
    {
        // Arrange
        const float expectedValue = 12.34f;
        TestViewModel vm = new() { FloatValue = expectedValue };
        TextBox element = new() { BindingContext = vm };
        Binding binding = new(nameof(TestViewModel.FloatValue));
        element.SetBinding(nameof(TextBox.Text), binding);

        // Act
        element.Text = "not a number";

        // Assert
        vm.FloatValue.ShouldBe(expectedValue);
    }

    [Fact]
    public void Binding_SourceToTarget_InvalidCast_UnsetsTargetValue()
    {
        // Arrange
        TestViewModel vm = new() { };
        Slider element = new() { BindingContext = vm, TicksFrequency = 1234 };
        Binding binding = new(nameof(TestViewModel.Text));
        element.SetBinding(nameof(Slider.TicksFrequency), binding);

        // Act
        vm.Text = "not a number";

        // Assert
        element.TicksFrequency.ShouldBe(0);
    }

    [Fact]
    public void InvalidPath_DoesNoHarm()
    {
        TestViewModel vm = new();
        TextBox element = new() { BindingContext = vm };
        Binding binding = new("Invalid.Path");

        element.SetBinding(nameof(TextBox.Text), binding);
    }

    [Fact]
    public void SetBinding_BindingContext_SetThenAdd()
    {
        // Arrange
        StackPanel panel = new();
        TextBox element = new();
        TestViewModel vm = new();
        TestViewModel child = new();

        vm.Child = child;
        panel.BindingContext = vm;

        // Act
        element.SetBinding(nameof(TextBox.BindingContext), nameof(TestViewModel.Child));
        panel.AddChild(element);

        // Assert
        element.BindingContext.ShouldBe(child);
    }

    [Fact]
    public void SetBinding_BindingContext_AddThenSet()
    {
        // Arrange
        StackPanel panel = new();
        TestViewModel vm = new();
        TestViewModel child = new();

        vm.Child = child;
        panel.BindingContext = vm;

        TextBox element = new();

        // Act
        panel.AddChild(element);
        element.SetBinding(nameof(TextBox.BindingContext), nameof(TestViewModel.Child));

        // Assert
        element.BindingContext.ShouldBe(child);
    }

    [Fact]
    public void OnUpdateTarget_CallsPropertyChangedOnce()
    {
        // Arrange
        TestViewModel vm = new();
        CheckBox checkBox = new() { BindingContext = vm };
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));

        int propertyChangedCount = 0;
        vm.PropertyChanged += (_, _) =>
        {
            propertyChangedCount++;
        };

        // Act
        vm.IsChecked = true;

        // Assert
        propertyChangedCount.ShouldBe(1);
    }

    [Fact]
    public void OnUpdateSource_CallsPropertyChangedOnce()
    {
        // Arrange
        TestViewModel vm = new();
        CheckBox checkBox = new() { BindingContext = vm };
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));
        int propertyChangedCount = 0;
        vm.PropertyChanged += (_, _) =>
        {
            propertyChangedCount++;
        };

        // Act
        checkBox.IsChecked = true;

        // Assert
        propertyChangedCount.ShouldBe(1);
    }

    [Fact]
    public void StringFormat_FormatsNewTargetValue()
    {
        TestViewModel vm = new() { FloatValue = 0.1234f };
        Label label = new() { BindingContext = vm };
        Binding binding = new(nameof(TestViewModel.FloatValue))
        {
            StringFormat = "Value: {0:P}"
        };

        label.SetBinding(nameof(Label.Text), binding);
        label.Text.ShouldBe("Value: 12.34%");

        label.Text = "Value: 55.55%";

        //string format technically only works on a one-way binding, but we
        //want to make sure we don't fall over or muck with the source value
        vm.FloatValue.ShouldBe(0.1234f);
    }

    [Fact]
    public void SetTargetNull_WhenSourceIsNullableValueType_SourceBecomesNull()
    {
        // Arrange
        TestViewModel vm = new() { NullableFloatValue = 42 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.NullableFloatValue));

        // Act
        label.Text = null!;

        // Assert
        vm.NullableFloatValue.ShouldBe(null);
    }

    [Fact]
    public void SetTargetNull_WhenSourceIsReferenceType_SourceBecomesNull()
    {
        // Arrange
        TestViewModel vm = new() { Text = "Initial" };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));
        // Act
        label.Text = null!;
        // Assert
        vm.Text.ShouldBe(null);
    }

    [Fact]
    public void SetTargetNull_WhenSourceIsNotNullable_SourceUnchanged()
    {
        // Arrange
        TestViewModel vm = new() { FloatValue = 42 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.FloatValue));

        // Act
        label.Text = null!;

        // Assert
        vm.FloatValue.ShouldBe(42f);
    }

    [Fact]
    public void SetSourceNull_WhenTargetIsNullable_TargetBecomesNull()
    {
        // Arrange
        TestViewModel viewModel = new() { Text = "not null" };
        Label label = new() { BindingContext  = viewModel };

        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));

        // Act
        viewModel.Text = null;

        // Assert
        label.Text.ShouldBe(null);
    }

    [Fact]
    public void SetSourceNull_WhenTargetNonNullable_TargetUnchanged()
    {
        // Arrange
        TestViewModel viewModel = new() { FloatValue = 42f };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.FloatValue));

        // Act
        viewModel.NullableFloatValue = null;

        // Assert
        label.Text.ShouldBe("42");
    }

    [Fact]
    public void SetTargetNull_WhenTargetNullValueDefined_UsesValue()
    {
        // Arrange
        const string targetNullValue = "Target is null";
        TestViewModel vm = new() { Text = "Initial" };
        Label label = new() { BindingContext = vm };
        Binding binding = new(nameof(TestViewModel.Text))
        {
            TargetNullValue = targetNullValue
        };
        label.SetBinding(nameof(Label.Text), binding);

        // Act
        label.Text = null!;

        // Assert
        label.Text.ShouldBe(targetNullValue);
    }

    [Fact]
    public void ReplaceBindingContext_UnrelatedTypes()
    {
        // Arrange
        const string expectedText = nameof(AuxTestViewModel);

        TextBox textBox = new();
        TestViewModel vm1 = new() {Text = nameof(TestViewModel)};
        textBox.BindingContext = vm1;
        textBox.SetBinding(nameof(textBox.Text), nameof(TestViewModel.Text));

        AuxTestViewModel vm2 = new() { Text = expectedText };

        // Act
        Exception? exception = Record.Exception(() => textBox.BindingContext = vm2);

        // Assert
        Assert.Null(exception);
        Assert.Equal(expectedText, textBox.Text);
    }

    private class TestViewModel : ViewModel
    {
        public TestViewModel? Child
        {
            get => Get<TestViewModel?>();
            set => Set(value);
        }

        public ObservableCollection<TestViewModel> Items
        {
            get => Get<ObservableCollection<TestViewModel>>();
            set => Set(value);
        }

        public string? Text
        {
            get => Get<string?>();
            set => Set(value);
        }

        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float FloatValue
        {
            get => Get<float>(); set => Set(value);
        }

        public float? NullableFloatValue
        {
            get => Get<float?>();
            set => Set(value);
        }

        public override string ToString() => Text;
    }

    public class AuxTestViewModel : ViewModel
    {
        public string? Text
        {
            get=> Get<string?>();
            set => Set(value);
        }
    }

    private class TestStringBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter)
        {
            return value switch
            {
                true => "Yes",
                false => "No",
                _ => GumProperty.UnsetValue
            };
        }

        public object? ConvertBack(object? value, Type sourceType, object? parameter)
        {
            return value switch
            {
                string s when s.Equals("yes", StringComparison.InvariantCultureIgnoreCase) => true,
                string s when s.Equals("no", StringComparison.InvariantCultureIgnoreCase) => false,
                _ => GumProperty.UnsetValue
            };
        }
    }
}