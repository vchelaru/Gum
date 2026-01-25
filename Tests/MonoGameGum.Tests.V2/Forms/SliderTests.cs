using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class SliderTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var slider = new Gum.Forms.Controls.Slider();
        slider.Visual.ShouldNotBeNull();
        (slider.Visual is Gum.Forms.DefaultVisuals.SliderVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Slider sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
