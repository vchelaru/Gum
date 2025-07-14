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
        var listBoxItem = new MonoGameGum.Forms.Controls.ListBoxItem();
        listBoxItem.Visual.ShouldNotBeNull();
        (listBoxItem.Visual is MonoGameGum.Forms.DefaultVisuals.ListBoxItemVisual).ShouldBeTrue();
    }
}
