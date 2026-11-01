using System;
using System.IO;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumPreview;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.IntegrationTests.GumPreview;

/// <summary>
/// #7315: a project whose Text uses a font file (every Forms theme) rendered in the default sans in the
/// Preview window whenever the tool had not yet written that font's FontCache file, because the Preview
/// had no way to bake one itself.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PreviewFontCreatorTests : BaseTestClass
{
    [Fact]
    public void Register_TextWithFontFileAndNoFontCache_GetsTheFontFileInsteadOfTheDefault()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        string previousRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            FileManager.RelativeDirectory = Path.Combine(AppContext.BaseDirectory, "Content") + Path.DirectorySeparatorChar;
            ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = GumProjectSave.NativeVersion };
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            PreviewFontCreator.Register(game.GraphicsDevice);

            string? error = null;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += m => error = m;
            TextRuntime text = new TextRuntime();
            game.GumService.Root.Children.Add(text);
            text.FontSize = 24;
            text.Font = "Fonts/Orbitron-Black.ttf";

            error.ShouldBeNull();
            text.Typeface.ShouldNotBeNull();
            text.Typeface.ShouldNotBeSameAs(RenderingLibrary.Graphics.Text.DefaultBitmapFont);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
            ObjectFinder.Self.GumProjectSave = previousProject;
            FileManager.RelativeDirectory = previousRelativeDirectory;
        }
    }

    [Fact]
    public void TryCreateFont_FontAlreadyInFontCache_DeclinesSoTheToolsFileLoads()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        string previousRelativeDirectory = FileManager.RelativeDirectory;
        string directory = Path.Combine(Path.GetTempPath(), "gum7315_" + Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        try
        {
            Directory.CreateDirectory(directory + "FontCache");
            FileManager.RelativeDirectory = directory;
            PreviewFontCreator creator = new PreviewFontCreator(game.GraphicsDevice);
            BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 18, UseSmoothing = true, Ranges = "65" };
            File.WriteAllText(directory + bmfcSave.FontCacheFileName, "info face=\"Arial\"");

            creator.TryCreateFont(bmfcSave).ShouldBeNull();
        }
        finally
        {
            FileManager.RelativeDirectory = previousRelativeDirectory;
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public Gum.GumService GumService { get; } = new Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
