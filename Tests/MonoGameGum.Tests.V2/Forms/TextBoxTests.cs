using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class TextBoxTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var textBox = new Gum.Forms.Controls.TextBox();
        textBox.Visual.ShouldNotBeNull();
        (textBox.Visual is Gum.Forms.DefaultVisuals.TextBoxVisual).ShouldBeTrue();
    }

    [Fact]
    public void SelectionLength_ShouldWork_OnMultiline()
    {
        TextBox textBox = new();
        textBox.TextWrapping = TextWrapping.Wrap;
        textBox.Text = "Hello, this is a multiline text box. It has really long text. This should line wrap";
        textBox.SelectionStart = 0;
        textBox.SelectionLength = textBox.Text.Length;
    }
}
