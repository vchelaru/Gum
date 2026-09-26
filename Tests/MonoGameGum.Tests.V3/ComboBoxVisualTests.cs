using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using System;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ComboBoxVisualTests
{
    [Fact]
    public void ComboBox_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ComboBox sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ListBoxInstance_SetToVisualWithoutListBox_ThrowsArgumentExceptionAndKeepsListBox()
    {
        ComboBoxVisual sut = new ComboBoxVisual();
        ListBox originalListBox = sut.FormsControl.ListBox;
        ContainerRuntime notAListBox = new ContainerRuntime { Name = "ListBoxInstance" };

        Should.Throw<ArgumentException>(() => sut.ListBoxInstance = notAListBox);

        sut.FormsControl.ListBox.ShouldBeSameAs(originalListBox);
    }
}
