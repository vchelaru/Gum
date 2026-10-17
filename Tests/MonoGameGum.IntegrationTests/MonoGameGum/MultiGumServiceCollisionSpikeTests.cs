using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Third spike for the epic on running several GumService instances in one process (#6066): name,
/// asset and lifecycle collisions between two services. Each test asserts the behavior we want, so a red
/// test is a collision; the failure message says which one. A test makes a service the default
/// (<c>IGumService.Default</c>) before building for it, as the service rule requires.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MultiGumServiceCollisionSpikeTests : BaseTestClass
{
    // Project A puts its content at (20,20) and B at (200,100); sizes below keep the two apart.
    private static readonly Point OriginA = new Point(20, 20);
    private static readonly Point OriginB = new Point(200, 100);

    private static readonly XnaColor Yellow = new XnaColor(255, 255, 0);
    private static readonly XnaColor Green = new XnaColor(0, 255, 0);

    private readonly string _root;
    private readonly IInMemoryFontCreator? _oldInMemoryFontCreator;
    private readonly IRuntimeFontService? _oldFontService;

    public MultiGumServiceCollisionSpikeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumCollisions_" + Guid.NewGuid().ToString("N"));
        // Other test classes leave font generators registered; these tests need fonts to come from the
        // .fnt files on disk, and nothing else.
        _oldInMemoryFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        _oldFontService = CustomSetPropertyOnRenderable.FontService;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        CustomSetPropertyOnRenderable.FontService = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        GraphicalUiElement.IsAllLayoutSuspended = false;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _oldInMemoryFontCreator;
        CustomSetPropertyOnRenderable.FontService = _oldFontService;
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    #region Harness checks

    // A lone service must load its own texture and fonts, otherwise the red tests below say nothing
    // about collisions.
    [Theory]
    [InlineData("SpriteScreen", "texture")]
    [InlineData("CacheFontScreen", "font")]
    [InlineData("CustomFontScreen", "font")]
    public void SingleService_Alone_ShouldLoadItsOwnAssets(string screenName, string kind)
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game, writeB: false);
        game.InitializeA(PathA);

        GraphicalUiElement screen = Build(game, useA: true, screenName);
        using CollisionCapture capture = new CollisionCapture(game.GraphicsDevice);
        capture.Draw(game.ServiceA, makeServiceDefault: true);
        XnaColor[] pixels = capture.Read();

        AssertOwnAssetPixels(pixels, OriginA, kind, expectRed: true, $"lone service, {screenName}");
    }

    #endregion

    #region 1. Names: same component, Standards, base type, behavior and screen names

    // Both projects define Card, Derived (BaseType Card, a custom component), MainScreen and their own
    // ColoredRectangle Standard (red default in A, blue default in B). Everything is resolved through
    // ObjectFinder, which follows the default service.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SameNamedComponentsStandardsAndBaseTypes_ShouldEachBuildTheirOwnDefinitions(bool aFirst)
    {
        using CollisionGame game = CreateGame();
        string pathA = WriteNamesProject("A", OriginA, standardDefault: (255, 0, 0), dot: (255, 255, 0));
        string pathB = WriteNamesProject("B", OriginB, standardDefault: (0, 0, 255), dot: (0, 255, 0));
        InitializeBoth(game, pathA, pathB, aFirst);

        // Build in the requested order.
        if (aFirst) { Build(game, true, "MainScreen"); Build(game, false, "MainScreen"); }
        else { Build(game, false, "MainScreen"); Build(game, true, "MainScreen"); }

        XnaColor[] pixels = DrawBoth(game, makeServiceDefault: true);

        AssertColors(pixels, new[]
        {
            ("A's Card.Fill takes A's ColoredRectangle Standard default (red)", OriginA.X + 50, OriginA.Y + 30, (Func<XnaColor, bool>)CollisionCapture.IsRed),
            ("A's Derived.Dot is A's yellow", OriginA.X + 8, OriginA.Y + 8, CollisionCapture.IsYellow),
            ("B's Card.Fill takes B's ColoredRectangle Standard default (blue)", OriginB.X + 50, OriginB.Y + 30, CollisionCapture.IsBlue),
            ("B's Derived.Dot is B's green", OriginB.X + 8, OriginB.Y + 8, CollisionCapture.IsGreen),
        });
    }

    // Both projects define the behavior ButtonBehavior with a different FormsProperty default for
    // IsEnabled (A false, B true). BehaviorFormsPropertyApplier reads them from ObjectFinder.Self at
    // construction time, so making the service the default should be enough.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SameNamedBehaviors_ShouldEachApplyTheirOwnFormsPropertyDefaults(bool aFirst)
    {
        using CollisionGame game = CreateGame();
        string pathA = WriteBehaviorProject("A", OriginA, isEnabledDefault: false);
        string pathB = WriteBehaviorProject("B", OriginB, isEnabledDefault: true);
        InitializeBoth(game, pathA, pathB, aFirst);

        GraphicalUiElement screenA;
        GraphicalUiElement screenB;
        if (aFirst) { screenA = Build(game, true, "MainScreen"); screenB = Build(game, false, "MainScreen"); }
        else { screenB = Build(game, false, "MainScreen"); screenA = Build(game, true, "MainScreen"); }

        Button? buttonA = FormsControlOf(screenA, "W") as Button;
        Button? buttonB = FormsControlOf(screenB, "W") as Button;
        buttonA.ShouldNotBeNull("A's component 'Btn' has ButtonBehavior, so it should be a Forms Button");
        buttonB.ShouldNotBeNull("B's component 'Btn' has ButtonBehavior, so it should be a Forms Button");
        buttonA.IsEnabled.ShouldBeFalse("A's ButtonBehavior declares IsEnabled default false");
        buttonB.IsEnabled.ShouldBeTrue("B's ButtonBehavior declares IsEnabled default true");
    }

    // The element-type table is keyed by element name. A's Widget has ButtonBehavior and B's Widget has
    // CheckBoxBehavior, so each service's table must give its own Widget its own runtime type.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SameNameDifferentFormsBehaviors_ShouldEachCreateTheirOwnControlType(bool aFirst)
    {
        using CollisionGame game = CreateGame();
        string pathA = WriteWidgetProject("A", OriginA, behavior: "ButtonBehavior");
        string pathB = WriteWidgetProject("B", OriginB, behavior: "CheckBoxBehavior");
        InitializeBoth(game, pathA, pathB, aFirst);

        GraphicalUiElement screenA;
        GraphicalUiElement screenB;
        if (aFirst) { screenA = Build(game, true, "MainScreen"); screenB = Build(game, false, "MainScreen"); }
        else { screenB = Build(game, false, "MainScreen"); screenA = Build(game, true, "MainScreen"); }

        List<string> failures = new();
        object? controlA = FormsControlOf(screenA, "W");
        object? controlB = FormsControlOf(screenB, "W");
        if (controlA is not Button) { failures.Add($"A's Widget (ButtonBehavior) became {controlA?.GetType().Name ?? "no Forms control"}"); }
        if (controlB is not CheckBox) { failures.Add($"B's Widget (CheckBoxBehavior) became {controlB?.GetType().Name ?? "no Forms control"}"); }
        failures.ShouldBeEmpty("each service's element-type table (ElementSaveExtensions) is keyed by element name: " + string.Join("; ", failures));
    }

    // A's Widget has no behavior (a plain container) and B's has ButtonBehavior. B's registration of
    // "Widget" must not turn A's plain Widget into a Forms Button too.
    [Fact]
    public void SameNamePlainVersusButton_ShouldKeepPlainComponentPlain()
    {
        using CollisionGame game = CreateGame();
        string pathA = WriteWidgetProject("A", OriginA, behavior: null);
        string pathB = WriteWidgetProject("B", OriginB, behavior: "ButtonBehavior");
        InitializeBoth(game, pathA, pathB, aFirst: true);

        GraphicalUiElement screenA = Build(game, true, "MainScreen");
        GraphicalUiElement screenB = Build(game, false, "MainScreen");

        FormsControlOf(screenB, "W").ShouldBeOfType<Button>("B's Widget has ButtonBehavior");
        FormsControlOf(screenA, "W").ShouldBeNull(
            "A's Widget has no behavior; it picked up B's registration of the same name in the element-type table");
    }

    #endregion

    #region 2. Assets: same relative file names, different content

    // A's screen is built after B initialized, so the relative name must still resolve against A's folder.
    [Fact]
    public void SameRelativeTextureName_ShouldLoadEachServicesOwnTexture()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        List<string> failures = new();
        try
        {
            Build(game, useA: true, "SpriteScreen");
            Build(game, useA: false, "SpriteScreen");
        }
        catch (Exception e)
        {
            Assert.Fail($"Building throws {e.GetType().Name}: {e.Message}");
        }
        XnaColor[] pixels = DrawBoth(game, makeServiceDefault: false);

        CollectOwnAssetFailures(failures, pixels, OriginA, "texture", expectRed: true, "A's Tex.png");
        CollectOwnAssetFailures(failures, pixels, OriginB, "texture", expectRed: false, "B's Tex.png");
        failures.ShouldBeEmpty("A's sprite must resolve Tex.png in A's folder, not B's: " + string.Join("; ", failures));
    }

    // LoadContent after the other service became the default, then A again: the cache must hand back A's
    // own texture, not B's.
    [Fact]
    public void LazyTextureLoad_AfterOtherServiceBecameDefault_ShouldReturnEachServicesOwnTexture()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        // Each load runs in its own service's scope (the service owns its cache and relative directory).
        Texture2D textureA;
        Texture2D textureB;
        Texture2D textureAAgain;
        global::RenderingLibrary.IGumService.Default = game.ServiceA; { textureA = LoaderManager.Self.LoadContent<Texture2D>("Tex.png")!; }
        global::RenderingLibrary.IGumService.Default = game.ServiceB; { textureB = LoaderManager.Self.LoadContent<Texture2D>("Tex.png")!; }
        global::RenderingLibrary.IGumService.Default = game.ServiceA; { textureAAgain = LoaderManager.Self.LoadContent<Texture2D>("Tex.png")!; }

        List<string> failures = new();
        if (ReferenceEquals(textureA, textureB)) { failures.Add("A and B got the same Texture2D instance for 'Tex.png'"); }
        if (!CollisionCapture.IsRed(FirstPixel(textureA))) { failures.Add($"A's texture pixel is {CollisionCapture.Describe(FirstPixel(textureA))}, not red"); }
        if (!CollisionCapture.IsBlue(FirstPixel(textureB))) { failures.Add($"B's texture pixel is {CollisionCapture.Describe(FirstPixel(textureB))}, not blue"); }
        if (!ReferenceEquals(textureA, textureAAgain)) { failures.Add("reloading under A did not come back from the cache as A's texture"); }
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // Fonts resolved from Font + FontSize use the cache name FontCache/Font31Bogus.fnt, made absolute
    // against RelativeDirectory, so the cache key differs per project folder.
    [Fact]
    public void SameRelativeFontCacheName_ShouldLoadEachServicesOwnFont()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        Build(game, true, "CacheFontScreen");
        Build(game, false, "CacheFontScreen");
        XnaColor[] pixels = DrawBoth(game, makeServiceDefault: true);

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "A's FontCache/Font31Bogus.fnt");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "B's FontCache/Font31Bogus.fnt");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // CustomFontFile fonts are cached under the raw relative name ("Fonts/TestFont.fnt", see
    // CustomSetPropertyOnRenderable.UpdateToFontValues), not the absolute path, so the cache must be
    // per service or the second project gets the first project's font.
    [Fact]
    public void SameRelativeCustomFontFile_ShouldLoadEachServicesOwnFont()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        Build(game, true, "CustomFontScreen");
        Build(game, false, "CustomFontScreen");
        XnaColor[] pixels = DrawBoth(game, makeServiceDefault: true);

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "A's Fonts/TestFont.fnt");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "B's Fonts/TestFont.fnt");
        failures.ShouldBeEmpty("CustomFontFile fonts are cached under the raw relative name, so the cache must not be shared across projects: " + string.Join("; ", failures));
    }

    #endregion

    #region 3. Lazy work after Initialize

    // Everything is built lazily, after both services initialized: A's screen after B was the last to
    // initialize, then B's, then A's again from a second screen instance.
    [Fact]
    public void ScreenBuiltInFirstService_AfterSecondServiceInitialized_ShouldMatchAScreenBuiltBeforeIt()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);

        // Reference: A alone.
        game.InitializeA(PathA);
        GraphicalUiElement lone = Build(game, true, "BothScreen");
        int loneCount = CountNodes(lone);
        lone.RemoveFromManagers();

        // Now B initializes, and A's screen is built lazily afterwards.
        game.InitializeB(PathB);
        GraphicalUiElement lazyA = Build(game, true, "BothScreen");
        GraphicalUiElement lazyB = Build(game, false, "BothScreen");
        XnaColor[] pixels = DrawBoth(game, makeServiceDefault: true);

        List<string> failures = new();
        if (CountNodes(lazyA) != loneCount) { failures.Add($"A's lazily built tree has {CountNodes(lazyA)} nodes, alone it has {loneCount}"); }
        CollectOwnAssetFailures(failures, pixels, OriginA, "texture", expectRed: true, "A's sprite (lazy)");
        CollectOwnAssetFailures(failures, pixels, OriginB, "texture", expectRed: false, "B's sprite (lazy)");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    #endregion

    #region 4. Text, fonts and context after construction

    // A's text is built while A is the default; B is the default when A is drawn. The text holds its
    // own BitmapFont, so drawing alone should not need A to be the default.
    [Fact]
    public void TextBuiltUnderA_DrawnWhileBIsDefault_ShouldStillUseAsFont()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        Build(game, true, "CacheFontScreen");
        Build(game, false, "CacheFontScreen");

        // B is the default service for both draws.
        XnaColor[] pixels;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            pixels = DrawBoth(game, makeServiceDefault: false);
        }

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "A's text drawn while B is the default");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "B's text drawn while B is the default");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // After construction each element should know which service it belongs to. EffectiveManagers
    // walks up to the root that AddToManagers set; the Text renderable keeps its own manager. The
    // Text, Polygon and Rectangle factories are registered by SystemManagers as closures over
    // "this", so each service's element-type table must hold its own.
    [Fact]
    public void ElementsBuiltUnderEachService_WhileTheOtherIsActive_ShouldRememberTheirOwnSystemManagers()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        GraphicalUiElement screenA = Build(game, true, "BothScreen");
        GraphicalUiElement screenB = Build(game, false, "BothScreen");
        IGumService.Default = game.ServiceB; // B is the default from here on

        List<string> failures = new();
        foreach ((string label, GraphicalUiElement screen, Gum.GumService service) in new[]
        {
            ("A", screenA, game.ServiceA), ("B", screenB, game.ServiceB),
        })
        {
            foreach (string childName in new[] { "Pic", "Label" })
            {
                GraphicalUiElement child = screen.GetGraphicalUiElementByName(childName)!;
                if (!ReferenceEquals(child.EffectiveManagers, service.SystemManagers))
                {
                    failures.Add($"{label}.{childName}.EffectiveManagers is not {label}'s SystemManagers");
                }
            }

            RenderingLibrary.Graphics.Text text = (RenderingLibrary.Graphics.Text)screen.GetGraphicalUiElementByName("Label")!.RenderableComponent!;
            object? textManagers = typeof(RenderingLibrary.Graphics.Text)
                .GetField("mManagers", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(text);
            if (!ReferenceEquals(textManagers, service.SystemManagers))
            {
                failures.Add($"{label}'s Text renderable keeps {(textManagers == null ? "no" : "another")} SystemManagers " +
                    "(the Text factory in the element-type table closes over the SystemManagers of whichever service registered last)");
            }
        }
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // An element built with addToManagers:false has no Managers until it is added, so it remembers
    // nothing about the service that built it (the Text renderable still keeps its manager).
    [Fact]
    public void ElementBuiltWithoutAddToManagers_ShouldKnowWhichServiceBuiltIt()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        IGumService.Default = game.ServiceA;
        GraphicalUiElement detachedA = game.ProjectA!.Screens.First(s => s.Name == "BothScreen")
            .ToGraphicalUiElement(game.ServiceA.SystemManagers, addToManagers: false);

        // By design EffectiveManagers stays null until the element is attached: several controls read
        // "EffectiveManagers != null" as "attached". The building service is remembered separately.
        detachedA.EffectiveManagers.ShouldBeNull();
        Gum.GumContext.OwnerOf(detachedA).ShouldBeSameAs(game.ServiceA.Context,
            "an element built for service A but not yet added anywhere should still know A built it");
    }

    // The font is resolved again when a font property changes after construction. It is resolved
    // against whatever FileManager.RelativeDirectory and ObjectFinder hold at that moment, not the
    // project the element came from.
    [Fact]
    public void FontSizeChangedAfterConstruction_WhileOtherServiceActive_ShouldResolveAgainstTheElementsOwnProject()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        GraphicalUiElement screenA = Build(game, true, "CacheFontScreen");
        Build(game, false, "CacheFontScreen");

        // B is the active service; A's text switches to FontCache/Font32Bogus.fnt (A yellow, B green).
        XnaColor[] pixels;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            GraphicalUiElement labelA = screenA.GetGraphicalUiElementByName("Label")!;
            labelA.SetProperty("FontSize", 32);
            screenA.UpdateLayout();

            pixels = DrawBoth(game, makeServiceDefault: false);
        }
        Rectangle textArea = new Rectangle(OriginA.X, OriginA.Y + 50, 120, 30);
        int yellow = CollisionCapture.Count(pixels, textArea, CollisionCapture.IsYellow);
        int green = CollisionCapture.Count(pixels, textArea, CollisionCapture.IsGreen);
        yellow.ShouldBeGreaterThan(100,
            $"A's text should load A's Font32Bogus (yellow); it has {yellow} yellow and {green} green pixels, so the font was resolved in B's folder");
    }

    // An element built for A and never attached to managers has no EffectiveManagers, but the service that
    // built it is remembered (GumContext.OwnerOf), so a font change made while B is active still resolves in A.
    [Fact]
    public void FontSizeChangedOnDetachedElementBuiltForA_WhileOtherServiceActive_ShouldResolveAgainstTheBuildingServicesProject()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        ScreenSave screenSave = game.ProjectA!.Screens.First(item => item.Name == "CacheFontScreen");
        GraphicalUiElement detached = screenSave.ToGraphicalUiElement(game.ServiceA.SystemManagers, addToManagers: false);
        detached.EffectiveManagers.ShouldBeNull("setup: the element is not attached to any managers");

        string fontFile;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            GraphicalUiElement label = detached.GetGraphicalUiElementByName("Label")!;
            label.SetProperty("FontSize", 32);
            fontFile = ((RenderingLibrary.Graphics.Text)label.RenderableComponent!).BitmapFont!.FontFile!;
        }

        string folderA = Path.GetDirectoryName(PathA)!.Replace(Path.DirectorySeparatorChar, '/');
        fontFile.Replace(Path.DirectorySeparatorChar, '/').ShouldStartWith(folderA, Case.Insensitive, "the font should come from A's project folder, not B's");
    }

    // Same for a Sprite whose SourceFile is reassigned after construction.
    [Fact]
    public void SpriteSourceFileChangedAfterConstruction_WhileOtherServiceActive_ShouldResolveAgainstTheElementsOwnProject()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);

        GraphicalUiElement screenA = Build(game, true, "SpriteScreen");
        Build(game, false, "SpriteScreen");

        XnaColor[] pixels;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            GraphicalUiElement picA = screenA.GetGraphicalUiElementByName("Pic")!;
            try
            {
                picA.SetProperty("SourceFile", "Tex2.png"); // A: yellow, B: green
            }
            catch (Exception e)
            {
                Assert.Fail($"Reassigning A's SourceFile while B is active throws {e.GetType().Name}: {e.Message}");
            }
            screenA.UpdateLayout();

            pixels = DrawBoth(game, makeServiceDefault: false);
        }
        XnaColor center = CollisionCapture.At(pixels, OriginA.X + 20, OriginA.Y + 20);
        CollisionCapture.IsYellow(center).ShouldBeTrue(
            $"A's sprite should show A's Tex2.png (yellow) but shows {CollisionCapture.Describe(center)}; the relative name resolved in the active service's folder");
    }

    #endregion

    #region 5. Initialize / Uninitialize cycles and shared resources

    // A then B initialized, then one is uninitialized and the other must still draw.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UninitializingOneService_ShouldLeaveTheOtherAbleToDraw(bool uninitializeA)
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);
        Build(game, true, "BothScreen");
        Build(game, false, "BothScreen");

        Gum.GumService leaving = uninitializeA ? game.ServiceA : game.ServiceB;
        Gum.GumService staying = uninitializeA ? game.ServiceB : game.ServiceA;
        Point stayingOrigin = uninitializeA ? OriginB : OriginA;

        try { leaving.Uninitialize(); }
        catch (Exception e) { Assert.Fail($"Uninitialize throws {e.GetType().Name}: {e.Message}"); }

        XnaColor[] pixels;
        try
        {
            using CollisionCapture capture = new CollisionCapture(game.GraphicsDevice);
            capture.Draw(staying, makeServiceDefault: true);
            pixels = capture.Read();
        }
        catch (Exception e)
        {
            Assert.Fail($"Drawing the remaining service after the other uninitialized throws {e.GetType().Name}: {e.Message}");
            return;
        }

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, stayingOrigin, "texture", expectRed: !uninitializeA, "remaining service's sprite");
        CollectOwnAssetFailures(failures, pixels, stayingOrigin, "font", expectRed: !uninitializeA, "remaining service's text");
        failures.ShouldBeEmpty("the remaining service lost resources when the other uninitialized (LoaderManager.DisposeAndClear and Sprite.InvalidTexture are shared): " + string.Join("; ", failures));
    }

    // Resources that belong to the service that stays: nothing it owns should be disposed by the
    // other service's Uninitialize.
    [Fact]
    public void UninitializingFirstService_ShouldNotDisposeSecondServicesResources()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);
        Build(game, true, "BothScreen");
        Build(game, false, "BothScreen");

        // B's own resources: its loaded texture and font page, its default font, its invalid-texture.
        Texture2D textureB;
        BitmapFontHandle defaultFontB;
        Texture2D? invalidB;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            textureB = LoaderManager.Self.LoadContent<Texture2D>("Tex.png")!;
            defaultFontB = new(RenderingLibrary.Graphics.Text.DefaultBitmapFont);
            invalidB = RenderingLibrary.Graphics.Sprite.InvalidTexture;
        }

        game.ServiceA.Uninitialize();

        List<string> failures = new();
        if (textureB.IsDisposed) { failures.Add("B's loaded Tex.png was disposed (shared LoaderManager.DisposeAndClear)"); }
        if (defaultFontB.IsDisposed) { failures.Add("B's default font texture was disposed"); }
        if (invalidB != null && invalidB.IsDisposed) { failures.Add("B's Sprite.InvalidTexture was disposed (UninitializePlatform disposes whichever Sprite.InvalidTexture is static; only set when LoaderManager.Initialize ran, so unexercised here)"); }
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // Re-initialize A after it was uninitialized while B stayed alive, then draw both.
    [Fact]
    public void ReinitializingFirstService_AfterItsUninitialize_WhileSecondStaysAlive_ShouldDrawBoth()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);
        Build(game, false, "BothScreen");

        game.ServiceA.Uninitialize();

        XnaColor[] pixels;
        try
        {
            game.InitializeA(PathA);
            Build(game, true, "BothScreen");
            pixels = DrawBoth(game, makeServiceDefault: true);
        }
        catch (Exception e)
        {
            Assert.Fail($"Re-initializing A or drawing both throws {e.GetType().Name}: {e.Message}");
            return;
        }

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "texture", expectRed: true, "re-initialized A's sprite");
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "re-initialized A's text");
        CollectOwnAssetFailures(failures, pixels, OriginB, "texture", expectRed: false, "B's sprite (built before A's Uninitialize)");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "B's text (built before A's Uninitialize)");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // Order B, A: B is uninitialized and initialized again while A stays alive, then both draw.
    [Fact]
    public void ReinitializingSecondService_AfterItsUninitialize_WhileFirstStaysAlive_ShouldDrawBoth()
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);
        Build(game, true, "BothScreen");

        game.ServiceB.Uninitialize();

        XnaColor[] pixels;
        try
        {
            game.InitializeB(PathB);
            Build(game, false, "BothScreen");
            pixels = DrawBoth(game, makeServiceDefault: true);
        }
        catch (Exception e)
        {
            Assert.Fail($"Re-initializing B or drawing both throws {e.GetType().Name}: {e.Message}");
            return;
        }

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "texture", expectRed: true, "A's sprite (built before B's Uninitialize)");
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "A's text (built before B's Uninitialize)");
        CollectOwnAssetFailures(failures, pixels, OriginB, "texture", expectRed: false, "re-initialized B's sprite");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "re-initialized B's text");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // The recovery path after a cycle: the other service's tree is gone with its disposed textures, so
    // both trees are rebuilt and drawn. Tests that Uninitialize + Initialize again leaves a usable pair.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UninitializeAndReinitializeOneService_AfterRebuildingBothTrees_ShouldDrawBoth(bool cycleA)
    {
        using CollisionGame game = CreateGame();
        WriteAssetProjects(game);
        game.InitializeA(PathA);
        game.InitializeB(PathB);
        GraphicalUiElement oldA = Build(game, true, "BothScreen");
        GraphicalUiElement oldB = Build(game, false, "BothScreen");

        XnaColor[] pixels;
        try
        {
            if (cycleA)
            {
                game.ServiceA.Uninitialize();
                game.InitializeA(PathA);
            }
            else
            {
                game.ServiceB.Uninitialize();
                game.InitializeB(PathB);
            }

            // Drop the trees built before the cycle (their textures were disposed), then build fresh ones.
            IGumService.Default = game.ServiceA;
            oldA.RemoveFromManagers();
            IGumService.Default = game.ServiceB;
            oldB.RemoveFromManagers();
            Build(game, true, "BothScreen");
            Build(game, false, "BothScreen");
            pixels = DrawBoth(game, makeServiceDefault: true);
        }
        catch (Exception e)
        {
            Assert.Fail($"Cycling a service and rebuilding throws {e.GetType().Name}: {e.Message}");
            return;
        }

        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, OriginA, "texture", expectRed: true, "A's sprite");
        CollectOwnAssetFailures(failures, pixels, OriginA, "font", expectRed: true, "A's text");
        CollectOwnAssetFailures(failures, pixels, OriginB, "texture", expectRed: false, "B's sprite");
        CollectOwnAssetFailures(failures, pixels, OriginB, "font", expectRed: false, "B's text");
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    // Uninitializing the first service must not empty the element-type table the second registered into.
    [Fact]
    public void BuildingFormsControlInSecondService_AfterFirstServiceUninitialized_ShouldStillCreateTheControl()
    {
        using CollisionGame game = CreateGame();
        string pathA = WriteWidgetProject("A", OriginA, behavior: "ButtonBehavior");
        string pathB = WriteWidgetProject("B", OriginB, behavior: "CheckBoxBehavior");
        game.InitializeA(pathA);
        game.InitializeB(pathB);

        game.ServiceA.Uninitialize();

        GraphicalUiElement screenB;
        try
        {
            screenB = Build(game, false, "MainScreen");
        }
        catch (Exception e)
        {
            Assert.Fail($"Building B's screen after A.Uninitialize throws {e.GetType().Name}: {e.Message}");
            return;
        }

        object? control = FormsControlOf(screenB, "W");
        (control is CheckBox).ShouldBeTrue(
            $"B's Widget became {control?.GetType().Name ?? "no Forms control"}: A.Uninitialize called ElementSaveExtensions.ClearRegistrations, which emptied the table B registered into");
    }

    #endregion

    #region Helpers

    private string PathA => Path.Combine(_root, "A", "Project.gumx");
    private string PathB => Path.Combine(_root, "B", "Project.gumx");

    private static void RewriteGumx(string gumxPath, string oldText, string newText)
    {
        string text = File.ReadAllText(gumxPath);
        text.ShouldContain(oldText);
        File.WriteAllText(gumxPath, text.Replace(oldText, newText));
    }

    private static CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        return game;
    }

    private static void InitializeBoth(CollisionGame game, string pathA, string pathB, bool aFirst)
    {
        if (aFirst) { game.InitializeA(pathA); game.InitializeB(pathB); }
        else { game.InitializeB(pathB); game.InitializeA(pathA); }
    }

    private static GraphicalUiElement Build(CollisionGame game, bool useA, string screenName)
    {
        Gum.GumService service = useA ? game.ServiceA : game.ServiceB;
        GumProjectSave project = useA ? game.ProjectA! : game.ProjectB!;
        ScreenSave screenSave = project.Screens.First(item => item.Name == screenName);
        // Building for a service means making it the default first; nothing switches implicitly.
        global::RenderingLibrary.IGumService.Default = service;
        GraphicalUiElement screen = screenSave.ToGraphicalUiElement(service.SystemManagers, addToManagers: true);
        screen.UpdateLayout();
        return screen;
    }

    private static XnaColor[] DrawBoth(CollisionGame game, bool makeServiceDefault)
    {
        using CollisionCapture capture = new CollisionCapture(game.GraphicsDevice);
        for (int pass = 0; pass < 2; pass++)
        {
            capture.Draw(game.ServiceA, makeServiceDefault);
            capture.Draw(game.ServiceB, makeServiceDefault);
        }
        return capture.Read();
    }

    private static object? FormsControlOf(GraphicalUiElement screen, string instanceName) =>
        (screen.GetGraphicalUiElementByName(instanceName) as InteractiveGue)?.FormsControlAsObject;

    private static int CountNodes(GraphicalUiElement element) =>
        1 + (element.Children?.OfType<GraphicalUiElement>().Sum(CountNodes) ?? 0);

    private static XnaColor FirstPixel(Texture2D texture)
    {
        XnaColor[] data = new XnaColor[texture.Width * texture.Height];
        texture.GetData(data);
        return data[0];
    }

    private readonly struct BitmapFontHandle
    {
        private readonly RenderingLibrary.Graphics.BitmapFont? _font;
        public BitmapFontHandle(RenderingLibrary.Graphics.BitmapFont? font) { _font = font; }
        public bool IsDisposed => _font?.Texture?.IsDisposed == true;
    }

    private static void AssertColors(XnaColor[] pixels, (string Message, int X, int Y, Func<XnaColor, bool> Check)[] expectations)
    {
        List<string> failures = new();
        foreach ((string message, int x, int y, Func<XnaColor, bool> check) in expectations)
        {
            XnaColor actual = CollisionCapture.At(pixels, x, y);
            if (!check(actual)) { failures.Add($"{message} (pixel {x},{y} is {CollisionCapture.Describe(actual)})"); }
        }
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    private static void AssertOwnAssetPixels(XnaColor[] pixels, Point origin, string kind, bool expectRed, string label)
    {
        List<string> failures = new();
        CollectOwnAssetFailures(failures, pixels, origin, kind, expectRed, label);
        failures.ShouldBeEmpty(string.Join("; ", failures));
    }

    /// <summary>
    /// Checks that the sprite or text at <paramref name="origin"/> shows the expected project's color:
    /// red for project A, blue for project B (the asset files are solid colors).
    /// </summary>
    private static void CollectOwnAssetFailures(List<string> failures, XnaColor[] pixels, Point origin, string kind, bool expectRed, string label)
    {
        Func<XnaColor, bool> expected = expectRed ? CollisionCapture.IsRed : CollisionCapture.IsBlue;
        Func<XnaColor, bool> other = expectRed ? CollisionCapture.IsBlue : CollisionCapture.IsRed;
        // The sprite is 40x40 at the origin; the text sits at the origin in a 120x30 box. A sprite and a
        // text from the same project share origin when both are in one screen, so look at distinct areas.
        Rectangle area = kind == "texture"
            ? new Rectangle(origin.X, origin.Y, 40, 40)
            : new Rectangle(origin.X, origin.Y + 50, 120, 30);

        int good = CollisionCapture.Count(pixels, area, expected);
        int bad = CollisionCapture.Count(pixels, area, other);
        int minimum = kind == "texture" ? 1000 : 100;
        if (good < minimum || bad > 0)
        {
            failures.Add($"{label}: expected {(expectRed ? "red" : "blue")} but found {good} red-or-blue-as-expected and {bad} of the other project's color pixels in {area}");
        }
    }

    #endregion

    #region Project writers

    private void WriteAssetProjects(CollisionGame game, bool writeB = true)
    {
        WriteAssetProject(game, "A", OriginA, XnaColor.Red, Yellow, xAdvance: 10);
        if (writeB)
        {
            WriteAssetProject(game, "B", OriginB, XnaColor.Blue, Green, xAdvance: 20);
        }
    }

    /// <summary>
    /// Writes a project whose files have the same relative names in every project but different
    /// content: Tex.png and Tex2.png, FontCache/Font31Bogus.fnt and Font32Bogus.fnt, Fonts/TestFont.fnt.
    /// The first asset set is <paramref name="color"/>, the second <paramref name="color2"/>.
    /// </summary>
    private void WriteAssetProject(CollisionGame game, string name, Point origin, XnaColor color, XnaColor color2, int xAdvance)
    {
        string dir = Path.Combine(_root, name);
        GumProjectFolder folder = new GumProjectFolder(dir);
        folder.WriteStandards();

        folder.Add(new GumElement("SpriteScreen", isScreen: true).Child("Pic", "Sprite").PicVars(origin));
        folder.Add(new GumElement("CacheFontScreen", isScreen: true).Child("Label", "Text")
            .LabelVars(origin, useCustomFont: false, yOffset: 50));
        folder.Add(new GumElement("CustomFontScreen", isScreen: true).Child("Label", "Text")
            .LabelVars(origin, useCustomFont: true, yOffset: 50));
        // One screen with both, text below the sprite, for lifecycle tests.
        folder.Add(new GumElement("BothScreen", isScreen: true).Child("Pic", "Sprite").Child("Label", "Text")
            .PicVars(origin).LabelVars(origin, useCustomFont: false, yOffset: 50));
        folder.Write();

        GraphicsDevice device = game.GraphicsDevice;
        CollisionAssets.WritePng(device, dir, "Tex.png", color);
        CollisionAssets.WritePng(device, dir, "Tex2.png", color2);
        CollisionAssets.WriteBitmapFont(device, dir, "FontCache/Font31Bogus.fnt", color, xAdvance);
        CollisionAssets.WriteBitmapFont(device, dir, "FontCache/Font32Bogus.fnt", color2, xAdvance);
        CollisionAssets.WriteBitmapFont(device, dir, "Fonts/TestFont.fnt", color, xAdvance);
    }

    /// <summary>
    /// Card (a Container with a Fill ColoredRectangle that sets no color of its own) and Derived (BaseType
    /// Card, plus a Dot); the project's own ColoredRectangle Standard sets the default color.
    /// </summary>
    private string WriteNamesProject(string name, Point origin, (int R, int G, int B) standardDefault, (int R, int G, int B) dot)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.WriteStandards(
            ("ColoredRectangle", "Red", standardDefault.R.ToString()),
            ("ColoredRectangle", "Green", standardDefault.G.ToString()),
            ("ColoredRectangle", "Blue", standardDefault.B.ToString()));

        folder.Add(new GumElement("Card")
            .Var(GumVar.Float("Width", 100)).Var(GumVar.Float("Height", 50))
            .Var(GumVar.Units("WidthUnits", 0)).Var(GumVar.Units("HeightUnits", 0))
            .Child("Fill", "ColoredRectangle")
            .Var(GumVar.Units("Fill.WidthUnits", 1)).Var(GumVar.Units("Fill.HeightUnits", 1))
            .Var(GumVar.Float("Fill.Width", 100)).Var(GumVar.Float("Fill.Height", 100)));
        folder.Add(new GumElement("Derived", baseType: "Card")
            .Child("Fill", "ColoredRectangle", definedByBase: true)
            .Child("Dot", "ColoredRectangle")
            .Var(GumVar.Float("Dot.X", 5)).Var(GumVar.Float("Dot.Y", 5))
            .Var(GumVar.Float("Dot.Width", 10)).Var(GumVar.Float("Dot.Height", 10))
            .Var(GumVar.Int("Dot.Red", dot.R)).Var(GumVar.Int("Dot.Green", dot.G)).Var(GumVar.Int("Dot.Blue", dot.B)));
        folder.Add(new GumElement("MainScreen", isScreen: true).Child("Item", "Derived")
            .Var(GumVar.Float("Item.X", origin.X)).Var(GumVar.Float("Item.Y", origin.Y)));
        return folder.Write();
    }

    /// <summary>A component 'Btn' with ButtonBehavior, whose behavior declares an IsEnabled default.</summary>
    private string WriteBehaviorProject(string name, Point origin, bool isEnabledDefault)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.WriteStandards();
        folder.AddBehavior("ButtonBehavior", "IsEnabled", isEnabledDefault, "ButtonCategory",
            "Enabled", "Disabled", "Highlighted", "Pushed", "HighlightedFocused", "Focused", "DisabledFocused");
        folder.Add(new GumElement("Btn").Behavior("ButtonBehavior")
            .Var(GumVar.Float("Width", 100)).Var(GumVar.Float("Height", 50))
            .Var(GumVar.Units("WidthUnits", 0)).Var(GumVar.Units("HeightUnits", 0))
            .Category("ButtonCategory", "Enabled", "Disabled", "Highlighted", "Pushed", "HighlightedFocused", "Focused", "DisabledFocused"));
        folder.Add(new GumElement("MainScreen", isScreen: true).Child("W", "Btn")
            .Var(GumVar.Float("W.X", origin.X)).Var(GumVar.Float("W.Y", origin.Y)));
        return folder.Write();
    }

    /// <summary>A component named 'Widget' (ButtonBehavior, CheckBoxBehavior or none) with an instance 'W' in MainScreen.</summary>
    private string WriteWidgetProject(string name, Point origin, string? behavior)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.WriteStandards();
        GumElement widget = new GumElement("Widget")
            .Var(GumVar.Float("Width", 100)).Var(GumVar.Float("Height", 50))
            .Var(GumVar.Units("WidthUnits", 0)).Var(GumVar.Units("HeightUnits", 0));
        if (behavior == "ButtonBehavior")
        {
            string[] states = { "Enabled", "Disabled", "Highlighted", "Pushed", "HighlightedFocused", "Focused", "DisabledFocused" };
            folder.AddBehavior("ButtonBehavior", "IsEnabled", true, "ButtonCategory", states);
            widget.Behavior("ButtonBehavior").Category("ButtonCategory", states);
        }
        else if (behavior == "CheckBoxBehavior")
        {
            string[] states = { "EnabledOn", "EnabledOff", "EnabledIndeterminate", "DisabledOn", "DisabledOff", "DisabledIndeterminate",
                "HighlightedOn", "HighlightedOff", "HighlightedIndeterminate", "PushedOn", "PushedOff", "PushedIndeterminate" };
            folder.AddBehavior("CheckBoxBehavior", "IsEnabled", true, "CheckBoxCategory", states);
            widget.Behavior("CheckBoxBehavior").Category("CheckBoxCategory", states);
        }
        folder.Add(widget);
        folder.Add(new GumElement("MainScreen", isScreen: true).Child("W", "Widget")
            .Var(GumVar.Float("W.X", origin.X)).Var(GumVar.Float("W.Y", origin.Y)));
        return folder.Write();
    }

    #endregion
}

internal static class CollisionElementExtensions
{
    /// <summary>A 40x40 Sprite named Pic showing Tex.png at the origin.</summary>
    public static GumElement PicVars(this GumElement element, Point origin) => element
        .Var(GumVar.String("Pic.SourceFile", "Tex.png"))
        .Var(GumVar.Float("Pic.X", origin.X)).Var(GumVar.Float("Pic.Y", origin.Y))
        .Var(GumVar.Float("Pic.Width", 40)).Var(GumVar.Float("Pic.Height", 40))
        .Var(GumVar.Units("Pic.WidthUnits", 0)).Var(GumVar.Units("Pic.HeightUnits", 0));

    /// <summary>A white "AAAA" Text named Label, from FontCache/Font31Bogus.fnt or from Fonts/TestFont.fnt.</summary>
    public static GumElement LabelVars(this GumElement element, Point origin, bool useCustomFont, int yOffset)
    {
        element
            .Var(GumVar.String("Label.Text", "AAAA"))
            .Var(GumVar.Float("Label.X", origin.X)).Var(GumVar.Float("Label.Y", origin.Y + yOffset))
            .Var(GumVar.Float("Label.Width", 120)).Var(GumVar.Float("Label.Height", 30))
            .Var(GumVar.Units("Label.WidthUnits", 0)).Var(GumVar.Units("Label.HeightUnits", 0))
            .Var(GumVar.Int("Label.Red", 255)).Var(GumVar.Int("Label.Green", 255)).Var(GumVar.Int("Label.Blue", 255));
        if (useCustomFont)
        {
            element.Var(GumVar.Bool("Label.UseCustomFont", true)).Var(GumVar.String("Label.CustomFontFile", "Fonts/TestFont.fnt"));
        }
        else
        {
            element.Var(GumVar.String("Label.Font", "Bogus")).Var(GumVar.Int("Label.FontSize", 31));
        }
        return element;
    }
}
