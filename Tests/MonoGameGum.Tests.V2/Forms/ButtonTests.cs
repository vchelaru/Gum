using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ButtonTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        Button button = new();
        button.Visual.ShouldNotBeNull();
        (button.Visual is ButtonVisual).ShouldBeTrue();
    }
}
