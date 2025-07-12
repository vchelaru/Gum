using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class SplitterTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var splitter = new MonoGameGum.Forms.Controls.Splitter();
        splitter.Visual.ShouldNotBeNull();
        (splitter.Visual is MonoGameGum.Forms.DefaultVisuals.SplitterVisual).ShouldBeTrue();
    }
}
