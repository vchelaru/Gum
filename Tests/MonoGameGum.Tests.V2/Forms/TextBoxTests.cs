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
        var textBox = new MonoGameGum.Forms.Controls.TextBox();
        textBox.Visual.ShouldNotBeNull();
        (textBox.Visual is MonoGameGum.Forms.DefaultVisuals.TextBoxVisual).ShouldBeTrue();
    }
}
