﻿using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ComboBoxTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        ComboBox comboBox = new();
        comboBox.Visual.ShouldNotBeNull();
        (comboBox.Visual is ComboBoxVisual).ShouldBeTrue();
    }
}
