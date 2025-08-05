using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class RootTests : BaseTestClass
{
    [Fact]
    public void RemovingChildren_ShouldNotThrowException()
    {
        var root = GumService.Default.Root;

        root.Children.Clear();

        TextBox textBox = new();
        (textBox).AddToRoot();
        textBox.IsFocused = true;
        root.Children.Clear();

        (textBox).AddToRoot();
        TextBox textBox2 = new();
        root.Children[0] = textBox2.Visual;
        textBox2.IsFocused = true;
        root.Children.Clear();

        textBox = new();
        root.Children.Add(textBox.Visual);
        textBox.IsFocused = true;
        root.Children.Remove(textBox.Visual);
    }

}
