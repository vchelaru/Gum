using Gum.Forms.Controls;
using Gum.Forms.Data;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

public class BindingOperationsTests
{
    [Fact]
    public void GetBindingExpression_FromPropertyName()
    {
        // Arrange
        TextBox textBox = new();
        textBox.SetBinding(nameof(TextBox.Text), "_");
        
        // Act
        BindingExpressionBase? result = BindingOperations.GetBindingExpression(textBox, "Text");
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void ClearBinding_UnsetsTargetValue_RemovesBinding()
    {
        // Arrange
        TextBox textBox = new()
        {
            BindingContext = new TestViewModel { FloatValue = 123}
        };
        
        textBox.SetBinding(nameof(FrameworkElement.Width), nameof(TestViewModel.FloatValue));
        
        // Act
        float widthBeforeClear = textBox.Width;
        BindingOperations.ClearBinding(textBox, nameof(CheckBox.Width));
        
        // Assert
        Assert.False(textBox.IsDataBound(nameof(FrameworkElement.Width)));
        
        Assert.Equal(123, widthBeforeClear);
        Assert.Equal(0, textBox.Width);
    }
}