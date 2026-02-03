using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class RadioButtonTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var radioButton = new Gum.Forms.Controls.RadioButton();
        radioButton.Visual.ShouldNotBeNull();
        (radioButton.Visual is Gum.Forms.DefaultVisuals.RadioButtonVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        RadioButton sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
