using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.Data;
using RenderingLibrary;

namespace MonoGameGum.Forms.Tests;

public class BindingTests
{
    public BindingTests()
    {
        SystemManagers.Default = new ();
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        FormsUtilities.InitializeDefaults();
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
                        Text = "Hello World"
                    }
                }
            }
        };

        TextBox sut = new() { BindingContext = vm };

        Binding binding = new Binding("Child.Child.Child.Text");

        // Initial target update from source binding
        sut.SetBinding(nameof(TextBox.Text), binding);
        Assert.Equal("Hello World", sut.Text);

        // Update source from target
        sut.Text = "FromUI";
        Assert.Equal("FromUI", vm.Child.Child.Child.Text);

        // Update target from source
        vm.Child.Child.Child.Text = "FromVM";
        Assert.Equal("FromVM", sut.Text);

        // Replace a middle node in the source path
        vm.Child.Child = new TestViewModel { Child = new TestViewModel { Text = "SwappedNestedPath" } };
        Assert.Equal("SwappedNestedPath", sut.Text);
    }

    [Fact]
    public void SourcePathResolutionFailure_UsesFallback()
    {
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

        TextBox sut = new() { BindingContext = vm };
        Binding binding = new Binding("Child.Child.Text")
        {
            FallbackValue = "Fallback"
        };

        sut.SetBinding(nameof(TextBox.Text), binding);

        Assert.Equal("HelloWorld", sut.Text);

        vm.Child.Child = null;
        Assert.Equal("Fallback", sut.Text);
    }

    [Fact]
    public void UpdateSourceTrigger_LostFocus()
    {
        TestViewModel vm = new() { Text = "Initial" };

        TextBox sut = new() { BindingContext = vm };

        Binding binding = new Binding(nameof(TestViewModel.Text))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        sut.SetBinding(nameof(TextBox.Text), binding);

        sut.IsFocused = true;
        sut.Text = "FromUI";

        Assert.Equal("Initial", vm.Text);
        sut.IsFocused = false;
        Assert.Equal("FromUI", vm.Text);
    }

    [Fact]
    public void Binding_Converter_ConvertsValues()
    {
        TestViewModel vm = new() { BoolValue = true };
        TextBox sut = new() { BindingContext = vm };
        Binding binding = new Binding(nameof(TestViewModel.BoolValue))
        {
            Converter = new TestStringBoolConverter(),
        };

        sut.SetBinding(nameof(TextBox.Text), binding);
        Assert.Equal("Yes", sut.Text);
        
        vm.BoolValue = false;
        Assert.Equal("No", sut.Text);

        sut.Text = "Yes";
        Assert.True(vm.BoolValue);

        sut.Text = "No";
        Assert.False(vm.BoolValue);
    }

    [Fact]
    public void Binding_TargetToSource_InvalidCast_DoesNotChangeSourceValue()
    {
        TestViewModel vm = new() { FloatValue = 12.34f };
        TextBox sut = new() { BindingContext = vm };
        Binding binding = new Binding(nameof(TestViewModel.FloatValue));

        sut.SetBinding(nameof(TextBox.Text), binding);

        sut.Text = "not a float";
        Assert.Equal(12.34f, vm.FloatValue);
    }

    [Fact]
    public void Binding_SourceToTarget_InvalidCast_DoesNotChangeTargetValue()
    {
        TestViewModel vm = new() { Text = "not a number" };
        Slider slider = new() { BindingContext = vm, TicksFrequency = 1234};

        Binding binding = new Binding(nameof(TestViewModel.Text));

        slider.SetBinding(nameof(Slider.TicksFrequency), binding);

        Assert.Equal(1234, slider.TicksFrequency);
    }

    [Fact]
    public void InvalidPath_DoesNothing()
    {
        TestViewModel vm = new();
        TextBox sut = new() { BindingContext = vm };
        Binding binding = new Binding("Invalid.Path");
        sut.SetBinding(nameof(TextBox.Text), binding);
    }

    private class TestViewModel : ViewModel
    {
        public TestViewModel? Child
        {
            get => Get<TestViewModel>();
            set => Set(value);
        }

        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }

        public float FloatValue
        {
            get => Get<float>(); set => Set(value);
        }

        public bool BoolValue
        {
            get => Get<bool>(); set => Set(value);
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