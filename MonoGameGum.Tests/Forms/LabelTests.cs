using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class LabelTests : BaseTestClass
{
    [Fact]
    public void Visual_HasEvents_ShouldBeFalse()
    {
        Label sut = new();
        sut.Visual.HasEvents.ShouldBeFalse();
    }

    [Fact]
    public void Text_ShouldParseBbCodeTags()
    {
        // Arrange
        var label = new Label();
        label.Text = "This is [IsBold=true]bold[/IsBold] and [IsItalic=true]italic[/IsItalic] text.";
        // Act
        var parsedText = label.Text;
        // Assert

        var rawText = (Text)label.TextComponent.RenderableComponent;
        rawText.InlineVariables.Count.ShouldBe(4, "because the font starts default and changes 4 times with tags: bold, not bold, italic, not italic");
        rawText.RawText.ShouldBe("This is bold and italic text.");
    }


    [Fact]
    public void Constructor_ShouldAcceptTextRuntimeVisual()
    {
        Label label = new Label(new TextRuntime());

        label.Text = "Hello test";
        label.Text.ShouldBe("Hello test");
    }
}
