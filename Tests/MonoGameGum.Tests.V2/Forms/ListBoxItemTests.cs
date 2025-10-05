using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ListBoxItemTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var listBoxItem = new Gum.Forms.Controls.ListBoxItem();
        listBoxItem.Visual.ShouldNotBeNull();
        (listBoxItem.Visual is Gum.Forms.DefaultVisuals.ListBoxItemVisual).ShouldBeTrue();
    }
}
