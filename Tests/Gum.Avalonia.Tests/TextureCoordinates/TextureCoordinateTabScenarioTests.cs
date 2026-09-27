using Avalonia;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// End-to-end scenarios on the Texture Coordinates tab (inventory area TEX): the head's own tab on a
/// graphics device, with selection made through the Project tree so the tab gets the visual and the
/// plugin events it gets in the tool.
/// </summary>
[Trait("Category", "EndToEnd")]
public class TextureCoordinateTabScenarioTests
{
    [SkippableFact]
    [Trait("Feature", "TEX-001")]
    [Trait("Feature", "TEX-002")]
    public void SelectingASprite_ShowsItsTextureAndRegion_AndDraggingTheRegionSavesTheNewCoordinates()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            ComponentSave panel = tab.Project.AddComponent("Panel");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(panel);
            tab.Tab.IsVisible.ShouldBeFalse("a Container has no texture coordinates");

            tab.Select(icon);

            tab.Tab.IsVisible.ShouldBeTrue(tab.Describe());
            tab.Canvas.CurrentTexture.ShouldNotBeNull(tab.Describe());
            tab.Canvas.CurrentTexture.Width.ShouldBe(256);
            tab.Canvas.RectangleSelectors.Count.ShouldBe(1, tab.Describe());
            tab.Canvas.RectangleSelector!.Left.ShouldBe(32);
            tab.Canvas.RectangleSelector.Width.ShouldBe(64);

            // Drag the region's body 40 texture pixels right and 20 down.
            Point from = tab.WindowPointOf(64, 64);
            Point to = tab.WindowPointOf(104, 84);
            tab.Drag(from, to);

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(72, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(52, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureWidth").ShouldBe(64);

            tab.Undo();
            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(32, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(32);
            tab.Canvas.RectangleSelector!.Left.ShouldBe(32, "the region follows the undo");

            tab.Editor.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "TEX-001")]
    public void TurningThePluginOffAndOn_LeavesTheTabWorking()
    {
        // #5310: turning the plugin off removed its tab and disposed its canvas background, and
        // turning it back on rebuilt neither.
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);
            tab.Tab.IsVisible.ShouldBeTrue(tab.Describe());

            tab.TurnPluginOffAndOn(whileOff: () =>
                tab.IsTabInShell.ShouldBeFalse("the tab of a plugin that is off is hidden"));
            // Turning it back on subscribes again to the render surface's one-time event.
            tab.Plugin.CallXnaInitialized();
            tab.TabManager.AllTabs.Count(candidate => candidate.Title == TextureCoordinateTabHarness.TabTitle).ShouldBe(1);
            tab.Select(button);
            tab.Select(icon);

            tab.IsTabInShell.ShouldBeTrue(tab.Describe());
            tab.Canvas.RectangleSelectors.Count.ShouldBe(1, tab.Describe());
            tab.Drag(tab.WindowPointOf(64, 64), tab.WindowPointOf(104, 64));
            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(72, tab.Describe());

            tab.Editor.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "TEX-003")]
    public void DraggingTheCornerHandle_ResizesTheRegion_AndKeepsItsTopLeft()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);

            tab.Drag(tab.WindowPointOf(96, 96), tab.WindowPointOf(112, 104));

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(32, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(32);
            tab.Editor.SavedValue(button, "Icon.TextureWidth").ShouldBe(80, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureHeight").ShouldBe(72);

            tab.Editor.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "TEX-004")]
    public void SnapToGrid_MovesTheRegionInGridSteps_AndIsSavedWithTheProject()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);

            tab.Input.Click(tab.SnapToGridCheckBox);
            tab.ViewModel.IsSnapToGridChecked.ShouldBeTrue();
            tab.ViewModel.SelectedSnapToGridValue.ShouldBe(16);
            // 21 pixels right, 5 down: to the nearest 16 that is one step right and none down.
            tab.Drag(tab.WindowPointOf(64, 64), tab.WindowPointOf(85, 69));

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(48, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(32, tab.Describe());
            File.ReadAllText(Path.Combine(tab.Project.ProjectFolder, "TextureCoordinateSettings.tcsj"))
                .ShouldContain("\"IsSnapToGridChecked\": true");

            tab.Editor.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "TEX-005")]
    public void ZoomingIn_ShowsTheTextureLarger_AndADragMovesTheRegionByTexturePixels()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);
            float zoomBefore = tab.Camera.Zoom;

            tab.Input.Click(tab.ZoomInButton);
            tab.Frame();

            tab.Camera.Zoom.ShouldBeGreaterThan(zoomBefore, tab.Describe());
            Point from = tab.WindowPointOf(64, 64);
            Point to = tab.WindowPointOf(84, 64);
            (to.X - from.X).ShouldBe(20 * tab.Camera.Zoom, tolerance: 0.5);
            tab.Drag(from, to);
            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(52, tab.Describe());

            tab.Editor.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "TEX-006")]
    public void AnInstanceExposingItsSpritesCoordinates_ShowsThatRegion_AndADragSetsTheExposedVariables()
    {
        OnTab(tab =>
        {
            ComponentSave icon = tab.Project.AddComponent("Icon");
            string atlas = tab.AddTextureFile("Atlas.png");
            tab.AddSprite(icon, "Sprite", atlas, left: 32, top: 32, width: 64, height: 64);
            using (tab.Project.UndoManager.RequestLock(icon))
            {
                foreach (string name in new[] { "Left", "Top", "Width", "Height" })
                {
                    icon.DefaultState!.GetVariableSave($"Sprite.Texture{name}")!.ExposedAsName = $"Icon{name}";
                }
            }
            ComponentSave button = tab.Project.AddComponent("Button");
            InstanceSave myIcon = tab.Project.AddInstance(button, "MyIcon", "Icon");
            tab.Tree.SaveAll();

            tab.Select(myIcon);

            tab.Tab.IsVisible.ShouldBeTrue(tab.Describe());
            tab.Canvas.RectangleSelectors.Count.ShouldBe(1, tab.Describe());
            tab.Canvas.RectangleSelector!.Left.ShouldBe(32);
            tab.Drag(tab.WindowPointOf(64, 64), tab.WindowPointOf(104, 84));

            tab.Editor.SavedValue(button, "MyIcon.IconLeft").ShouldBe(72, tab.Describe());
            tab.Editor.SavedValue(button, "MyIcon.IconTop").ShouldBe(52);
            tab.Editor.SavedValue(icon, "Sprite.TextureLeft").ShouldBe(32, "the drag changes the instance, not the component");
            // The edit refreshes the exposed sources, which must not drop the region.
            tab.Canvas.RectangleSelectors.Count.ShouldBe(1, tab.Describe());
            tab.Canvas.RectangleSelector!.Left.ShouldBe(72);

            tab.Editor.AssertOracles();
        });
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
