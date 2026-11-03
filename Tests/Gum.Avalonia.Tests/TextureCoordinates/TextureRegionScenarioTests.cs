using Avalonia;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Shouldly;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// The double-click on the Texture Coordinates canvas (#7354): what it did before plugins could
/// report regions, which must not change, and what it does inside a region a plugin reported.
/// </summary>
[Trait("Category", "EndToEnd")]
public class TextureRegionScenarioTests
{
    #region Today's double-click (characterization)

    [SkippableFact]
    public void DoubleClick_WithoutSnap_SetsA64x64RectangleCenteredOnTheCursor_AsOneUndoStep()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 8, top: 8, width: 16, height: 16);
            tab.Select(icon);

            DoubleClick(tab, 200f, 150f);

            Value(button, "Icon.TextureLeft").ShouldBe(168, tab.Describe());
            Value(button, "Icon.TextureTop").ShouldBe(118);
            Value(button, "Icon.TextureWidth").ShouldBe(64);
            Value(button, "Icon.TextureHeight").ShouldBe(64);

            tab.Undo();

            Value(button, "Icon.TextureLeft").ShouldBe(8, "one undo restores the rectangle");
            Value(button, "Icon.TextureTop").ShouldBe(8);
            Value(button, "Icon.TextureWidth").ShouldBe(16);
            Value(button, "Icon.TextureHeight").ShouldBe(16);
        });
    }

    [SkippableFact]
    public void DoubleClick_WithSnapToGrid_SetsTheGridCellUnderTheCursor()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 8, top: 8, width: 16, height: 16);
            tab.Select(icon);
            tab.Input.Click(tab.SnapToGridCheckBox);
            tab.ViewModel.SelectedSnapToGridValue.ShouldBe(16);

            DoubleClick(tab, 200f, 150f);

            Value(button, "Icon.TextureLeft").ShouldBe(192, tab.Describe());
            Value(button, "Icon.TextureTop").ShouldBe(144);
            Value(button, "Icon.TextureWidth").ShouldBe(16);
            Value(button, "Icon.TextureHeight").ShouldBe(16);
        });
    }

    #endregion

    #region Plugin regions

    [SkippableFact]
    public void ARegionPlugin_GivesTheOverlayEveryRegion_AndNamesOnlyTheHoveredOne()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);

            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(SampleSpriteSheet.Few.Count, tab.Describe());
            tab.Plugin.DisplayController.RegionOverlay.VisibleLabel.ShouldBeNull("no name until the pointer is over a region");

            tab.MoveTo(tab.WindowPointOf(140, 24));
            tab.Plugin.DisplayController.RegionOverlay.VisibleLabel.ShouldBe("Coin");

            tab.MoveTo(tab.WindowPointOf(300, 170));
            tab.Plugin.DisplayController.RegionOverlay.VisibleLabel.ShouldBeNull("outside every region");
            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(SampleSpriteSheet.Few.Count);
        });
    }

    [SkippableFact]
    public void WithNoRegionPlugin_ThereIsNoOverlay()
    {
        OnTab(tab =>
        {
            SelectSheetSprite(tab);

            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(0);
            tab.Plugin.DisplayController.RegionOverlay.VisibleLabel.ShouldBeNull();
        });
    }

    [SkippableFact]
    public void ADoubleClickInsideARegion_SetsThatRegionAsOneUndoStep_AndOneUndoRestoresIt()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab, TextureAddress.EntireTexture);

            DoubleClick(tab, 140, 24); // Coin: 128, 12, 24x24

            Value(button, "Icon.TextureAddress").ShouldBe(TextureAddress.Custom);
            Value(button, "Icon.TextureLeft").ShouldBe(128);
            Value(button, "Icon.TextureTop").ShouldBe(12);
            Value(button, "Icon.TextureWidth").ShouldBe(24);
            Value(button, "Icon.TextureHeight").ShouldBe(24);
            tab.Canvas.RectangleSelector!.Left.ShouldBe(128, "the selection follows the region");

            tab.Undo();

            Value(button, "Icon.TextureAddress").ShouldBe(TextureAddress.EntireTexture, "one undo restores the address");
            Value(button, "Icon.TextureLeft").ShouldBe(8);
            Value(button, "Icon.TextureTop").ShouldBe(8);
            Value(button, "Icon.TextureWidth").ShouldBe(16);
            Value(button, "Icon.TextureHeight").ShouldBe(16);
        });
    }

    [SkippableFact]
    public void ADoubleClickOutsideEveryRegion_StillMakesTheDefaultRectangle()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);

            DoubleClick(tab, 300, 170); // nothing there

            Value(button, "Icon.TextureLeft").ShouldBe(268);
            Value(button, "Icon.TextureTop").ShouldBe(138);
            Value(button, "Icon.TextureWidth").ShouldBe(64);
            Value(button, "Icon.TextureHeight").ShouldBe(64);
        });
    }

    [SkippableFact]
    public void WhereRegionsOverlap_TheSmallestOneWins()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", new[]
            {
                new TextureRegion("Panel", 0, 0, 200, 150),
                new TextureRegion("Icon", 60, 60, 20, 20),
            });
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);

            DoubleClick(tab, 70, 70);
            Value(button, "Icon.TextureWidth").ShouldBe(20, "the small region on top of the panel");

            DoubleClick(tab, 150, 100);
            Value(button, "Icon.TextureWidth").ShouldBe(200, $"the panel where the small region is not; outlines {tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount} {tab.Describe()}");
        });
    }

    [SkippableFact]
    public void ARegionPluginThatThrows_DoesNotBreakTheTab_AndOtherPluginsRegionsStillShow()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin buggy = new SampleRegionPlugin("Sheet.png", Array.Empty<TextureRegion>(), throws: true);
            SampleRegionPlugin good = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable first = Register(tab, buggy);
            using IDisposable second = Register(tab, good);
            ComponentSave button = SelectSheetSprite(tab);

            tab.Tab.IsVisible.ShouldBeTrue(tab.Describe());
            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(SampleSpriteSheet.Few.Count);
            DoubleClick(tab, 140, 24);
            Value(button, "Icon.TextureLeft").ShouldBe(128);
        });
    }

    [SkippableFact]
    public void ThreeHundredThirtyNineRegions_AreOneQueryAndOneOutlineEach_NoMatterHowOftenTheTabRefreshes()
    {
        OnTab(tab =>
        {
            List<TextureRegion> many = new List<TextureRegion>();
            for (int i = 0; i < 339; i++)
            {
                many.Add(new TextureRegion($"Slice_{i}", i % 20 * 16, i / 20 * 10, 15, 9));
            }
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", many);
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);

            for (int i = 0; i < 5; i++)
            {
                tab.Plugin.DisplayController.Refresh();
            }

            plugin.QueryCount.ShouldBe(1, "regions are cached per file");
            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(339);
        });
    }

    #endregion

    private static ComponentSave SelectSheetSprite(TextureCoordinateTabHarness tab, TextureAddress address = TextureAddress.Custom)
    {
        ComponentSave button = tab.Project.AddComponent("Button");
        SampleSpriteSheet.Write(Path.Combine(tab.Project.ProjectFolder, "Sheet.png"));
        InstanceSave icon = tab.AddSprite(button, "Icon", "Sheet.png", left: 8, top: 8, width: 16, height: 16);
        if (address != TextureAddress.Custom)
        {
            using (tab.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Icon.TextureAddress", address, nameof(TextureAddress));
            }
        }
        tab.Select(icon);
        return button;
    }

    // Adds the plugin to the tool's real PluginManager, as an installed plugin would be.
    internal static IDisposable Register(TextureCoordinateTabHarness tab, PluginBase plugin)
    {
        PluginManager manager = tab.PluginManager;
        manager.Plugins = manager.InitializedPlugins.Append(plugin).ToList();
        PluginManager.StartupPlugin(plugin, manager);
        return new Unregistration(manager, plugin);
    }

    private sealed class Unregistration : IDisposable
    {
        private readonly PluginManager _manager;
        private readonly PluginBase _plugin;

        public Unregistration(PluginManager manager, PluginBase plugin)
        {
            _manager = manager;
            _plugin = plugin;
        }

        public void Dispose()
        {
            _manager.Plugins = _manager.InitializedPlugins.Where(item => item != _plugin).ToList();
            _manager.PluginContainers.Remove(_plugin);
        }
    }

    // A double-click does not auto-save, so read the edited state itself.
    private static object? Value(ComponentSave owner, string name) => owner.DefaultState!.GetValue(name);

    internal static void DoubleClick(TextureCoordinateTabHarness tab, float textureX, float textureY)
    {
        // The camera re-centers on a new selection a frame later; settle it before aiming.
        tab.Frame();
        tab.Frame();
        Point point = tab.WindowPointOf(textureX, textureY);
        tab.Drag(point, point);
        tab.Drag(point, point);
    }

    private static void OnTab(Action<TextureCoordinateTabHarness> scenario)
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        TextureCoordinateTabHarness.OnUiThread(() =>
        {
            using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
            scenario(tab);
        });
    }
}
