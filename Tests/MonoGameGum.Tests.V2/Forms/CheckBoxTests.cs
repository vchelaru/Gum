using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class CheckBoxTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        CheckBox checkBox = new();
        checkBox.Visual.ShouldNotBeNull();
        (checkBox.Visual is CheckBoxVisual).ShouldBeTrue();
    }
}
