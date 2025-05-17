using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.Data;
using RenderingLibrary;
using System.Collections.ObjectModel;
using TUnit.Assertions.AssertConditions.Throws;

namespace MonoGameGum.Tests.Forms;

public class BindingTests
{
    [Before(Class)]
    public static void SetUp()
    {
        SystemManagers.Default = new();
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        FormsUtilities.InitializeDefaults();
    }

    [Test]
    public async Task LegacySetBinding_UsingStringParameter()
    {
        // Arrange
        TestViewModel vm = new() { Text = "Hello World!" };
        TextBox element = new() { BindingContext = vm };

        // Act
        element.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        // Assert
        await Assert.That(element.IsDataBound(nameof(TextBox.Text))).IsTrue();
        await Assert.That(element.Text).IsEqualTo(vm.Text);
    }

    [Test]
    public async Task SetBinding_WithoutExplicitContext_PullsFromParent()
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
        await Assert.That(textBox.BindingContext).IsEqualTo(vm);
        await Assert.That(textBox.Text).IsEqualTo("1234");

    }

    [Test]
    public async Task SetBinding_ToBindingContext_SwapBranchNode()
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
        await Assert.That(textBox.BindingContext).IsEqualTo(swapped);
        await Assert.That(textBox.Text).IsEqualTo("SwappedChild");
    }

    [Test]
    public async Task SetBinding_ToBindingContext_SwapRoot()
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
        await Assert.That(textBox.BindingContext).IsEqualTo(bar.Child);
        await Assert.That(textBox.Text).IsEqualTo("Bar");
    }

    [Test]
    public async Task Mode_TwoWay_ByDefault()
    {
        // Arrange
        CheckBox checkBox = new();
        TestViewModel vm = new();
        checkBox.BindingContext = vm;
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));

        // Act / Assert
        checkBox.IsChecked = true;
        await Assert.That(vm.IsChecked).IsTrue();

        vm.IsChecked = false;
        await Assert.That(checkBox.IsChecked).IsFalse();
    }

    [Test]
    public async Task Mode_OneWay_OnlyUpdatesTarget()
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
        await Assert.That(vm.IsChecked).IsFalse();

        checkBox.IsChecked = false;
        vm.IsChecked = true;
        await Assert.That(checkBox.IsChecked).IsTrue();
    }

    [Test]
    public async Task Mode_OneWayToSource_OnlyUpdatesSource()
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
        await Assert.That(checkBox.IsChecked).IsFalse();

        vm.IsChecked = false;
        checkBox.IsChecked = true;
        await Assert.That(vm.IsChecked).IsTrue();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task SetBinding_DeepInheritance(int depth)
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
        await Assert.That(vm.IsChecked).IsTrue();

        vm.Text = "foo";
        await Assert.That(checkBox.Text).IsEqualTo("foo");
    }

    [Test]
    public async Task ListBoxItemBinding_ShouldSetBindingContextOnListBoxItems()
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
        await Assert.That(listBox.ListBoxItems).HasCount(2);
        await Assert.That(listBox.ListBoxItems.Select(i => i.BindingContext)).IsEquivalentTo(vm.Items);
    }

    [Test]
    public async Task ComplexPaths()
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
        await Assert.That(element.Text).IsEqualTo("Hello World!");

        // Update source from target
        element.Text = "FromUI";
        await Assert.That(vm.Child.Child.Child.Text).IsEqualTo("FromUI");

        // Update target from source
        vm.Child.Child.Child.Text = "FromVM";
        await Assert.That(element.Text).IsEqualTo("FromVM");

        // Replace a middle node in the source path
        vm.Child.Child = new TestViewModel { Child = new TestViewModel { Text = "SwappedNestedPath" } };
        await Assert.That(element.Text).IsEqualTo("SwappedNestedPath");
    }

    [Test]
    public async Task SourcePathResolutionFailure_UsesFallback()
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
        await Assert.That(element.Text).IsEqualTo("Fallback");
    }

    [Test]
    public async Task UpdateSourceTrigger_LostFocus()
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
        await Assert.That(vm.Text).IsEqualTo("Initial");
        element.IsFocused = false;
        await Assert.That(vm.Text).IsEqualTo("FromUI");
    }

    [Test]
    [Arguments("Yes", true)]
    [Arguments("No", false)]
    public async Task Binding_Converter_ToSource(string targetValue, bool expectedSourceValue)
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
        await Assert.That(vm.IsChecked).IsEqualTo(expectedSourceValue);
    }

    [Test]
    [Arguments(true, "Yes")]
    [Arguments(false, "No")]
    public async Task Binding_Converter_FromSource(bool sourceValue, string expectedTargetValue)
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
        await Assert.That(element.Text).IsEqualTo(expectedTargetValue);
    }

    [Test]
    public async Task Binding_TargetToSource_InvalidCast_LeavesSourceUnchanged()
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
        await Assert.That(vm.FloatValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task Binding_SourceToTarget_InvalidCast_UnsetsTargetValue()
    {
        // Arrange
        TestViewModel vm = new() { };
        Slider element = new() { BindingContext = vm, TicksFrequency = 1234 };
        Binding binding = new(nameof(TestViewModel.Text));
        element.SetBinding(nameof(Slider.TicksFrequency), binding);

        // Act
        vm.Text = "not a number";

        // Assert
        await Assert.That(element.TicksFrequency).IsEqualTo(0);
    }

    [Test]
    public async Task InvalidPath_DoesNoHarm()
    {
        TestViewModel vm = new();
        TextBox element = new() { BindingContext = vm };
        Binding binding = new("Invalid.Path");
        await Assert.That(() => element.SetBinding(nameof(TextBox.Text), binding)).ThrowsNothing();
    }

    [Test]
    public async Task SetBinding_BindingContext_SetThenAdd()
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
        await Assert.That(element.BindingContext).IsEqualTo(child);
    }

    [Test]
    public async Task SetBinding_BindingContext_AddThenSet()
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
        await Assert.That(element.BindingContext).IsEqualTo(child);
    }

    [Test]
    public async Task OnUpdateTarget_CallsPropertyChangedOnce()
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
        await Assert.That(propertyChangedCount).IsEqualTo(1);
    }

    [Test]
    public async Task OnUpdateSource_CallsPropertyChangedOnce()
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
        await Assert.That(propertyChangedCount).IsEqualTo(1);
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

        public override string ToString() => Text;
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