using MonoGameGum.Forms;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class WindowTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var window = new Window();
        window.Visual.ShouldNotBeNull();
        (window.Visual is Gum.Forms.DefaultVisuals.WindowVisual).ShouldBeTrue();
    }
}
