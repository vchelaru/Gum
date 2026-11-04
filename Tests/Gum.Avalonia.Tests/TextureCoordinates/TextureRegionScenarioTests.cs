using TextureCoordinateSelectionPlugin.Logic;
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
    public void TwoRegionsWithTheSameName_AreBothDrawn_AndEachClickAppliesItsOwnRectangle()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", new[]
            {
                new TextureRegion("hero-a", "Hero", 128, 12, 24, 24),
                new TextureRegion("hero-b", "Hero", 160, 40, 20, 30),
            });
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab, TextureAddress.EntireTexture);

            tab.Plugin.DisplayController.RegionOverlay.VisibleOutlineCount.ShouldBe(2, tab.Describe());

            Click(tab, 140, 24);
            Value(button, "Icon.TextureLeft").ShouldBe(128);
            Value(button, "Icon.TextureWidth").ShouldBe(24);

            Click(tab, 170, 50);
            Value(button, "Icon.TextureLeft").ShouldBe(160);
            Value(button, "Icon.TextureTop").ShouldBe(40);
            Value(button, "Icon.TextureWidth").ShouldBe(20);
            Value(button, "Icon.TextureHeight").ShouldBe(30);
        });
    }

    [SkippableFact]
    public void AClickInsideARegion_RaisesTextureRegionChosenOnce_AfterTheRegionIsApplied()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab, TextureAddress.EntireTexture);
            int leftWhenRaised = -1;
            plugin.TextureRegionChosen += (_, _) => leftWhenRaised = (int)Value(button, "Icon.TextureLeft")!;

            Click(tab, 140, 24); // Coin

            plugin.Chosen.Count.ShouldBe(1, tab.Describe());
            plugin.Chosen[0].Region.Key.ShouldBe("Coin");
            plugin.Chosen[0].Region.Name.ShouldBe("Coin");
            plugin.Chosen[0].File.FileNameNoPath.ShouldBe("Sheet.png");
            leftWhenRaised.ShouldBe(128, "raised after the rectangle was applied");
        });
    }

    [SkippableFact]
    public void TextureRegionChosen_CarriesTheKeyOfTheRegionClicked_WhenNamesRepeat()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", new[]
            {
                new TextureRegion("hero-a", "Hero", 128, 12, 24, 24),
                new TextureRegion("hero-b", "Hero", 160, 40, 20, 30),
            });
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab, TextureAddress.EntireTexture);

            Click(tab, 170, 50);

            plugin.Chosen.Select(item => item.Region.Key).ShouldBe(new[] { "hero-b" });
        });
    }

    [SkippableFact]
    public void AClickOutsideEveryRegion_DoesNotRaiseTextureRegionChosen()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab);

            Click(tab, 300, 170);

            plugin.Chosen.ShouldBeEmpty();
        });
    }

    [SkippableFact]
    public void ADragEndingInsideARegion_DoesNotRaiseTextureRegionChosen()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab);
            tab.Frame();
            tab.Frame();

            tab.Drag(tab.WindowPointOf(300, 170), tab.WindowPointOf(140, 24));

            plugin.Chosen.ShouldBeEmpty();
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
    public void AClickInsideARegion_SetsThatRegionAsOneUndoStep_AndOneUndoRestoresIt()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab, TextureAddress.EntireTexture);

            Click(tab, 140, 24); // Coin: 128, 12, 24x24

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
    public void AClickInsideARegion_DoesNotMoveTheCamera()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab, TextureAddress.EntireTexture);
            tab.Frame();
            tab.Frame();
            float cameraX = tab.Camera.X;
            float cameraY = tab.Camera.Y;

            Click(tab, 140, 24);
            tab.Frame();
            tab.Frame();

            tab.Camera.X.ShouldBe(cameraX);
            tab.Camera.Y.ShouldBe(cameraY);
        });
    }

    [SkippableFact]
    public void AClickOutsideEveryRegion_ChangesNothing_AndDoesNotMoveTheCamera()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);
            tab.Frame();
            tab.Frame();
            float cameraX = tab.Camera.X;
            float cameraY = tab.Camera.Y;

            Click(tab, 300, 170); // nothing there

            Value(button, "Icon.TextureLeft").ShouldBe(8, "the current rectangle is untouched");
            tab.Camera.X.ShouldBe(cameraX);
            tab.Camera.Y.ShouldBe(cameraY);
        });
    }

    [SkippableFact]
    public void ADoubleClickInsideARegion_KeepsTheRegion_AndDoesNotMoveTheCamera()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab, TextureAddress.EntireTexture);
            tab.Frame();
            tab.Frame();
            float cameraX = tab.Camera.X;
            float cameraY = tab.Camera.Y;

            DoubleClick(tab, 140, 24); // Coin: 128, 12, 24x24

            Value(button, "Icon.TextureLeft").ShouldBe(128);
            Value(button, "Icon.TextureWidth").ShouldBe(24, "the click applied the region and the double-click leaves it");
            tab.Camera.X.ShouldBe(cameraX);
            tab.Camera.Y.ShouldBe(cameraY);
        });
    }

    [SkippableFact]
    public void AClickThatBecomesADrag_DoesNotApplyTheRegionUnderTheRelease()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);
            tab.Frame();
            tab.Frame();

            // A drag across empty space that ends inside Coin: a drag is not a click.
            tab.Drag(tab.WindowPointOf(300, 170), tab.WindowPointOf(140, 24));

            Value(button, "Icon.TextureLeft").ShouldNotBe(128, "the rectangle was not replaced by Coin");
            Value(button, "Icon.TextureWidth").ShouldBe(16);
        });
    }

    [SkippableFact]
    public void TheHoverLabel_SitsAboveItsRegionWithAGap_AndTheSameSizeAtEveryZoom()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab);
            TextureRegionOverlayManager overlay = tab.Plugin.DisplayController.RegionOverlay;
            TextureRegion door = new TextureRegion("Door", "Door", 96, 80, 48, 96);
            float? firstScale = null;

            foreach (int zoom in new[] { 100, 200, 400 })
            {
                tab.ViewModel.SelectedZoomLevel = zoom;
                tab.Frame();
                tab.Frame();
                overlay.Hovered = null;
                overlay.Hovered = door;
                overlay.Refresh();

                var label = overlay.VisibleLabelPlacement!.Value;
                float regionTop = (door.Y - tab.Camera.Y) * tab.Camera.Zoom;
                label.Y.ShouldBeLessThan(regionTop, $"above the region at {zoom}%");
                (regionTop - (label.Y + label.Height)).ShouldBeGreaterThan(0, $"a gap between text and region at {zoom}%");
                firstScale ??= label.FontScale;
                label.FontScale.ShouldBe(firstScale.Value, $"the label does not scale with the zoom ({zoom}%)");
            }
        });
    }

    [SkippableFact]
    public void TheHoverLabel_FallsBelowItsRegion_WhenThereIsNoRoomAbove()
    {
        OnTab(tab =>
        {
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = Register(tab, plugin);
            SelectSheetSprite(tab);
            tab.Frame();
            tab.Frame();
            TextureRegionOverlayManager overlay = tab.Plugin.DisplayController.RegionOverlay;
            tab.Camera.Y = 0; // the top of the texture is the top of the view
            TextureRegion hero = new TextureRegion("Hero_Idle", "Hero_Idle", 8, 0, 48, 64);

            overlay.Hovered = hero;
            overlay.Refresh();

            var label = overlay.VisibleLabelPlacement!.Value;
            float regionBottom = (hero.Y + hero.Height - tab.Camera.Y) * tab.Camera.Zoom;
            label.Y.ShouldBeGreaterThan(regionBottom, "below the region");
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
                new TextureRegion("Panel", "Panel", 0, 0, 200, 150),
                new TextureRegion("Icon", "Icon", 60, 60, 20, 20),
            });
            using IDisposable registration = Register(tab, plugin);
            ComponentSave button = SelectSheetSprite(tab);

            Click(tab, 70, 70);
            Value(button, "Icon.TextureWidth").ShouldBe(20, "the small region on top of the panel");

            Click(tab, 150, 100);
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
            Click(tab, 140, 24);
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
                many.Add(new TextureRegion($"Slice_{i}", $"Slice_{i}", i % 20 * 16, i / 20 * 10, 15, 9));
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

    internal static void Click(TextureCoordinateTabHarness tab, float textureX, float textureY)
    {
        // The camera re-centers on a new selection a frame later; settle it before aiming.
        tab.Frame();
        tab.Frame();
        Point point = tab.WindowPointOf(textureX, textureY);
        tab.Drag(point, point);
    }

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
