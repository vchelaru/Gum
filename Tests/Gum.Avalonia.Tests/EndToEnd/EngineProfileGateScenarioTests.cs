using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.Plugins.ImportPlugin.Manager;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The paths an engine profile restricts that a user can still reach by right-clicking, dragging,
/// importing or duplicating (#7722). Each scenario builds the project unrestricted, then applies
/// the profile, since a profile never rewrites what a file already holds.
/// </summary>
[Trait("Category", "EndToEnd")]
public class EngineProfileGateScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private static void HideParentOnSprites(EngineProfile profile)
    {
        profile.VariableRules.Add(new VariableRule { Pattern = "Sprite.Parent", Action = VariableRuleAction.Hide });
    }

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    #region Add to base

    private static (ComponentSave derived, InstanceSave glow) BuildDerivedWithGlow(ProjectTreeHarness tree)
    {
        tree.Project.AddComponent("Button");
        ComponentSave fancy = tree.Project.AddComponent("FancyButton");
        tree.Click(tree.NodeFor(fancy));
        tree.Grid.PickComboItem("BaseType", "Button");
        InstanceSave glow = tree.Project.AddInstance(fancy, "Glow", "Sprite");
        return (fancy, glow);
    }

    [AvaloniaFact]
    public void AddToBase_WithoutAProfile_IsOffered()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (_, InstanceSave glow) = BuildDerivedWithGlow(tree);
        tree.Click(tree.NodeFor(glow));

        tree.RightClick(tree.NodeFor(glow));

        tree.MenuHeaders().ShouldContain(header => header.StartsWith("Add Glow to base Button"));
    }

    [AvaloniaFact]
    public void AddToBase_InstanceTypeTheProjectDoesNotSupport_IsNotOffered()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave fancy, InstanceSave glow) = BuildDerivedWithGlow(tree);
        tree.Project.AddInstance(fancy, "Box", "Container");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.SupportedStandardTypes.Add("Container");
        tree.Click(tree.NodeFor(glow));

        tree.RightClick(tree.NodeFor(glow));

        tree.MenuHeaders().ShouldNotContain(header => header.StartsWith("Add Glow to base"));
    }

    [AvaloniaFact]
    public void AddToBase_ParentedInstanceWhileHierarchyIsUnsupported_IsNotOffered()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave fancy, InstanceSave glow) = BuildDerivedWithGlow(tree);
        tree.Project.AddInstance(fancy, "Frame", "Container");
        fancy.DefaultState!.SetValue("Glow.Parent", "Frame", "string");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        HideParentOnSprites(scope.Profile);
        tree.Click(tree.NodeFor(glow));

        tree.RightClick(tree.NodeFor(glow));

        tree.MenuHeaders().ShouldNotContain(header => header.StartsWith("Add Glow to base"));
    }

    #endregion

    #region Drops

    [AvaloniaFact]
    public void DroppingABehavior_WhenBehaviorsAreDisabled_IsRefused()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        BehaviorSave clickable = tree.Project.Project.Behaviors.Single();
        ComponentSave button = tree.Project.AddComponent("Button");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableBehaviors = true;

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(clickable));
        tree.DropOn(tree.NodeFor(button)).ShouldBe(DragDropEffects.None);

        button.Behaviors.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void DroppingAnInstanceOnABehavior_WhenBehaviorsAreDisabled_IsRefused()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        BehaviorSave clickable = tree.Project.Project.Behaviors.Single();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableBehaviors = true;

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(button.GetInstance("Background")!));
        tree.DropOn(tree.NodeFor(clickable)).ShouldBe(DragDropEffects.None);

        clickable.RequiredInstances.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void DroppingAComponentOnAScreen_WhenComponentsAreDisabled_IsRefused()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableComponents = true;

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(button));
        tree.DropOn(tree.NodeFor(title)).ShouldBe(DragDropEffects.None);

        title.Instances.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void DroppingAnInstanceOnAnotherElementsInstance_WhileHierarchyIsUnsupported_WritesNoParent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave menu = tree.Project.AddScreen("Menu");
        tree.Project.AddInstance(menu, "Icon", "Sprite");
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Frame", "Container");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        HideParentOnSprites(scope.Profile);

        tree.Drag(tree.NodeFor(menu.GetInstance("Icon")!), tree.NodeFor(card.GetInstance("Frame")!));

        card.Instances.Select(instance => instance.Name).ShouldContain("Icon");
        card.DefaultState!.GetValue("Icon.Parent").ShouldBeNull();
    }

    #endregion

    #region Import

    [AvaloniaFact]
    public void ComponentsFolderMenu_WhenComponentsAreDisabled_OffersNoImport()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableComponents = true;

        tree.RightClick(tree.RootNode("Components"));

        tree.MenuHeaders().ShouldNotContain("Import Components");
    }

    [AvaloniaFact]
    public void ComponentsFolderMenu_WithoutAProfile_OffersImport()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();

        tree.RightClick(tree.RootNode("Components"));

        tree.MenuHeaders().ShouldContain("Import Components");
    }

    [AvaloniaFact]
    public void ImportComponent_WhenComponentsAreDisabled_RefusesWithAMessageAndAddsNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableComponents = true;
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        ComponentSave incoming = new ComponentSave { Name = "Incoming" };
        IImportLogic importLogic = Services.GetRequiredService<IImportLogic>();

        ComponentSave? result = importLogic.ImportComponent(incoming, saveProject: false);

        result.ShouldBeNull();
        tree.Project.Project.Components.ShouldNotContain(incoming);
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("components"));
    }

    [AvaloniaFact]
    public void ImportBehavior_WhenBehaviorsAreDisabled_RefusesWithAMessageAndCopiesNoFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.DisableBehaviors = true;
        string incomingFolder = Path.Combine(Path.GetTempPath(), "GumProfileGates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(incomingFolder);
        FilePath incoming = Path.Combine(incomingFolder, "Clickable.behx");
        File.WriteAllText(incoming.FullPath, "<BehaviorSave />");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        IImportLogic importLogic = Services.GetRequiredService<IImportLogic>();

        BehaviorSave? result = importLogic.ImportBehavior(incoming);

        result.ShouldBeNull();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldBeFalse();
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("behaviors"));
    }

    #endregion

    #region Duplicating an element

    private static ScreenSave BuildScreenWithParentedSprite(ProjectTreeHarness tree)
    {
        ScreenSave main = tree.Project.AddScreen("Main");
        tree.Project.AddInstance(main, "Frame", "Container");
        tree.Project.AddInstance(main, "Glow", "Sprite");
        main.DefaultState!.SetValue("Glow.Parent", "Frame", "string");
        return main;
    }

    [AvaloniaFact]
    public void DuplicateScreen_WithoutAProfile_KeepsParents()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = BuildScreenWithParentedSprite(tree);
        tree.Click(tree.NodeFor(main));

        tree.Dialogs.AnswerNextUserString("MainCopy");
        tree.RightClick(tree.NodeFor(main));
        tree.PickMenu("Duplicate Main");

        ScreenSave copy = tree.Project.Project.Screens.Single(screen => screen.Name == "MainCopy");
        copy.DefaultState!.GetValue("Glow.Parent").ShouldBe("Frame");
    }

    [AvaloniaFact]
    public void DuplicateScreen_HoldingAnUnsupportedType_IsRefusedWithAMessage()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = BuildScreenWithParentedSprite(tree);
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.SupportedStandardTypes.Add("Container");
        tree.Click(tree.NodeFor(main));

        tree.Dialogs.AnswerNextUserString("MainCopy");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.RightClick(tree.NodeFor(main));
        tree.PickMenu("Duplicate Main");

        tree.Project.Project.Screens.ShouldNotContain(screen => screen.Name == "MainCopy");
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("Sprite"));
    }

    [AvaloniaFact]
    public void DuplicateScreen_WhileHierarchyIsUnsupported_WritesNoParentsToTheCopy_AndLeavesTheOriginal()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = BuildScreenWithParentedSprite(tree);
        using EngineProfileScope scope = new EngineProfileScope(Services);
        HideParentOnSprites(scope.Profile);
        tree.Click(tree.NodeFor(main));

        tree.Dialogs.AnswerNextUserString("MainCopy");
        tree.RightClick(tree.NodeFor(main));
        tree.PickMenu("Duplicate Main");

        ScreenSave copy = tree.Project.Project.Screens.Single(screen => screen.Name == "MainCopy");
        copy.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Frame", "Glow" });
        copy.DefaultState!.GetValue("Glow.Parent").ShouldBeNull();
        main.DefaultState!.GetValue("Glow.Parent").ShouldBe("Frame");
    }

    [AvaloniaFact]
    public void PasteCopiedScreen_HoldingAnUnsupportedType_IsRefusedWithAMessage()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = BuildScreenWithParentedSprite(tree);
        using EngineProfileScope scope = new EngineProfileScope(Services);
        scope.Profile.SupportedStandardTypes.Add("Container");
        tree.Click(tree.NodeFor(main));
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        ICopyPasteLogic copyPaste = Services.GetRequiredService<ICopyPasteLogic>();

        copyPaste.OnCopy(CopyType.InstanceOrElement);
        copyPaste.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Recursive);

        tree.Project.Project.Screens.Count.ShouldBe(1);
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("Sprite"));
    }

    #endregion
}
