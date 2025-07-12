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
        var radioButton = new MonoGameGum.Forms.Controls.RadioButton();
        radioButton.Visual.ShouldNotBeNull();
        (radioButton.Visual is MonoGameGum.Forms.DefaultVisuals.RadioButtonVisual).ShouldBeTrue();
    }
}
