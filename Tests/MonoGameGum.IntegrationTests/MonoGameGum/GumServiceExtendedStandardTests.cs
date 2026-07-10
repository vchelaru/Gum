using Gum.Managers;
using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Regression tests for a runtime crash where a project that declares a Skia-only standard element
/// (Canvas/Svg/LottieAnimation) threw "Could not get the default state for type Canvas" from
/// GumService.Initialize on a non-Skia runtime. #3507 changed the shape runtime's default-state
/// resolver to return null for types it does not own — correct for the Gum tool, where the Skia
/// plugin resolves them, but it removed the Container fallback the game runtime had relied on to
/// tolerate these declared-but-never-rendered standards. GumService now registers the extended
/// default states at load so they resolve instead of throwing.
/// </summary>
public class GumServiceExtendedStandardTests : BaseTestClass, IDisposable
{
    private string? _tempDirectory;

    public override void Dispose()
    {
        base.Dispose();

        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort - file handles may still be held during test teardown.
            }
        }
    }

    [Fact]
    public void Initialize_ResolvesCanvasStandard_WhenProjectDeclaresCanvasStandard()
    {
        _tempDirectory = CreateTempDirectory();
        string gumxPath = WriteProjectDeclaringStandard(_tempDirectory, "Canvas");

        using GameForExtendedStandardTest game = new GameForExtendedStandardTest(gumxPath);
        game.RunOneFrame();

        game.GumService.LastLoadResult.ShouldNotBeNull();
        game.GumService.LastLoadResult!.ErrorMessage.ShouldBeNullOrEmpty();
        StandardElementsManager.Self.TryGetDefaultStateFor("Canvas", throwExceptionOnMissing: false)
            .ShouldNotBeNull();
    }

    [Fact]
    public void Initialize_ResolvesCanvasStandard_AfterUninitializeAndReinitialize()
    {
        _tempDirectory = CreateTempDirectory();
        string gumxPath = WriteProjectDeclaringStandard(_tempDirectory, "Canvas");

        using GameForExtendedStandardTest game = new GameForExtendedStandardTest(gumxPath);
        game.RunOneFrame();
        StandardElementsManager.Self.TryGetDefaultStateFor("Canvas", throwExceptionOnMissing: false)
            .ShouldNotBeNull();

        // The extended-state registration guard and the wired resolver are process-global and are
        // NOT reset by Uninitialize, so a second Initialize must still resolve Canvas. If a future
        // change makes Uninitialize clear StandardElementsManager state, this pins that it must also
        // re-register.
        game.GumService.Uninitialize();
        game.GumService.Initialize(game, gumxPath);

        StandardElementsManager.Self.TryGetDefaultStateFor("Canvas", throwExceptionOnMissing: false)
            .ShouldNotBeNull();
    }

    #region Helpers

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumExtendedStandardTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static string WriteProjectDeclaringStandard(string tempDirectory, string standardName)
    {
        string standardsDir = Path.Combine(tempDirectory, "Standards");
        Directory.CreateDirectory(standardsDir);
        File.WriteAllText(Path.Combine(standardsDir, $"{standardName}.gutx"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <StandardElementSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>{standardName}</Name>
              <State>
                <Name>Default</Name>
              </State>
              <Behaviors />
            </StandardElementSave>
            """);

        string gumxPath = Path.Combine(tempDirectory, "Project.gumx");
        File.WriteAllText(gumxPath, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>600</DefaultCanvasHeight>
              <StandardElementReference Name="{standardName}" />
            </GumProjectSave>
            """);
        return gumxPath;
    }

    #endregion

    #region Test Game

    private class GameForExtendedStandardTest : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _gumProjectFile;
        public Gum.GumService GumService { get; private set; }

        public GameForExtendedStandardTest(string gumProjectFile)
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _gumProjectFile = gumProjectFile;
            GumService = new Gum.GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, _gumProjectFile);
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }

    #endregion
}
