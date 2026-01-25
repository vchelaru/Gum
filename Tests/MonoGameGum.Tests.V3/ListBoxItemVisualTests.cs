using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ListBoxItemVisualTests
{
    [Fact]
    public void ListBoxItem_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ListBoxItem sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
