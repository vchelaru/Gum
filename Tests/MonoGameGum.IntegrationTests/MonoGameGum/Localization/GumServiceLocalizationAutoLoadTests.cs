using Gum.Localization;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Localization;

/// <summary>
/// Integration tests verifying that GumService.Initialize auto-loads the
/// project's LocalizationFile for both CSV and RESX formats.
/// </summary>
public class GumServiceLocalizationAutoLoadTests : BaseTestClass, IDisposable
{
    private string? _tempDirectory;

    public override void Dispose()
    {
        // Reset the shared static localization service so each test starts clean.
        CustomSetPropertyOnRenderable.LocalizationService = null;

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
    public void Initialize_ShouldAutoLoadCsvLocalizationFile()
    {
        _tempDirectory = CreateTempDirectory();

        string csv = "String ID,English,Spanish\nT_OK,OK,Aceptar\n";
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"), csv);

        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        File.WriteAllText(gumxPath, BuildMinimalGumx(localizationFile: "Strings.csv"));

        using GameForLocalizationTest game = new GameForLocalizationTest(gumxPath);
        game.RunOneFrame();

        ILocalizationService? service = CustomSetPropertyOnRenderable.LocalizationService;
        service.ShouldNotBeNull();
        service.Languages.Count.ShouldBe(2);

        service.CurrentLanguage = 1;
        service.Translate("T_OK").ShouldBe("OK");
        service.CurrentLanguage = 2;
        service.Translate("T_OK").ShouldBe("Aceptar");
    }

    [Fact]
    public void Initialize_ShouldAutoLoadResxLocalizationFile()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });

        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        File.WriteAllText(gumxPath, BuildMinimalGumx(localizationFile: "Strings.resx"));

        using GameForLocalizationTest game = new GameForLocalizationTest(gumxPath);
        game.RunOneFrame();

        ILocalizationService? service = CustomSetPropertyOnRenderable.LocalizationService;
        service.ShouldNotBeNull();
        service.Languages.Count.ShouldBeGreaterThanOrEqualTo(1);
        service.CurrentLanguage = 1;
        service.Translate("T_OK").ShouldBe("OK");
    }

    [Fact]
    public void Initialize_ShouldAutoLoadResxWithSatellites()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_OK", "Aceptar" }
        });

        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        File.WriteAllText(gumxPath, BuildMinimalGumx(localizationFile: "Strings.resx"));

        using GameForLocalizationTest game = new GameForLocalizationTest(gumxPath);
        game.RunOneFrame();

        ILocalizationService? service = CustomSetPropertyOnRenderable.LocalizationService;
        service.ShouldNotBeNull();
        service.Languages.Count.ShouldBe(2);

        service.CurrentLanguage = 1;
        service.Translate("T_OK").ShouldBe("OK");
        service.CurrentLanguage = 2;
        service.Translate("T_OK").ShouldBe("Aceptar");
    }

    #region Helpers

    private static string BuildMinimalGumx(string localizationFile)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<GumProjectSave xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine("  <DefaultCanvasWidth>800</DefaultCanvasWidth>");
        sb.AppendLine("  <DefaultCanvasHeight>600</DefaultCanvasHeight>");
        sb.AppendLine($"  <LocalizationFile>{localizationFile}</LocalizationFile>");
        sb.AppendLine("</GumProjectSave>");
        return sb.ToString();
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumAutoLoadTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void WriteResxFile(string filePath, Dictionary<string, string> entries)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<root>");
        sb.AppendLine("  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>");
        sb.AppendLine("  <resheader name=\"version\"><value>2.0</value></resheader>");
        foreach (KeyValuePair<string, string> entry in entries)
        {
            sb.AppendLine($"  <data name=\"{entry.Key}\" xml:space=\"preserve\">");
            sb.AppendLine($"    <value>{entry.Value}</value>");
            sb.AppendLine("  </data>");
        }
        sb.AppendLine("</root>");
        File.WriteAllText(filePath, sb.ToString());
    }

    #endregion

    #region Test Game

    private class GameForLocalizationTest : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _gumProjectFile;
        public GumService GumService { get; private set; }

        public GameForLocalizationTest(string gumProjectFile)
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _gumProjectFile = gumProjectFile;
            GumService = new GumService();
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
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }

    #endregion
}
