using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
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
}
