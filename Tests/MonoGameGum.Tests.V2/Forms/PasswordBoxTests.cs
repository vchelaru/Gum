using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class PasswordBoxTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var passwordBox = new MonoGameGum.Forms.Controls.PasswordBox();
        passwordBox.Visual.ShouldNotBeNull();
        (passwordBox.Visual is Gum.Forms.DefaultVisuals.PasswordBoxVisual).ShouldBeTrue();
    }
}
