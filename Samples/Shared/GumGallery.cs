using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using GumSamples.Screens;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SilkNetGum.Screens;
using System;

namespace GumSamples;

/// <summary>
/// The tab bar and screen switching of the Skia gallery samples (SilkNetGumSample, BrutalGumSample). It has
/// no windowing or rendering code, so each host links this file and only adds its own window, input and
/// presentation. The screens are the files both samples link from MonoGameGumInCode and SilkNetGumSample,
/// compiled with the SKIA symbol.
/// </summary>
internal sealed class GumGallery
{
    // Code-only screens appended after every screen from the gumx, allowing the sample to exercise runtime
    // features (like the unified NineSliceRuntime) that are not yet authored as Gum screens.
    private static readonly Func<FrameworkElement>[] codeScreenFactories =
    {
        () => new SilkNetGum.Screens.NineSliceScreen(),
        () => new SilkNetGum.Screens.SpriteScreen(),
        () => new SilkNetGum.Screens.SvgScreen(),
        () => new SilkNetGum.Screens.TextScreen(),
        () => new SilkNetGum.Screens.CirclesScreen(),
        () => new SilkNetGum.Screens.RectanglesScreen(),
        () => new SilkNetGum.Screens.ArcsScreen(),
        () => new SilkNetGum.Screens.PolygonsScreen(),
        () => new SilkNetGum.Screens.RenderTargetScreen(),
        () => new SilkNetGum.Screens.RenderTargetShaderScreen(),
        () => new GumSamples.Screens.FormsScreen(),
        () => new SilkNetGum.Screens.ZoomScreen(),
        () => new SilkNetGum.Screens.LayerCameraSettingsScreen(),
    };

    // Parallel to codeScreenFactories, used as nav-strip button labels.
    private static readonly string[] codeScreenNames =
    {
        "NineSlice",
        "Sprite",
        "Svg",
        "Text",
        "Circles",
        "Rectangles",
        "Arcs",
        "Polygons",
        "Render Target",
        "RT Shader",
        "Forms",
        "Zoom",
        "Layer Camera",
    };

    private GraphicalUiElement? _currentGumxScreen;
    private FrameworkElement? _currentCodeScreen;
    private int _currentScreenIndex;
    private StackPanel _navStrip = null!;

    /// <summary>The number of screens in the gallery: the gumx project's screens, then the code screens.</summary>
    public int ScreenCount => ObjectFinder.Self.GumProjectSave!.Screens.Count + codeScreenFactories.Length;

    /// <summary>Builds the tab bar, then shows the code screen named <paramref name="initialCodeScreenName"/>, or the first screen.</summary>
    public void Build(string? initialCodeScreenName = null)
    {
        BuildNavStrip();

        int codeIndex = initialCodeScreenName == null ? -1 : Array.IndexOf(codeScreenNames, initialCodeScreenName);
        LoadScreen(codeIndex >= 0 ? ObjectFinder.Self.GumProjectSave!.Screens.Count + codeIndex : 0);
    }

    /// <summary>Ticks whatever the current screen animates by hand, with the seconds since the last tick.</summary>
    public void Tick(double elapsedSeconds) => (_currentCodeScreen as TextScreen)?.Tick(elapsedSeconds);

    // A horizontal strip of buttons, one per screen, pinned to the top-left. The gallery mixes two screen
    // kinds (.gumx-authored GraphicalUiElement screens and code-only FrameworkElement screens), so each
    // button just calls LoadScreen(index) with its own captured index.
    private void BuildNavStrip()
    {
        _navStrip = new StackPanel();
        _navStrip.Orientation = Orientation.Horizontal;
        _navStrip.Spacing = 4;
        _navStrip.Visual.X = 4;
        _navStrip.Visual.Y = 4;
        _navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        _navStrip.Width = 0;
        _navStrip.Visual.WrapsChildren = true;
        _navStrip.AddToRoot();

        var gumxScreens = ObjectFinder.Self.GumProjectSave!.Screens;

        for (int i = 0; i < gumxScreens.Count; i++)
        {
            int capturedIndex = i;
            AddNavButton(gumxScreens[i].Name, () => LoadScreen(capturedIndex));
        }

        for (int i = 0; i < codeScreenFactories.Length; i++)
        {
            int capturedIndex = gumxScreens.Count + i;
            AddNavButton(codeScreenNames[i], () => LoadScreen(capturedIndex));
        }
    }

    private void AddNavButton(string text, Action onClick)
    {
        var button = new Gum.Forms.Controls.Button();
        button.Text = text;
        button.Click += (_, _) => onClick();
        _navStrip.AddChild(button);
    }

    /// <summary>Shows the screen at <paramref name="index"/>, wrapping around at either end.</summary>
    public void LoadScreen(int index)
    {
        var gumxScreens = ObjectFinder.Self.GumProjectSave!.Screens;
        int total = gumxScreens.Count + codeScreenFactories.Length;
        if (total == 0) return;

        _currentScreenIndex = ((index % total) + total) % total;

        // Attach via AddToRoot (children of GumService.Default.Root) instead of AddToManagers, so
        // GumService.Default.Update walks the screen and its descendants, which is what advances
        // AnimationChain playback on the SpriteScreen's animated bear row.
        _currentGumxScreen?.RemoveFromRoot();
        _currentGumxScreen = null;
        _currentCodeScreen?.RemoveFromRoot();
        _currentCodeScreen = null;

        // ZoomScreen and LayerCameraSettingsScreen both drive the shared main Camera directly
        // (issues #4330, #4367) -- reset it here so leaving either screen zoomed/panned never leaks
        // into whichever screen is shown next.
        SystemManagers.Default.Renderer.Camera.Zoom = 1f;
        SystemManagers.Default.Renderer.Camera.X = 0f;
        SystemManagers.Default.Renderer.Camera.Y = 0f;

        if (_currentScreenIndex < gumxScreens.Count)
        {
            _currentGumxScreen = gumxScreens[_currentScreenIndex].ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            _currentGumxScreen.AddToRoot();
            // Fill the root below the tab bar. Relative sizes follow the canvas when the window resizes.
            _currentGumxScreen.YOrigin = VerticalAlignment.Top;
            _currentGumxScreen.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            _currentGumxScreen.Y = _navStrip.Visual.AbsoluteHeight;
            _currentGumxScreen.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            _currentGumxScreen.Width = 0;
            _currentGumxScreen.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            _currentGumxScreen.Height = -_navStrip.Visual.AbsoluteHeight;
        }
        else
        {
            // Code screens are FrameworkElement and Dock(Fill) themselves, so their visual fills the
            // canvas without an explicit size assignment here -- only the top-offset and height-shrink
            // need to be applied on top of that.
            _currentCodeScreen = codeScreenFactories[_currentScreenIndex - gumxScreens.Count]();
            _currentCodeScreen.Visual.YOrigin = VerticalAlignment.Top;
            _currentCodeScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            _currentCodeScreen.Visual.Y = _navStrip.Visual.AbsoluteHeight;
            _currentCodeScreen.Visual.Height = -_navStrip.Visual.AbsoluteHeight;
            _currentCodeScreen.AddToRoot();
        }
    }
}
