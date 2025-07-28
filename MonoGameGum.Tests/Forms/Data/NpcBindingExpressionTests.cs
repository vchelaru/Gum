using Gum.Forms.Controls;
using Gum.Forms.Data;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

public class NpcBindingExpressionTests
{
    [Fact]
    public void UpdateSource_UpdatesSourceProperty()
    {
        // Arrange
        TextBox textBox = new();
        TestViewModel vm = new()
        {
            Text = null
        };

        textBox.BindingContext = vm;
        Binding binding = new(() => vm.Text)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };;
        textBox.SetBinding(nameof(TextBox.Text), binding);
        BindingExpressionBase? sut = BindingOperations.GetBindingExpression(textBox, nameof(TextBox.Text));
        
        // Act
        textBox.Text = "foo";
        string? previousValue = vm.Text;
        sut!.UpdateSource();
        
        // Assert
        Assert.Null(previousValue);
        Assert.Equal("foo", vm.Text);
    }

    [Fact]
    public void UpdateTarget_UpdatesTargetProperty()
    {
        // Arrange
        TextBox textBox = new();
        NonReactiveObject vm = new()
        {
            Text = null
        };

        textBox.BindingContext = vm;
        Binding binding = new(() => vm.Text);
        textBox.SetBinding(nameof(TextBox.Text), binding);
        BindingExpressionBase? sut = BindingOperations.GetBindingExpression(textBox, nameof(TextBox.Text));
        
        // Act
        vm.Text = "foo";
        string? previousValue = textBox.Text;
        sut!.UpdateTarget();
        
        // Assert
        Assert.Null(previousValue);
        Assert.Equal("foo", vm.Text);
    }
    
    /// <summary>
    /// So we don't automatically trigger an update to target.
    /// </summary>
    private class NonReactiveObject
    {
        public string? Text { get; set; }
    }
    
}