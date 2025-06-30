using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ButtonTests
{
    [Fact]
    public void Constructor_ShouldAssignVisual()
    {
        Button button = new();

        button.Visual.ShouldNotBeNull();

        button.Visual.ShouldBeOfType<DefaultButtonRuntime>();
    }
}
