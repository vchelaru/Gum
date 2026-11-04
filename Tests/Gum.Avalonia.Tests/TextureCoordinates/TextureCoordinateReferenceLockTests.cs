using Avalonia;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// A texture coordinate that a variable reference sets is locked in the Texture Coordinates tab the
/// way an unexposed one is (#7577): the selector cannot change it, the free coordinates still work.
/// </summary>
[Trait("Category", "EndToEnd")]
public class TextureCoordinateReferenceLockTests
{
    #region Pinned exposure behavior

    [SkippableFact]
    public void AnInstanceExposingOnlyTop_ADragMovesOnlyTop()
    {
        OnTab(tab =>
        {
            ComponentSave icon = tab.Project.AddComponent("Icon");
            string atlas = tab.AddTextureFile("Atlas.png");
            tab.AddSprite(icon, "Sprite", atlas, left: 32, top: 32, width: 64, height: 64);
            using (tab.Project.UndoManager.RequestLock(icon))
            {
                icon.DefaultState!.GetVariableSave("Sprite.TextureTop")!.ExposedAsName = "IconTop";
            }
            ComponentSave button = tab.Project.AddComponent("Button");
            InstanceSave myIcon = tab.Project.AddInstance(button, "MyIcon", "Icon");
            tab.Tree.SaveAll();
            tab.Select(myIcon);

            tab.Drag(tab.WindowPointOf(64, 64), tab.WindowPointOf(104, 84));

            tab.Editor.SavedValue(button, "MyIcon.IconTop").ShouldBe(52, tab.Describe());
            tab.Canvas.CanChangeX.ShouldBeFalse("the reference owns Left");
            tab.Canvas.CanChangeY.ShouldBeTrue();
            tab.Canvas.RectangleSelector!.Left.ShouldBe(40, "the selector does not show a Left the reference then snaps back");
            tab.Canvas.RectangleSelector!.Left.ShouldBe(32, "Left is not exposed, so it cannot move");
        });
    }

    #endregion

    #region Direct sprite

    [SkippableFact]
    public void ALeftSetByAReference_ABodyDragMovesOnlyTheFreeCoordinates_AndNeverSnapsBack()
    {
        OnTab(tab =>
        {
            (ComponentSave button, InstanceSave sprite) = SpriteWithFrame(tab, frameX: 40);
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureLeft = Frame.X");
            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(40, "the reference applies");
            tab.Frame();

            tab.Drag(tab.WindowPointOf(72, 64), tab.WindowPointOf(112, 84));

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(40, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(52, tab.Describe());
            tab.Canvas.RectangleSelector!.Left.ShouldBe(40, "the selector shows the value the reference gave");
            ReferenceLines(button, "Icon").ShouldBe(new[] { "TextureLeft = Frame.X" });
        });
    }

    [SkippableFact]
    public void ALeftSetByAReference_MatchesWhatAnUnexposedLeftDoes()
    {
        OnTab(tab =>
        {
            // The same body drag on an instance whose Left is not exposed.
            ComponentSave icon = tab.Project.AddComponent("Icon");
            string atlas = tab.AddTextureFile("Atlas.png");
            tab.AddSprite(icon, "Sprite", atlas, left: 40, top: 32, width: 64, height: 64);
            using (tab.Project.UndoManager.RequestLock(icon))
            {
                foreach (string name in new[] { "Top", "Width", "Height" })
                {
                    icon.DefaultState!.GetVariableSave($"Sprite.Texture{name}")!.ExposedAsName = $"Icon{name}";
                }
            }
            ComponentSave panel = tab.Project.AddComponent("Panel");
            InstanceSave myIcon = tab.Project.AddInstance(panel, "MyIcon", "Icon");
            tab.Tree.SaveAll();
            tab.Select(myIcon);
            tab.Drag(tab.WindowPointOf(72, 64), tab.WindowPointOf(112, 84));
            float unexposedLeft = tab.Canvas.RectangleSelector!.Left;
            object? unexposedTop = tab.Editor.SavedValue(panel, "MyIcon.IconTop");

            (ComponentSave button, InstanceSave sprite) = SpriteWithFrame(tab, frameX: 40, file: "Second.png");
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureLeft = Frame.X");
            tab.Frame();
            tab.Drag(tab.WindowPointOf(72, 64), tab.WindowPointOf(112, 84));

            tab.Canvas.RectangleSelector!.Left.ShouldBe(unexposedLeft);
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(unexposedTop);
        });
    }

    [SkippableFact]
    public void AWidthSetByAReference_DraggingTheRightEdgeDoesNotResizeIt_ButTheHeightStillResizes()
    {
        OnTab(tab =>
        {
            (ComponentSave button, InstanceSave sprite) = SpriteWithFrame(tab, frameX: 40);
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureWidth = Frame.Width");
            int width = (int)tab.Editor.SavedValue(button, "Icon.TextureWidth")!;
            tab.Frame();

            tab.Drag(tab.WindowPointOf(32 + width, 64), tab.WindowPointOf(32 + width + 20, 64));
            tab.Editor.SavedValue(button, "Icon.TextureWidth").ShouldBe(width, tab.Describe());

            tab.Drag(tab.WindowPointOf(32 + width / 2f, 96), tab.WindowPointOf(32 + width / 2f, 116));
            tab.Editor.SavedValue(button, "Icon.TextureHeight").ShouldBe(84, tab.Describe());
        });
    }

    [SkippableFact]
    public void ALeftSetByAReference_ClickingARegionAppliesTheFreeCoordinatesOnly()
    {
        OnTab(tab =>
        {
            SampleSpriteSheet.Write(Path.Combine(tab.Project.ProjectFolder, "Sheet.png"));
            SampleRegionPlugin plugin = new SampleRegionPlugin("Sheet.png", SampleRegionPlugin.FromSheet(SampleSpriteSheet.Few));
            using IDisposable registration = TextureRegionScenarioTests.Register(tab, plugin);
            ComponentSave button = tab.Project.AddComponent("Button");
            tab.Project.AddInstance(button, "Frame", "Container");
            InstanceSave sprite = tab.AddSprite(button, "Icon", "Sheet.png", left: 8, top: 8, width: 16, height: 16);
            using (tab.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Frame.X", 40f, "float");
            }
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureLeft = Frame.X");

            TextureRegionScenarioTests.Click(tab, 140, 24); // Coin: 128, 12, 24x24

            button.DefaultState!.GetValue("Icon.TextureLeft").ShouldBe(40, "the reference keeps Left");
            button.DefaultState.GetValue("Icon.TextureTop").ShouldBe(12);
            button.DefaultState.GetValue("Icon.TextureWidth").ShouldBe(24);
            button.DefaultState.GetValue("Icon.TextureHeight").ShouldBe(24);
        });
    }

    [SkippableFact]
    public void AReferenceOnAnotherInstanceOrAnotherVariable_LocksNothing()
    {
        OnTab(tab =>
        {
            (ComponentSave button, InstanceSave sprite) = SpriteWithFrame(tab, frameX: 40);
            InstanceSave other = tab.AddSprite(button, "Other", tab.AddTextureFile("Other.png"), left: 32, top: 32, width: 64, height: 64);
            tab.Select(other);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureLeft = Frame.X");
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "X = Frame.X");
            tab.Frame();

            tab.Drag(tab.WindowPointOf(64, 64), tab.WindowPointOf(104, 84));

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(72, tab.Describe());
            tab.Editor.SavedValue(button, "Icon.TextureTop").ShouldBe(52);
        });
    }

    [SkippableFact]
    public void ClearingTheReference_UnlocksTheCoordinate()
    {
        OnTab(tab =>
        {
            (ComponentSave button, InstanceSave sprite) = SpriteWithFrame(tab, frameX: 40);
            tab.Select(sprite);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "TextureLeft = Frame.X");
            tab.Frame();
            tab.Drag(tab.WindowPointOf(72, 64), tab.WindowPointOf(112, 84));
            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(40);

            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "// TextureLeft = Frame.X");
            tab.Frame();
            tab.Drag(tab.WindowPointOf(72, 84), tab.WindowPointOf(92, 84));

            tab.Editor.SavedValue(button, "Icon.TextureLeft").ShouldBe(60, tab.Describe() + " refs=[" + string.Join("|", ReferenceLines(button, "Icon")) + "]");
        });
    }

    #endregion

    #region Exposed through an instance

    [SkippableFact]
    public void AnExposedLeftSetByAReferenceOnTheInstance_IsLocked_AndTheOtherExposedOnesStillMove()
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
            tab.Project.AddInstance(button, "Frame", "Container");
            InstanceSave myIcon = tab.Project.AddInstance(button, "MyIcon", "Icon");
            using (tab.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Frame.X", 40f, "float");
            }
            tab.Tree.SaveAll();
            tab.Select(myIcon);
            tab.Tree.Grid.TypeLinesAndApply("VariableReferences", "IconLeft = Frame.X");
            tab.Frame();

            tab.Drag(tab.WindowPointOf(72, 64), tab.WindowPointOf(112, 84));

            tab.Editor.SavedValue(button, "MyIcon.IconLeft").ShouldBe(40, tab.Describe());
            tab.Editor.SavedValue(button, "MyIcon.IconTop").ShouldBe(52, tab.Describe());
            tab.Canvas.CanChangeX.ShouldBeFalse("the reference owns Left");
            tab.Canvas.CanChangeY.ShouldBeTrue();
            tab.Canvas.RectangleSelector!.Left.ShouldBe(40, "the selector does not show a Left the reference then snaps back");
        });
    }

    #endregion

    private static (ComponentSave Button, InstanceSave Sprite) SpriteWithFrame(TextureCoordinateTabHarness tab, float frameX, string file = "Atlas.png")
    {
        ComponentSave button = tab.Project.AddComponent("Button");
        tab.Project.AddInstance(button, "Frame", "Container");
        string atlas = tab.AddTextureFile(file);
        InstanceSave sprite = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
        using (tab.Project.UndoManager.RequestLock(button))
        {
            button.DefaultState!.SetValue("Frame.X", frameX, "float");
            button.DefaultState.SetValue("Frame.Width", 64f, "float");
        }
        tab.Tree.SaveAll();
        return (button, sprite);
    }

    private static string[] ReferenceLines(ComponentSave owner, string instance) =>
        owner.DefaultState!.GetVariableListSave($"{instance}.VariableReferences")?.ValueAsIList?.Cast<string>().ToArray()
        ?? Array.Empty<string>();

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
