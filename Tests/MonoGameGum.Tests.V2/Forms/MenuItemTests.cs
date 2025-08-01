using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class MenuItemTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var menuItem = new MonoGameGum.Forms.Controls.MenuItem();
        menuItem.Visual.ShouldNotBeNull();
        (menuItem.Visual is Gum.Forms.DefaultVisuals.MenuItemVisual).ShouldBeTrue();
    }
}
