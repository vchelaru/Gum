using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class LabelTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var label = new MonoGameGum.Forms.Controls.Label();
        label.Visual.ShouldNotBeNull();
        (label.Visual is MonoGameGum.Forms.DefaultVisuals.LabelVisual).ShouldBeTrue();
    }
}
