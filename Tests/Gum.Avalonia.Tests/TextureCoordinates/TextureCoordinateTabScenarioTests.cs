using Avalonia;
using Gum.DataTypes;
using Gum.Avalonia.Shell;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.PropertiesWindowPlugin;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
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
                tab.IsTabInShell.ShouldBeFalse("the tab of a plugin that is off is hidden"));            tab.Select(button);
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

    [SkippableFact]
    [Trait("Feature", "TEX-007")]
    public void Background_FollowsTheThemesCheckerColors_AndTheProjectsCheckerSetting()
    {
        OnTab(tab =>
        {
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);
            IThemingService theming = TestAppBuilder.Services.GetRequiredService<IThemingService>();
            System.Drawing.Color? originalCheckerA = theming.CheckerA;
            ProjectPropertiesViewModel properties = ProjectProperties();
            bool originalChecker = properties.ShowCheckerBackground;
            SolidRectangle solid = tab.Canvas.SystemManagers.ShapeManager.SolidRectangles.Single(shape => shape.Name == "Background Solid Color");
            Sprite checker = tab.Canvas.SystemManagers.SpriteManager.Sprites.Single(sprite => sprite.Name == "Background checkerboard Sprite");
            System.Drawing.Color picked = System.Drawing.Color.FromArgb(255, 12, 34, 56);
            try
            {
                checker.Visible.ShouldBe(properties.ShowCheckerBackground, tab.Describe());
                solid.Color.ShouldBe(theming.EffectiveSettings.CheckerA);

                // The dialog applies a color while it is open; Cancel puts it back, so no setting is saved.
                System.Drawing.Color? whileOpen = null;
                tab.Project.Dialogs.AnswerNext<ThemingDialogViewModel>(dialog =>
                {
                    dialog.CheckerAColor = picked;
                    tab.Frame();
                    whileOpen = solid.Color;
                    return false;
                });
                tab.Tree.PickMainMenu("View", "Theming");
                tab.Frame();
                whileOpen.ShouldBe(picked, "the tab's background takes the theme's checker color");
                solid.Color.ShouldBe(theming.EffectiveSettings.CheckerA, "Cancel puts the color back");

                properties.ShowCheckerBackground = !originalChecker;
                tab.Frame();
                checker.Visible.ShouldBe(!originalChecker, "the project's checker setting shows or hides the checkerboard here too");
            }
            finally
            {
                properties.ShowCheckerBackground = originalChecker;
                theming.CheckerA = originalCheckerA;
            }

            checker.Visible.ShouldBe(originalChecker);
            tab.Editor.AssertOracles();
        });
    }

    private static ProjectPropertiesViewModel ProjectProperties()
    {
        AvaloniaTabManager tabs = (AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>();
        AvaloniaPluginTab tab = tabs.AllTabs.Single(candidate => candidate.Title == "Project Properties");
        return (ProjectPropertiesViewModel)((global::Avalonia.Controls.Control)tab.Content).DataContext!;
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
