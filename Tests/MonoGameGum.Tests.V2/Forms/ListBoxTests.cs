using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ListBoxTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var listBox = new MonoGameGum.Forms.Controls.ListBox();
        listBox.Visual.ShouldNotBeNull();
        (listBox.Visual is Gum.Forms.DefaultVisuals.ListBoxVisual).ShouldBeTrue();
    }
}
