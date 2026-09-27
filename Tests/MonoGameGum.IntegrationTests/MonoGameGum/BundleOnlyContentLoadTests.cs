using Gum.Bundle;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A desktop game that loads a <c>.gumpkg</c> has no loose content files: every texture, font page
/// and animation frame is served by the bundle's <see cref="FileManager.CustomGetStreamFromFile"/>
/// hook. These load each content type through the real MonoGame loaders with a real GraphicsDevice
/// (#5225), and pin that a loose file still wins over a bundle hook that doesn't serve it.
/// </summary>
public class BundleOnlyContentLoadTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly string _previousRelativeDirectory;
    private readonly Func<string, Stream>? _previousHook;

    public BundleOnlyContentLoadTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumBundleOnly_" + Guid.NewGuid().ToString("N"));
        _previousRelativeDirectory = FileManager.RelativeDirectory;
        _previousHook = FileManager.CustomGetStreamFromFile;
    }

    public override void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(_projectDirectory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    // GumService.Initialize(Game) hands the loader the game's ContentManager; the GraphicsDevice-only
    // overload (a host with no Game) leaves it null, which is the path that skipped the hook.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BundledContent_WithNoLooseFiles_ShouldLoadSpriteNineSliceFontPageAndAnimationFrame(bool initializeWithGame)
    {
        using MinimalGame game = new(initializeWithGame);
        game.RunOneFrame();

        byte[] png = CreatePng(game.GraphicsDevice, width: 4, height: 2);
        string fnt =
            "info face=\"Bundled\" size=-14 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=16 base=12 scaleW=4 scaleH=2 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            "page id=0 file=\"FontPage.png\"\n" +
            "chars count=1\n" +
            "char id=65 x=0 y=0 width=2 height=2 xoffset=0 yoffset=0 xadvance=3 page=0 chnl=15\n";
        string achx =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<AnimationChainArraySave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <FileRelativeTextures>true</FileRelativeTextures>\n" +
            "  <TimeMeasurementUnit>Second</TimeMeasurementUnit>\n" +
            "  <CoordinateType>Pixel</CoordinateType>\n" +
            "  <AnimationChain><Name>Idle</Name><Frame><TextureName>Frame.png</TextureName><FrameLength>0.1</FrameLength>" +
            "<LeftCoordinate>0</LeftCoordinate><RightCoordinate>4</RightCoordinate><TopCoordinate>0</TopCoordinate><BottomCoordinate>2</BottomCoordinate></Frame></AnimationChain>\n" +
            "</AnimationChainArraySave>\n";
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            ("Sprite.png", png),
            ("NineSlice.png", png),
            ("Fonts/Bundled.fnt", Encoding.UTF8.GetBytes(fnt)),
            ("Fonts/FontPage.png", png),
            ("Animations/Idle.achx", Encoding.UTF8.GetBytes(achx)),
            ("Animations/Frame.png", png),
        });

        SpriteRuntime sprite = new() { SourceFileName = "Sprite.png" };
        NineSliceRuntime nineSlice = new() { SourceFileName = "NineSlice.png" };
        TextRuntime text = new() { UseCustomFont = true, CustomFontFile = "Fonts/Bundled.fnt" };
        SpriteRuntime animated = new() { SourceFileName = "Animations/Idle.achx" };

        sprite.Texture.ShouldNotBeNull();
        sprite.Texture!.Width.ShouldBe(4);
        ((NineSlice)nineSlice.RenderableComponent).CenterTexture.ShouldNotBeNull();
        BitmapFont? font = ((Text)text.RenderableComponent).BitmapFont;
        font.ShouldNotBeNull();
        font!.Textures[0].ShouldNotBeNull();
        font.Textures[0].ShouldNotBeSameAs(Sprite.InvalidTexture);
        font.Textures[0]!.Width.ShouldBe(4);
        ((Sprite)animated.RenderableComponent).AnimationChains![0][0].Texture.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LooseTexture_WhenBundleHookDoesNotServeIt_ShouldStillLoadFromDisk(bool initializeWithGame)
    {
        using MinimalGame game = new(initializeWithGame);
        game.RunOneFrame();

        byte[] png = CreatePng(game.GraphicsDevice, width: 3, height: 5);
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
        });
        string loosePath = Path.Combine(_projectDirectory, "Loose.png");
        File.WriteAllBytes(loosePath, png);

        Texture2D loaded = LoaderManager.Self.LoadContent<Texture2D>(loosePath);

        loaded.ShouldNotBeNull();
        loaded.Width.ShouldBe(3);
    }

    private void InstallBundle(List<(string path, byte[] content)> entries)
    {
        Directory.CreateDirectory(_projectDirectory);
        string bundlePath = Path.Combine(_projectDirectory, "Bundled.gumpkg");
        using (FileStream stream = File.Create(bundlePath))
        {
            GumBundleWriter.Write(stream, entries);
        }

        ProjectResolution resolution = GumBundleLoader.Resolve(bundlePath);
        resolution.UsedBundle.ShouldBeTrue();
        // Only the bundle and its sibling test files are on disk; content must come from the hook.
        File.Exists(Path.Combine(_projectDirectory, "Sprite.png")).ShouldBeFalse();
        FileManager.RelativeDirectory = _projectDirectory + Path.DirectorySeparatorChar;
    }

    private static byte[] CreatePng(GraphicsDevice graphicsDevice, int width, int height)
    {
        using Texture2D source = new(graphicsDevice, width, height, false, SurfaceFormat.Color);
        XnaColor[] pixels = new XnaColor[width * height];
        Array.Fill(pixels, XnaColor.White);
        source.SetData(pixels);
        using MemoryStream stream = new();
        source.SaveAsPng(stream, width, height);
        return stream.ToArray();
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly bool _initializeWithGame;

        public MinimalGame(bool initializeWithGame)
        {
            _initializeWithGame = initializeWithGame;
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_initializeWithGame)
            {
                Gum.GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
            }
            else
            {
                Gum.GumService.Default.Initialize(GraphicsDevice);
            }
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
