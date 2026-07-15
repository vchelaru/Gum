using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2.GueDeriving;

/// <summary>
/// Pins #3703: CustomFontFile pointing at a .ttf/.otf should route through the same bake
/// cascade Font-as-path uses, instead of the .fnt-only BitmapFont load.
/// </summary>
public class TextRuntimeCustomFontFileTtfTests
{
    [Fact]
    public void SettingCustomFontFileToTtf_PassesFontFileToInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyCustomFont.ttf";

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.FontFile.ShouldNotBeNullOrEmpty();
            creator.LastBmfcSave.FontFile.ShouldEndWith("MyCustomFont.ttf");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    [Fact]
    public void SettingCustomFontFileToTtf_WithNoGumProjectLoaded_ResolvesAgainstFileManagerRelativeDirectory()
    {
        // Pins the code-only-game bug: with no .gumx loaded (ObjectFinder.Self.GumProjectSave is
        // null), ResolveFontFilePath must fall back to FileManager.RelativeDirectory instead of
        // leaving the path unresolved -- otherwise KernSmith/bmfont.exe receive a relative path
        // that resolves against the process's current working directory (which may not be the
        // exe's own directory), not the documented CustomFontFile contract.
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        string previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;

        try
        {
            Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldBeNull();
            ToolsUtilities.FileManager.RelativeDirectory =
                System.IO.Path.Combine(AppContext.BaseDirectory, "Content") + System.IO.Path.DirectorySeparatorChar;

            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyCustomFont.ttf";

            creator.LastBmfcSave.ShouldNotBeNull();
            System.IO.Path.IsPathRooted(creator.LastBmfcSave!.FontFile).ShouldBeTrue();
            creator.LastBmfcSave.FontFile.ShouldBe(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "MyCustomFont.ttf"));
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
            ToolsUtilities.FileManager.RelativeDirectory = previousRelativeDirectory;
        }
    }

    [Fact]
    public void SettingCustomFontFileToFnt_DoesNotConsultInMemoryFontCreator()
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previous = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            TextRuntime text = new TextRuntime();
            text.UseCustomFont = true;
            text.FontSize = 24;
            text.CustomFontFile = "Fonts/MyPrebakedFont.fnt";

            creator.LastBmfcSave.ShouldBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previous;
        }
    }

    private sealed class CapturingInMemoryFontCreator : IInMemoryFontCreator
    {
        public BmfcSave? LastBmfcSave { get; private set; }

        public RenderingLibrary.Graphics.BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            LastBmfcSave = bmfcSave;
            return null;
        }
    }
}
