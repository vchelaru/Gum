using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Code-only V3 text names the font it draws. The embedded default font is rendered from Liberation Sans (#5434),
/// so asking for "Liberation Sans" at 18 resolves that same embedded atlas with no font creator wired, and "Arial"
/// keeps resolving it for older code.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class V3DefaultTextFontTests : BaseTestClass
{
    [Fact]
    public void V3TextStyles_NameLiberationSans18_InTheirStyle()
    {
        using V3Lease game = new();
        V3Styling styling = V3Styling.ActiveStyle;

        AssertFont(styling.Text.Normal, isBold: false, isItalic: false);
        AssertFont(styling.Text.Strong, isBold: true, isItalic: false);
        AssertFont(styling.Text.Emphasis, isBold: false, isItalic: true);

        static void AssertFont(StateSave state, bool isBold, bool isItalic)
        {
            state.GetValue("Font").ShouldBe("Liberation Sans");
            state.GetValue("FontSize").ShouldBe(18);
            state.GetValue("IsBold").ShouldBe(isBold);
            state.GetValue("IsItalic").ShouldBe(isItalic);
        }
    }

    [Theory]
    [InlineData("Liberation Sans", false, false, "Font18Arial.fnt")]
    [InlineData("Liberation Sans", true, false, "Font18Arial_Bold.fnt")]
    [InlineData("Liberation Sans", false, true, "Font18Arial_Italic.fnt")]
    [InlineData("Liberation Sans", true, true, "Font18Arial_Italic_Bold.fnt")]
    [InlineData("Arial", true, false, "Font18Arial_Bold.fnt")]
    public void Text_At18_ResolvesTheEmbeddedFont_WithNoFontCreatorAndNoReport(string font, bool isBold, bool isItalic, string embeddedFile)
    {
        using V3Lease game = new();
        List<string> reports = new();
        void Handler(string message) => reports.Add(message);
        global::Gum.Wireframe.CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            TextRuntime text = new TextRuntime();
            text.Font = font;
            text.FontSize = 18;
            text.IsBold = isBold;
            text.IsItalic = isItalic;

            BitmapFont embedded = (BitmapFont)LoaderManager.Self.GetDisposable("EmbeddedResource.MonoGameGum.Content." + embeddedFile)!;
            text.BitmapFont.ShouldBeSameAs(embedded);
            reports.ShouldBeEmpty();
        }
        finally
        {
            global::Gum.Wireframe.CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    // The ladder rungs other than 18 (#8509) need a font creator, like any size the embedded atlas lacks. With none,
    // the text keeps the embedded default font and the missing font is reported.
    [Fact]
    public void V3HeadingStyle_WithNoFontCreator_FallsBackToTheEmbeddedFontAndReports()
    {
        using V3Lease game = new();
        StateSave h1 = V3Styling.ActiveStyle.Text.H1;
        h1.GetValue("FontSize").ShouldBe(28);
        List<string> reports = new();
        void Handler(string message) => reports.Add(message);
        global::Gum.Wireframe.CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            TextRuntime text = new TextRuntime();
            text.ApplyState(h1);

            BitmapFont embedded = (BitmapFont)LoaderManager.Self.GetDisposable("EmbeddedResource.MonoGameGum.Content.Font18Arial.fnt")!;
            text.BitmapFont.ShouldBeSameAs(embedded);
            reports.ShouldNotBeEmpty();
        }
        finally
        {
            global::Gum.Wireframe.CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    // The before/after proof for the family switch: a code-only screen built with the styles' old family ("Arial")
    // and with "Liberation Sans" lays out to the same sizes and draws the same pixels. GUM_SCREENSHOT_DIR keeps both.
    [Fact]
    public void V3Screen_WithLiberationSans_MatchesTheArialLayoutAndPixels()
    {
        using V3Lease game = new();
        (List<(float Width, float Height)> arialSizes, XnaColor[] arialPixels) = RenderControls(game, "Arial", "v3-font-before-arial.png");
        (List<(float Width, float Height)> liberationSizes, XnaColor[] liberationPixels) = RenderControls(game, "Liberation Sans", "v3-font-after-liberation-sans.png");

        liberationSizes.ShouldBe(arialSizes);
        liberationPixels.ShouldBe(arialPixels);
    }

    private static (List<(float Width, float Height)> Sizes, XnaColor[] Pixels) RenderControls(V3Lease game, string family, string pngName)
    {
        V3Styling styling = V3Styling.ActiveStyle;
        StateSave[] states = { styling.Text.Normal, styling.Text.Strong, styling.Text.Emphasis };
        List<object?> saved = new();
        foreach (StateSave state in states)
        {
            saved.Add(state.GetValue("Font"));
            state.SetValue("Font", family, "string");
        }

        try
        {
            StackPanel panel = new StackPanel();
            panel.Spacing = 8;
            panel.X = 16;
            panel.Y = 16;
            Label label = new Label { Text = "Gold: 30" };
            Button button = new Button { Text = "Fight monster" };
            ListBox listBox = new ListBox();
            listBox.Height = 80;
            listBox.Items!.Add("Sword");
            listBox.Items!.Add("Shield");
            TextBox textBox = new TextBox { Text = "Name" };
            FrameworkElement[] controls = { label, button, listBox, textBox };
            foreach (FrameworkElement control in controls)
            {
                panel.AddChild(control);
            }
            panel.AddToRoot();

            XnaColor[] pixels = Rendering.MenuFocusRenderTests.Capture(game.GraphicsDevice, new XnaColor(32, 32, 32), pngName, 320, 320);
            List<(float Width, float Height)> sizes = new();
            foreach (FrameworkElement control in controls)
            {
                sizes.Add((control.Visual.GetAbsoluteWidth(), control.Visual.GetAbsoluteHeight()));
            }
            panel.RemoveFromRoot();
            return (sizes, pixels);
        }
        finally
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i].SetValue("Font", saved[i], "string");
            }
        }
    }

    private sealed class V3Lease : SharedGameLease
    {
        public V3Lease()
        {
            global::Gum.GumService.Default.Initialize(Game, DefaultVisualsVersion.V3);
            global::Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        }

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
