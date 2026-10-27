using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The file rows' drop-down (#6869) beyond the basic pick: which files each instance type is
/// offered, the copy prompt for a file outside the project, several selected instances, and a row
/// that cannot be edited.
/// </summary>
[Trait("Category", "EndToEnd")]
public class FileDropdownScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileDropdown_OffersWhatTheExtensionCheckAccepts_AndNotTextOrShaderFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        string content = Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png");
        File.Copy(content, Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        foreach (string other in new[] { "logo.svg", "anim.json", "notes.txt", "blur.fx" })
        {
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, other), "");
        }
        InstanceSave sprite = tree.Project.AddInstance(card, "Icon", "Sprite");
        StateSave state = card.GetDefaultStateOrThrow();
        InstanceSave svgFile = tree.Project.AddInstance(card, "Logo", "Sprite");
        InstanceSave lottieFile = tree.Project.AddInstance(card, "Spinner", "Sprite");
        state.SetValue("Logo.SourceFile", "logo.svg", "string");
        state.SetValue("Spinner.SourceFile", "anim.json", "string");
        InstanceSave holder = tree.Project.AddInstance(card, "Holder", "Sprite");
        state.SetValue("Holder.SourceFile", "Hero.png", "string");
        InstanceSave notes = tree.Project.AddInstance(card, "Notes", "Sprite");
        state.SetValue("Notes.SourceFile", "notes.txt", "string");
        InstanceSave shaded = tree.Project.AddInstance(card, "Shaded", "Sprite");
        state.SetValue("Shaded.SourceShaderFile", "blur.fx", "string");
        tree.SaveAll();
        VariableGridHarness grid = tree.Grid;

        // The plugin that accepts .svg and .json never looks at the instance's type (Svg and Lottie
        // instances cannot be created without the Skia standards, which the headless head lacks), so
        // every SourceFile row lists the same files. A text file or a shader is never offered.
        tree.Click(tree.NodeFor(sprite));
        grid.Settle();
        ComboBox combo = grid.Combo("SourceFile");
        combo.IsDropDownOpen = true;
        grid.Settle();

        combo.Items.Cast<object?>().ShouldBe(new object[] { "anim.json", "Hero.png", "logo.svg" });
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileDropdown_PickingAFileOutsideTheProject_AsksToCopyOrReferenceItLikeThePicker()
    {
        string outside = Path.Combine(Path.GetTempPath(), "Gum6869Outside_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            using ProjectTreeHarness tree = new ProjectTreeHarness();
            ComponentSave card = tree.Project.AddComponent("Card");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(outside, "Far.png"));
            InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Sprite");
            tree.Project.AddInstance(card, "Holder", "Sprite");
            string reference = Path.GetRelativePath(tree.Project.ProjectFolder, Path.Combine(outside, "Far.png")).Replace('\\', '/');
            card.GetDefaultStateOrThrow().SetValue("Holder.SourceFile", reference, "string");
            tree.SaveAll();
            tree.Click(tree.NodeFor(icon));
            VariableGridHarness grid = tree.Grid;
            grid.Settle();
            ProjectFileSnapshot start = tree.SnapshotFiles();

            // Cancel keeps the old value.
            tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog => false);
            grid.PickComboItem("SourceFile", reference);

            (VariableGridHarness.StoredValue(card, "Icon.SourceFile") as string).ShouldBeNullOrEmpty("cancelling the prompt leaves the value");

            // Referencing it in place keeps the path.
            tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
            {
                dialog.SelectedValue = dialog.OptionValues.First(option => option.StartsWith("Reference"));
                return true;
            });
            grid.PickComboItem("SourceFile", reference);

            VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe(reference);
            tree.Undo();
            tree.SnapshotFiles().ShouldMatch(start, "undoing the pick should restore the files");
            tree.AssertOracles();
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileDropdown_WithASpriteAndANineSliceSelected_ListsOnceAndPicksForBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        InstanceSave sprite = tree.Project.AddInstance(card, "Icon", "Sprite");
        InstanceSave nineSlice = tree.Project.AddInstance(card, "Panel", "NineSlice");
        tree.Project.AddInstance(card, "Holder", "Sprite");
        card.GetDefaultStateOrThrow().SetValue("Holder.SourceFile", "Hero.png", "string");
        tree.SaveAll();
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Select(sprite, nineSlice);
        grid.Combo("SourceFile").IsDropDownOpen = true;
        grid.Settle();
        grid.Combo("SourceFile").Items.Cast<object?>().ShouldBe(new object[] { "Hero.png" });
        grid.Combo("SourceFile").IsDropDownOpen = false;
        grid.PickComboItem("SourceFile", "Hero.png");

        VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe("Hero.png");
        VariableGridHarness.StoredValue(card, "Panel.SourceFile").ShouldBe("Hero.png");
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "one undo restores both instances");
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileDropdown_OnARowSetByAReference_CannotBePicked()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        InstanceSave leader = tree.Project.AddInstance(card, "Leader", "Sprite");
        InstanceSave follower = tree.Project.AddInstance(card, "Follower", "Sprite");
        card.GetDefaultStateOrThrow().SetValue("Leader.SourceFile", "Hero.png", "string");
        tree.SaveAll();
        tree.Click(tree.NodeFor(follower));
        VariableGridHarness grid = tree.Grid;

        grid.TypeLinesAndApply("VariableReferences", "SourceFile = Leader.SourceFile");

        grid.Member("SourceFile").IsReadOnly.ShouldBeTrue();
        grid.Combo("SourceFile").IsEffectivelyEnabled.ShouldBeFalse("a row set by a reference cannot be picked from");
        grid.Combo("SourceFile").SelectedItem.ShouldBe("Hero.png");
    }

    #region Typing

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_TypedPathUrlAndEmptyText_CommitOnEnter_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Sprite");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        tree.Click(tree.NodeFor(icon));
        VariableGridHarness grid = tree.Grid;
        grid.Settle();
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Input.TypeAndEnter(TypedField(grid), "Hero.png");
        VariableGridHarness.StoredValue(grid.ReadSaved(card), "Icon.SourceFile").ShouldBe("Hero.png");

        grid.Input.TypeAndEnter(TypedField(grid), "https://example.com/Hero.png");
        VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe("https://example.com/Hero.png");

        ClearAndEnter(grid);
        (VariableGridHarness.StoredValue(card, "Icon.SourceFile") as string).ShouldBeNullOrEmpty("emptying the text clears the value");

        tree.Undo();
        tree.Undo();
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the three edits should restore the files");
        tree.Redo();
        VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe("Hero.png");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_TypedInvalidValue_ShowsTheErrorAndKeepsTheOldValue()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Sprite");
        tree.Click(tree.NodeFor(icon));
        VariableGridHarness grid = tree.Grid;
        grid.Settle();
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        grid.Input.TypeAndEnter(TypedField(grid), "notes.txt");

        tree.Dialogs.Messages.ShouldHaveSingleItem().ShouldContain("txt");
        (VariableGridHarness.StoredValue(card, "Icon.SourceFile") as string).ShouldBeNullOrEmpty();
        grid.Combo("SourceFile").Text.ShouldBeNullOrEmpty("the row shows the kept value again");
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_TypedPathOutsideTheProject_StillAsksToCopyOrReference()
    {
        string outside = Path.Combine(Path.GetTempPath(), "Gum6869Typed_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            using ProjectTreeHarness tree = new ProjectTreeHarness();
            ComponentSave card = tree.Project.AddComponent("Card");
            InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Sprite");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(outside, "Far.png"));
            tree.Click(tree.NodeFor(icon));
            VariableGridHarness grid = tree.Grid;
            grid.Settle();
            string reference = Path.GetRelativePath(tree.Project.ProjectFolder, Path.Combine(outside, "Far.png")).Replace('\\', '/');

            tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
            {
                dialog.SelectedValue = dialog.OptionValues.First(option => option.StartsWith("Reference"));
                return true;
            });
            grid.Input.TypeAndEnter(TypedField(grid), reference);

            VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe(reference);
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_SetByAReference_StaysDisabledWhenTypedAt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Leader", "Sprite");
        InstanceSave follower = tree.Project.AddInstance(card, "Follower", "Sprite");
        card.GetDefaultStateOrThrow().SetValue("Leader.SourceFile", "Hero.png", "string");
        tree.SaveAll();
        tree.Click(tree.NodeFor(follower));
        VariableGridHarness grid = tree.Grid;
        grid.TypeLinesAndApply("VariableReferences", "SourceFile = Leader.SourceFile");

        TextBox field = TypedField(grid);
        field.IsEffectivelyEnabled.ShouldBeFalse();
        grid.Input.TypeAndEnter(field, "Other.png");

        VariableGridHarness.StoredValue(card, "Follower.SourceFile").ShouldBe("Hero.png");
        grid.Combo("SourceFile").IsEffectivelyEnabled.ShouldBeFalse();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_RetargetedToAnotherInstance_ShowsItsValueAndKeepsTypingAndTheList()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        InstanceSave first = tree.Project.AddInstance(card, "First", "Sprite");
        InstanceSave second = tree.Project.AddInstance(card, "Second", "Sprite");
        tree.Click(tree.NodeFor(first));
        VariableGridHarness grid = tree.Grid;
        grid.Settle();
        grid.Input.TypeAndEnter(TypedField(grid), "Hero.png");

        tree.Click(tree.NodeFor(second));
        grid.Settle();

        grid.Combo("SourceFile").Text.ShouldBeNullOrEmpty("the second instance has no file");
        grid.PickComboItem("SourceFile", "Hero.png");
        VariableGridHarness.StoredValue(card, "Second.SourceFile").ShouldBe("Hero.png");
        tree.Click(tree.NodeFor(first));
        grid.Settle();
        grid.Combo("SourceFile").Text.ShouldBe("Hero.png");
        ClearAndEnter(grid);
        (VariableGridHarness.StoredValue(card, "First.SourceFile") as string).ShouldBeNullOrEmpty();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-011")]
    public void SourceFileRow_TypedWithTwoInstancesSelected_SetsBoth_AndOneUndoRestoresThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(tree.Project.ProjectFolder, "Hero.png"));
        InstanceSave sprite = tree.Project.AddInstance(card, "Icon", "Sprite");
        InstanceSave nineSlice = tree.Project.AddInstance(card, "Panel", "NineSlice");
        tree.SaveAll();
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();
        grid.Select(sprite, nineSlice);

        grid.Input.TypeAndEnter(TypedField(grid), "Hero.png");

        VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBe("Hero.png");
        VariableGridHarness.StoredValue(card, "Panel.SourceFile").ShouldBe("Hero.png");
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "one undo restores both instances");
    }

    private static void ClearAndEnter(VariableGridHarness grid)
    {
        TextBox field = TypedField(grid);
        grid.Input.Click(field);
        field.Focus();
        field.SelectAll();
        grid.Input.Press(Key.Delete, PhysicalKey.Delete);
        grid.Input.Press(Key.Enter, PhysicalKey.Enter);
    }

    // The editable combo's own text field (the row's other text box, the value holder, is not shown).
    private static TextBox TypedField(VariableGridHarness grid) =>
        grid.Row("SourceFile").GetVisualDescendants().OfType<TextBox>().First();

    #endregion

    [AvaloniaTheory]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(24)]
    public void FileRow_PickAndRevealButtonsMatchTheFieldHeight_AtEveryZoom(double baseFontSize)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave icon = grid.Project.AddInstance(card, "Icon", "Sprite");
        grid.Select(icon);
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(grid.Input.Window, scale);
        try
        {
            scale.BaseFontSize = baseFontSize;
            grid.Settle();

            Control row = grid.Row("SourceFile");
            Control field = row.GetVisualDescendants().OfType<ComboBox>().First();
            List<Button> buttons = row.GetVisualDescendants().OfType<Button>().ToList();
            buttons.Count.ShouldBe(2);
            foreach (Button button in buttons)
            {
                button.Bounds.Height.ShouldBe(field.Bounds.Height, 0.5, $"the {button.Content} button at font size {baseFontSize}");
                double buttonTop = button.TranslatePoint(new Point(0, 0), grid.Input.Window)!.Value.Y;
                double fieldTop = field.TranslatePoint(new Point(0, 0), grid.Input.Window)!.Value.Y;
                buttonTop.ShouldBe(fieldTop, 0.5, $"the {button.Content} button's top at font size {baseFontSize}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    }
}
