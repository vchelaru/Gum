using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// Undo and redo of renaming an animation: the keyframes that play it through an instance in other
/// elements follow, including copies made after the rename that no undo record knows about.
/// </summary>
public class AnimationRenameUndoTests
{
    private const string Category = "AnimationCategory";

    #region Undo and redo follow the references that exist now

    [AvaloniaFact]
    public void UndoingTheRename_RestoresAKeyframeCopiedIntoAnotherElementAfterTheRename()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ComponentSave menu = AddPlayer(editor, scenario.IconType, "Menu", "IconInstance.Wink");

        editor.Undo();

        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Blink" });
        editor.ReadSavedAnimations(scenario.IconType).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Blink");
    }

    [AvaloniaFact]
    public void RedoingTheRename_RenamesEveryCurrentReference_IncludingACopyMadeAfterTheRename()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ComponentSave menu = AddPlayer(editor, scenario.IconType, "Menu", "IconInstance.Wink");
        editor.Undo();
        // A copy made while the rename is undone plays the old name.
        ComponentSave hud = AddPlayer(editor, scenario.IconType, "Hud", "IconInstance.Blink");

        editor.Redo();

        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Wink" });
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Wink" });
        PlayedAnimations(editor, hud).ShouldBe(new[] { "IconInstance.Wink" });
    }

    [AvaloniaFact]
    public void UndoingAndRedoingTheRenameTwice_RestoresEveryFileByteForByte()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ProjectFileSnapshot renamed = editor.SnapshotFiles();

        editor.Undo();
        editor.SnapshotFiles().ShouldMatch(scenario.Start, "first undo");
        editor.Redo();
        editor.SnapshotFiles().ShouldMatch(renamed, "first redo");
        editor.Undo();
        editor.SnapshotFiles().ShouldMatch(scenario.Start, "second undo");
        editor.Redo();
        editor.SnapshotFiles().ShouldMatch(renamed, "second redo");
    }

    [AvaloniaFact]
    public void UndoingTheRename_RestoresSeveralKeyframesInSeveralElements_AndLeavesOtherAnimationsAlone()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = InstanceSubAnimationTests.ButtonWithAnimatedIcon(editor);
        InstanceSubAnimationTests.AddIconBlink(editor, icon);
        ComponentSave iconType = editor.Project.Components.Single(component => component.Name == "Icon");
        ComponentSave menu = AddPlayer(editor, iconType, "Menu", "IconInstance.Blink", "IconInstance.Spin", "IconInstance.Blink");
        ComponentSave hud = AddPlayer(editor, iconType, "Hud", "IconInstance.Blink", "IconInstance.Blink");
        ProjectFileSnapshot start = RenameBlink(editor, iconType, "Wink");
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Wink", "IconInstance.Spin", "IconInstance.Wink" });

        editor.Undo();

        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Blink", "IconInstance.Spin", "IconInstance.Blink" });
        PlayedAnimations(editor, hud).ShouldBe(new[] { "IconInstance.Blink", "IconInstance.Blink" });
        PlayedAnimations(editor, button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.SnapshotFiles().ShouldMatch(start, "undoing should restore every file");

        editor.Redo();

        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Wink", "IconInstance.Spin", "IconInstance.Wink" });
        PlayedAnimations(editor, hud).ShouldBe(new[] { "IconInstance.Wink", "IconInstance.Wink" });
        PlayedAnimations(editor, button).ShouldBe(new[] { "IconInstance.Wink" });
    }

    [AvaloniaFact]
    public void UndoingTheRename_RestoresAReferenceInAScreen()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ScreenSave menu = editor.AddScreen("MainMenu", Category, "Shown");
        editor.AddInstance(menu, "IconInstance", scenario.IconType);
        WritePlayer(editor.AnimationFilePath(menu), "IconInstance.Wink");

        editor.Undo();
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Blink" });
        editor.Redo();
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Wink" });
    }

    [AvaloniaFact]
    public void RenamingTwice_UndoesAndRedoesEachRenameInOrder()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        RenameAnimation(editor, scenario.IconType, "Wink", "Flash");
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Flash" });

        editor.Undo();
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Wink" });
        editor.Undo();
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.SnapshotFiles().ShouldMatch(scenario.Start, "both undone");

        editor.Redo();
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Wink" });
        editor.Redo();
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Flash" });
    }

    [AvaloniaFact]
    public void UndoingTheRename_AlsoUndoesEditsMadeToTheElementAfterIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        editor.AddStateKeyframe($"{Category}/Dim");

        editor.Undo();
        editor.Undo();

        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.SnapshotFiles().ShouldMatch(scenario.Start, "undoing the keyframe and then the rename");
    }

    #endregion

    #region Nested sub-animations

    [AvaloniaFact]
    public void RenamingAnAnimationThatIsItselfPlayedFromAnotherAnimation_UndoesAndRedoesInTheElementPlayingIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = InstanceSubAnimationTests.ButtonWithAnimatedIcon(editor);
        InstanceSubAnimationTests.AddIconBlink(editor, icon);
        // Panel plays Button's Walk, which plays Icon's Blink.
        ComponentSave panel = editor.AddComponent("Panel", Category, "Open");
        editor.AddInstance(panel, "ButtonInstance", button);
        WritePlayer(editor.AnimationFilePath(panel), "ButtonInstance.Walk");
        editor.Select(button);
        ProjectFileSnapshot start = editor.StartScenario();

        RenameAnimation(editor, button, "Walk", "Run");

        PlayedAnimations(editor, panel).ShouldBe(new[] { "ButtonInstance.Run" });
        PlayedAnimations(editor, button).ShouldBe(new[] { "IconInstance.Blink" });
        ProjectFileSnapshot renamed = editor.SnapshotFiles();

        editor.Undo();
        PlayedAnimations(editor, panel).ShouldBe(new[] { "ButtonInstance.Walk" });
        editor.SnapshotFiles().ShouldMatch(start, "undo");
        editor.Redo();
        editor.SnapshotFiles().ShouldMatch(renamed, "redo");
    }

    [AvaloniaFact]
    public void UndoingTheRename_RestoresKeyframesInAnElementThatPlaysAnotherElementPlayingIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = InstanceSubAnimationTests.ButtonWithAnimatedIcon(editor);
        InstanceSubAnimationTests.AddIconBlink(editor, icon);
        ComponentSave iconType = editor.Project.Components.Single(component => component.Name == "Icon");
        ComponentSave panel = editor.AddComponent("Panel", Category, "Open");
        editor.AddInstance(panel, "ButtonInstance", button);
        WritePlayer(editor.AnimationFilePath(panel), "ButtonInstance.Walk");

        ProjectFileSnapshot start = RenameBlink(editor, iconType, "Wink");
        PlayedAnimations(editor, panel).ShouldBe(new[] { "ButtonInstance.Walk" });
        editor.Undo();

        PlayedAnimations(editor, button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.SnapshotFiles().ShouldMatch(start, "undo");
    }

    #endregion

    #region Which references are renamed

    [AvaloniaFact]
    public void AnInstanceOfAnotherComponent_PlayingItsOwnAnimationWithTheSameName_IsNeverRenamed()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor, beforeRename: (e, icon) =>
        {
            ComponentSave other = e.AddComponent("Other", Category, "A");
            e.Select(other);
            e.AddAnimation("Blink");
            ComponentSave menu = e.AddComponent("Menu", Category, "A");
            e.AddInstance(menu, "OtherInstance", other);
            WritePlayer(e.AnimationFilePath(menu), "OtherInstance.Blink");
        });
        ComponentSave menu = editor.Project.Components.Single(component => component.Name == "Menu");
        PlayedAnimations(editor, menu).ShouldBe(new[] { "OtherInstance.Blink" });

        editor.Undo();
        PlayedAnimations(editor, menu).ShouldBe(new[] { "OtherInstance.Blink" });
        editor.Redo();
        PlayedAnimations(editor, menu).ShouldBe(new[] { "OtherInstance.Blink" });
    }

    #endregion

    #region Name clash

    [AvaloniaFact]
    public void UndoingTheRename_WhenAnotherAnimationAlreadyUsesTheOldName_RefusesWithAMessageAndChangesNothing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        // An external edit adds a different animation named Blink to Icon's sidecar.
        string iconPath = editor.AnimationFilePath(scenario.IconType);
        ElementAnimationsSave external = ElementAnimationsSave.Load(iconPath);
        external.Animations.Add(new AnimationSave { Name = "Blink" });
        external.Save(iconPath);
        editor.Plugin.CallReactToFileChanged(new FilePath(iconPath));
        editor.Layout();
        ProjectFileSnapshot beforeUndo = editor.SnapshotFiles();
        int messages = editor.Dialogs.Messages.Count;

        editor.Undo();

        editor.Dialogs.Messages.Count.ShouldBe(messages + 1);
        editor.Dialogs.Messages.Last().ShouldContain("Blink");
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Wink" });
        editor.SnapshotFiles().ShouldMatch(beforeUndo, "a refused undo changes nothing");
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Wink", "Blink" });
    }

    #endregion

    #region Referencing elements that changed since

    [AvaloniaFact]
    public void UndoingTheRename_SkipsAReferencingElementDeletedSinceWithoutFailing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ComponentSave menu = AddPlayer(editor, scenario.IconType, "Menu", "IconInstance.Wink");
        editor.Project.Components.Remove(menu);
        editor.Project.ComponentReferences.RemoveAll(reference => reference.Name == "Menu");

        editor.Undo();

        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.ThrowIfPluginFailed();
    }

    [AvaloniaFact]
    public void UndoingTheRename_SkipsAKeyframeWhoseInstanceWasDeletedSince()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        ComponentSave menu = AddPlayer(editor, scenario.IconType, "Menu", "IconInstance.Wink");
        menu.Instances.Clear();

        editor.Undo();

        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        PlayedAnimations(editor, menu).ShouldBe(new[] { "IconInstance.Wink" });
        editor.ThrowIfPluginFailed();
    }

    [AvaloniaFact]
    public void UndoingTheRename_FollowsTheReferencingElementsOwnInstanceRename()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        // Menu renamed its instance after the rename: its keyframe plays Glyph.Wink.
        ComponentSave menu = editor.AddComponent("Menu", Category, "A");
        editor.AddInstance(menu, "Glyph", scenario.IconType);
        WritePlayer(editor.AnimationFilePath(menu), "Glyph.Wink");

        editor.Undo();

        PlayedAnimations(editor, menu).ShouldBe(new[] { "Glyph.Blink" });
    }

    #endregion

    #region The Animations tab afterwards

    [AvaloniaFact]
    public void AfterUndo_SelectingTheReferencingElement_ShowsTheRestoredKeyframe_AndWritesNothing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        editor.Undo();
        ProjectFileSnapshot afterUndo = editor.SnapshotFiles();

        editor.Select(scenario.Button);

        AnimatedKeyframeViewModel sub = editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => !string.IsNullOrEmpty(keyframe.AnimationName));
        sub.AnimationName.ShouldBe("IconInstance.Blink");
        sub.IsMissingReference.ShouldBeFalse();
        editor.SnapshotFiles().ShouldMatch(afterUndo, "selecting is not an edit");
    }

    [AvaloniaFact]
    public void AfterRedo_SelectingTheReferencingElement_ShowsTheRenamedKeyframe_AndWritesNothing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        editor.Undo();
        PlayedAnimations(editor, scenario.Button).ShouldBe(new[] { "IconInstance.Blink" });
        editor.Redo();
        ProjectFileSnapshot afterRedo = editor.SnapshotFiles();

        editor.Select(scenario.Button);

        AnimatedKeyframeViewModel sub = editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => !string.IsNullOrEmpty(keyframe.AnimationName));
        sub.AnimationName.ShouldBe("IconInstance.Wink");
        sub.IsMissingReference.ShouldBeFalse();
        editor.SnapshotFiles().ShouldMatch(afterRedo, "selecting is not an edit");
    }

    [AvaloniaFact]
    public void UndoingTheRename_TakesNoUndoStepInTheReferencingElement()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Scenario scenario = RenameBlinkToWink(editor);
        editor.Undo();
        editor.Select(scenario.Button);

        editor.UndoManager.CanUndo().ShouldBeTrue("Button keeps the undo history of its own edits");
        editor.UndoManager.CanRedo().ShouldBeFalse();
        editor.Select(scenario.IconType);
        editor.UndoManager.CanRedo().ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private record Scenario(ComponentSave Button, ComponentSave IconType, ProjectFileSnapshot Start);

    /// <summary>
    /// Button plays Icon's Blink through IconInstance; Icon is selected and its Blink renamed to Wink.
    /// <paramref name="beforeRename"/> may add more files first; Start is the files just before the rename.
    /// </summary>
    private static Scenario RenameBlinkToWink(AnimationEditorHarness editor, Action<AnimationEditorHarness, InstanceSave>? beforeRename = null)
    {
        (ComponentSave button, InstanceSave icon) = InstanceSubAnimationTests.ButtonWithAnimatedIcon(editor);
        InstanceSubAnimationTests.AddIconBlink(editor, icon);
        ComponentSave iconType = editor.Project.Components.Single(component => component.Name == "Icon");
        beforeRename?.Invoke(editor, icon);
        ProjectFileSnapshot start = RenameBlink(editor, iconType, "Wink");
        return new Scenario(button, iconType, start);
    }

    /// <summary>Selects <paramref name="iconType"/>, starts the scenario and renames Blink; returns the files before the rename.</summary>
    private static ProjectFileSnapshot RenameBlink(AnimationEditorHarness editor, ComponentSave iconType, string newName)
    {
        editor.Select(iconType);
        ProjectFileSnapshot start = editor.StartScenario();
        RenameAnimation(editor, iconType, "Blink", newName);
        return start;
    }

    private static void RenameAnimation(AnimationEditorHarness editor, ComponentSave owner, string from, string to)
    {
        editor.Select(owner);
        AnimationViewModel animation = editor.ViewModel.Animations.Single(item => item.Name == from);
        editor.Click(editor.RowFor(editor.AnimationList, animation));
        editor.Dialogs.AnswerNextUserString(to);
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Rename Animation").Action!();
        editor.Layout();
        editor.ThrowIfPluginFailed();
    }

    /// <summary>A component holding an instance of <paramref name="iconType"/> named IconInstance whose Intro plays each of <paramref name="played"/>.</summary>
    private static ComponentSave AddPlayer(AnimationEditorHarness editor, ComponentSave iconType, string name, params string[] played)
    {
        ComponentSave player = editor.AddComponent(name, Category, "A");
        editor.AddInstance(player, "IconInstance", iconType);
        WritePlayer(editor.AnimationFilePath(player), played);
        return player;
    }

    /// <summary>Writes an Intro animation with one sub-animation keyframe per name, a second apart.</summary>
    private static void WritePlayer(string path, params string[] played)
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationSave intro = new AnimationSave { Name = "Intro" };
        for (int i = 0; i < played.Length; i++)
        {
            intro.Animations.Add(new AnimationReferenceSave { Name = played[i], Time = i });
        }
        animations.Animations.Add(intro);
        animations.Save(path);
    }

    /// <summary>The animations the element's saved sidecar plays through instances, in order.</summary>
    private static List<string> PlayedAnimations(AnimationEditorHarness editor, ElementSave element) =>
        editor.ReadSavedAnimations(element).ShouldNotBeNull().Animations
            .SelectMany(animation => animation.Animations)
            .Select(reference => reference.Name)
            .ToList();

    #endregion
}
