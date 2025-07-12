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
        var slider = new MonoGameGum.Forms.Controls.Slider();
        slider.Visual.ShouldNotBeNull();
        (slider.Visual is MonoGameGum.Forms.DefaultVisuals.SliderVisual).ShouldBeTrue();
    }
}
