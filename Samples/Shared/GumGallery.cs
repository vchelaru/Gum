#nullable enable
using Gum;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using GumSamples.Screens;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
#if RAYLIB
using Examples.Shapes;
#elif SKIA
using SilkNetGum.Screens;
#else
using MonoGameGumInCode.Screens;
#endif

namespace GumSamples;

/// <summary>
/// The tab bar and screen switching shared by every gallery sample (MonoGameGumInCode, KniSkiaGumInCode, the
/// raylib gallery, SilkNetGumSample, BrutalGumSample). It has no windowing or rendering code, so each host
/// links this file and only adds its own window, input and presentation. The screens are the files the hosts
/// link from MonoGameGumInCode, compiled with their own backend symbol (RAYLIB, SKIA or neither), so a
/// screen that is missing on a backend is missing from that backend's list only.
/// </summary>
internal sealed class GumGallery
{
    // Code-only screens appended after every screen from the gumx (hosts with no gumx have none), allowing
    // the samples to exercise runtime features that are not yet authored as Gum screens. The names are the
    // nav-strip button labels.
    private static readonly (string Name, Func<FrameworkElement> Create)[] codeScreens =
    {
        ("Forms", () => new FormsScreen()),
        ("Standards", () => new StandardsScreen()),
        ("Circles", () => new CirclesScreen()),
        ("Rectangles", () => new RectanglesScreen()),
        ("Arcs", () => new ArcsScreen()),
        ("Polygons", () => new PolygonsScreen()),
        ("Gradients", () => new GradientScreen()),
        ("Text", () => new TextScreen()),
        ("Mixed", () => new MixedScreen()),
        ("Invisible", () => new InvisibleScreen()),
        ("NineSlice", () => new NineSliceScreen()),
        ("Sprite", () => new SpriteScreen()),
#if !RAYLIB
        ("Svg", () => new SvgScreen()),
#endif
        ("Clip", () => new ClippingScreen()),
        ("Render Target", () => new RenderTargetScreen()),
        ("RT Shader", () => new RenderTargetShaderScreen()),
        ("Zoom", () => new ZoomScreen()),
        ("Layer Camera", () => new LayerCameraSettingsScreen()),
#if !SKIA && !RAYLIB
        ("Draw Call Stress", () => new DrawCallStressScreen()),
#endif
    };

    private GraphicalUiElement? _currentGumxScreen;
    private FrameworkElement? _currentCodeScreen;
    private int _currentScreenIndex;
    private StackPanel _navStrip = null!;

    /// <summary>The screen shown by <see cref="LoadScreen"/> if it is a code screen, otherwise null.</summary>
    public FrameworkElement? CurrentCodeScreen => _currentCodeScreen;

    /// <summary>
    /// Raised before the Zoom or Expand radio applies its fit mode, so a host that offers its own fit mode
    /// (like raylib's manual camera) can switch it off.
    /// </summary>
    public event Action? FitModeChanging;

    private static int GumxScreenCount => ObjectFinder.Self.GumProjectSave?.Screens.Count ?? 0;

    /// <summary>The number of screens in the gallery: the gumx project's screens, then the code screens.</summary>
    public int ScreenCount => GumxScreenCount + codeScreens.Length;

    /// <summary>Builds the tab bar, then shows the code screen named <paramref name="initialCodeScreenName"/>, or the first screen.</summary>
    public void Build(string? initialCodeScreenName = null)
    {
        BuildNavStrip();

        int codeIndex = IndexOfCodeScreen(initialCodeScreenName);
        LoadScreen(codeIndex >= 0 ? GumxScreenCount + codeIndex : 0);
    }

    /// <summary>Shows the code screen named <paramref name="name"/>, returning false if the gallery has none by that name.</summary>
    public bool TryShowCodeScreen(string name)
    {
        int codeIndex = IndexOfCodeScreen(name);
        if (codeIndex < 0)
        {
            return false;
        }

        LoadScreen(GumxScreenCount + codeIndex);
        return true;
    }

    /// <summary>
    /// Adds a radio to the fit-mode group at the end of the tab bar. <see cref="Build"/> already adds Zoom and
    /// Expand; a host calls this after it for any mode of its own.
    /// </summary>
    public void AddFitModeRadio(string text, bool isChecked, Action onChecked)
    {
        RadioButton radio = new RadioButton();
        radio.Name = "FitMode" + text.Replace(" ", "");
        radio.GroupName = "FitMode";
        radio.Text = text;
        radio.IsChecked = isChecked;
        radio.Checked += (_, _) => onChecked();
        _navStrip.AddChild(radio);
    }

    /// <summary>
    /// Ticks whatever the current screen animates by hand, with the seconds since the last tick, and keeps the
    /// screen below the tab bar, whose height changes when a resize makes its buttons wrap.
    /// </summary>
    public void Tick(double elapsedSeconds)
    {
        // FrameworkElement.Activity() is FRB-only, so screens that animate get a host-driven per-frame Tick.
        (_currentCodeScreen as TextScreen)?.Tick(elapsedSeconds);
#if !SKIA && !RAYLIB
        (_currentCodeScreen as DrawCallStressScreen)?.Tick(elapsedSeconds);
#endif
        FitBelowNavStrip(_currentGumxScreen ?? _currentCodeScreen?.Visual);
    }

    private static int IndexOfCodeScreen(string? name)
    {
        if (name == null)
        {
            return -1;
        }

        for (int i = 0; i < codeScreens.Length; i++)
        {
            if (codeScreens[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    private void FitBelowNavStrip(GraphicalUiElement? screen)
    {
        if (screen == null) return;

        float navStripHeight = _navStrip.Visual.AbsoluteHeight;
        if (screen.Y != navStripHeight)
        {
            screen.Y = navStripHeight;
            screen.Height = -navStripHeight;
        }
    }

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
        _navStrip.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _navStrip.Height = 0;
        _navStrip.Visual.WrapsChildren = true;
        _navStrip.AddToRoot();

        int gumxCount = GumxScreenCount;

        for (int i = 0; i < gumxCount; i++)
        {
            int capturedIndex = i;
            AddNavButton(ObjectFinder.Self.GumProjectSave!.Screens[i].Name, () => LoadScreen(capturedIndex));
        }

        for (int i = 0; i < codeScreens.Length; i++)
        {
            int capturedIndex = gumxCount + i;
            AddNavButton(codeScreens[i].Name, () => LoadScreen(capturedIndex));
        }

        AddFitModeRadio("Zoom", isChecked: true, () =>
        {
            FitModeChanging?.Invoke();
            GumService.Default.EnableZoomToWindow();
        });
        AddFitModeRadio("Expand", isChecked: false, () =>
        {
            FitModeChanging?.Invoke();
            GumService.Default.EnableExpandToWindow();
        });
    }

    private void AddNavButton(string text, Action onClick)
    {
        Button button = new Button();
        // Named so automation scripts can target it, e.g. NavForms or NavRenderTarget.
        button.Name = "Nav" + text.Replace(" ", "");
        button.Text = text;
        button.Click += (_, _) => onClick();
        _navStrip.AddChild(button);
    }

    /// <summary>Shows the screen at <paramref name="index"/>, wrapping around at either end.</summary>
    public void LoadScreen(int index)
    {
        int gumxCount = GumxScreenCount;
        int total = gumxCount + codeScreens.Length;
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

        if (_currentScreenIndex < gumxCount)
        {
            _currentGumxScreen = ObjectFinder.Self.GumProjectSave!.Screens[_currentScreenIndex]
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
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
            _currentCodeScreen = codeScreens[_currentScreenIndex - gumxCount].Create();
            _currentCodeScreen.Visual.YOrigin = VerticalAlignment.Top;
            _currentCodeScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            _currentCodeScreen.Visual.Y = _navStrip.Visual.AbsoluteHeight;
            _currentCodeScreen.Visual.Height = -_navStrip.Visual.AbsoluteHeight;
            _currentCodeScreen.AddToRoot();
        }
    }
}
