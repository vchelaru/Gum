using Gum.Forms.Controls;
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
        Menu menu = new ();
        menu.Visual.ShouldNotBeNull();
        (menu.Visual is Gum.Forms.DefaultVisuals.MenuVisual).ShouldBeTrue();
    }

    [Fact]
    public void AbsoluteHeight_ShouldNotBe0_IfNoItemsAreAdded()
    {
        Menu menu = new();
        menu.Visual.GetAbsoluteHeight().ShouldBeGreaterThan(0);
    }
}
