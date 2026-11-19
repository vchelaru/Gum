using System.Collections.Generic;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The context a <see cref="Text.ContextCustomizations"/> callback receives during a real draw: the letter's
/// color as set by the enclosing BBCode tags, and the letter's index in the displayed text.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextContextCustomizationDrawTests : BaseTestClass
{
    private const string Name = "RecordContext";

    [Theory]
    [InlineData("[Color=Red][Custom=RecordContext]b[/Custom][/Color]", 255, 0, 0)]
    [InlineData("a[Color=Blue]x[Custom=RecordContext]b[/Custom][/Color]", 0, 0, 255)]
    [InlineData("[Red=200][Custom=RecordContext]b[/Custom][/Red]", 200, 255, 255)]
    [InlineData("[Color=Red][Green=128][Custom=RecordContext]b[/Custom][/Green][/Color]", 255, 128, 0)]
    public void ContextCustom_InsideColorTags_ReceivesTheEnclosingColor(string markup, int r, int g, int b)
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(markup, textColor: null);

        calls.Count.ShouldBeGreaterThan(0);
        System.Drawing.Color? color = calls[0].Context.Color;
        color.ShouldNotBeNull();
        (color!.Value.R, color.Value.G, color.Value.B).ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("[Alpha=128][Custom=RecordContext]b[/Custom][/Alpha]", 128, 255, 255, 255)]
    [InlineData("[Color=Red][Alpha=64][Custom=RecordContext]b[/Custom][/Alpha][/Color]", 64, 255, 0, 0)]
    public void ContextCustom_InsideAlphaTag_ReceivesTheEnclosingAlpha(string markup, int a, int r, int g, int b)
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(markup, textColor: null);

        calls.Count.ShouldBeGreaterThan(0);
        System.Drawing.Color? color = calls[0].Context.Color;
        color.ShouldNotBeNull();
        (color!.Value.A, color.Value.R, color.Value.G, color.Value.B).ShouldBe(((byte)a, (byte)r, (byte)g, (byte)b));
    }

    [Fact]
    public void ContextCustom_WithNoColorTag_ReceivesTheTextColor()
    {
        List<(int Index, LetterCustomization Context)> calls =
            DrawAndRecord("a[Custom=RecordContext]b[/Custom]", textColor: System.Drawing.Color.FromArgb(255, 10, 20, 30));

        calls.Count.ShouldBeGreaterThan(0);
        System.Drawing.Color? color = calls[0].Context.Color;
        color.ShouldNotBeNull();
        (color!.Value.R, color.Value.G, color.Value.B).ShouldBe(((byte)10, (byte)20, (byte)30));
    }

    [Theory]
    [InlineData("ab[Color=Red]c[Custom=RecordContext]d[/Custom][/Color]", 3)]
    [InlineData("a[Glyph=0x42][Custom=RecordContext]c[/Custom]", 2)]
    [InlineData("[Color=Red]ab[/Color]\n[Custom=RecordContext]c[/Custom]", 3)]
    public void ContextCustom_AfterTagsAndGlyphs_ReceivesTheDisplayedIndex(string markup, int expectedIndex)
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(markup, textColor: null);

        calls.Count.ShouldBeGreaterThan(0);
        calls[0].Index.ShouldBe(expectedIndex);
    }

    private static List<(int Index, LetterCustomization Context)> DrawAndRecord(string markup, System.Drawing.Color? textColor)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        List<(int, LetterCustomization)> calls = new();
        Text.ContextCustomizations[Name] = (index, block, context) =>
        {
            calls.Add((index, context));
            return new LetterCustomization();
        };

        SystemManagers managers = SystemManagers.Default;
        TextRuntime text = new();
        try
        {
            text.X = 10;
            text.Y = 10;
            if (textColor != null)
            {
                text.Color = new XnaColor(textColor.Value.R, textColor.Value.G, textColor.Value.B, textColor.Value.A);
            }
            text.Text = markup;
            text.AddToManagers(managers, null);
            text.UpdateLayout();

            using RenderTarget2D capture = new(game.GraphicsDevice, 100, 60);
            game.GraphicsDevice.SetRenderTarget(capture);
            game.GraphicsDevice.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
            game.GraphicsDevice.SetRenderTarget(null);
        }
        finally
        {
            text.RemoveFromManagers();
            Text.ContextCustomizations.Remove(Name);
        }
        return calls;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
