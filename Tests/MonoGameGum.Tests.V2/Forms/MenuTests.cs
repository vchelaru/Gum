using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class MenuTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var menu = new MonoGameGum.Forms.Controls.Menu();
        menu.Visual.ShouldNotBeNull();
        (menu.Visual is Gum.Forms.DefaultVisuals.MenuVisual).ShouldBeTrue();
    }
}
