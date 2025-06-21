using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class SliderTests
{
    [Fact]
    public void Value_ShouldBeLimited_WhenOutsideOfMinimumAndMaximum()
    {
        var slider = new MonoGameGum.Forms.Controls.Slider
        {
            Minimum = 0,
            Maximum = 100
        };
        slider.Value = -10;
        slider.Value.ShouldBe(0);
        slider.Value = 110;
        slider.Value.ShouldBe(100);
    }

    [Fact]
    public void Value_ShouldBeAdjusted_WhenMinimumAndMaximumChange()
    {
        var slider = new MonoGameGum.Forms.Controls.Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        slider.Value = 10;
        slider.Minimum = 25;
        slider.Value.ShouldBe(25);

        slider.Value = 90;
        slider.Maximum = 75;
        slider.Maximum.ShouldBe(75);
    }
}
