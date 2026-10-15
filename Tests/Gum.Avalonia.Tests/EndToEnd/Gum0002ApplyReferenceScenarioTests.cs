using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.Errors;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The Apply Reference button on a GUM0002 row in the Errors tab (#6051), through the tool's own
/// wiring: a typed value on a reference-driven row raises the row, the button asks, and the answer
/// decides whether the reference's value replaces the typed one.
/// </summary>
[Trait("Category", "EndToEnd")]
public class Gum0002ApplyReferenceScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void ApplyReferenceButton_ReplacesTheStaleValue_KeepsTheReference_AndUndoes()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = CreateCardWithStaleLabelWidth(tree, leaderWidth: "120", staleWidth: "50");
        AllErrorsViewModel errors = ErrorsViewModel();
        ErrorViewModel row = errors.Errors.ShouldHaveSingleItem();
        row.Code.ShouldBe("GUM0002");
        VariableGridHarness.StoredValue(card, "Label.Width").ShouldBe(50f);

        using HeadlessWindowDriver errorsWindow = HostErrorsTab();
        Button apply = ApplyButton(errorsWindow);
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        errorsWindow.Click(apply);
        tree.ThrowIfCrashed();

        tree.Dialogs.Messages.Last().ShouldContain("50");
        tree.Dialogs.Messages.Last().ShouldContain("120");
        VariableGridHarness.StoredValue(card, "Label.Width").ShouldBe(120f);
        card.GetDefaultStateOrThrow().GetVariableListSave("Label.VariableReferences")!.ValueAsIList.Cast<string>()
            .ShouldBe(new[] { "Width = Leader.Width" });
        errors.Errors.ShouldBeEmpty("the stale value no longer disagrees with its reference");

        tree.Undo();
        VariableGridHarness.StoredValue(card, "Label.Width").ShouldBe(50f, "one undo restores the typed value");
        errors.Errors.ShouldHaveSingleItem().Code.ShouldBe("GUM0002");
    }

    [AvaloniaFact]
    public void ApplyReferenceButton_ChangesNothing_WhenTheUserAnswersNo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = CreateCardWithStaleLabelWidth(tree, leaderWidth: "120", staleWidth: "50");
        AllErrorsViewModel errors = ErrorsViewModel();

        using HeadlessWindowDriver errorsWindow = HostErrorsTab();
        Button apply = ApplyButton(errorsWindow);
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
        errorsWindow.Click(apply);
        tree.ThrowIfCrashed();

        VariableGridHarness.StoredValue(card, "Label.Width").ShouldBe(50f);
        errors.Errors.ShouldHaveSingleItem().Code.ShouldBe("GUM0002");
    }

    /// <summary>
    /// A Card whose Label takes its Width from Leader by a reference, then reopened with a different
    /// Width stored on Label, as a file edited by hand or saved by an older version holds it (typing a
    /// value into a reference-driven row re-applies the reference instead). The Errors tab is left
    /// listing that conflict. Returns the reopened Card.
    /// </summary>
    private static ComponentSave CreateCardWithStaleLabelWidth(ProjectTreeHarness tree, string leaderWidth, string staleWidth)
    {
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave leader = tree.Project.AddInstance(card, "Leader", "Container");
        InstanceSave label = tree.Project.AddInstance(card, "Label", "Container");
        tree.Click(tree.NodeFor(leader));
        tree.Grid.TypeAndEnter("Width", leaderWidth);
        tree.Click(tree.NodeFor(label));
        tree.Grid.TypeLinesAndApply("VariableReferences", "Width = Leader.Width");
        card.GetDefaultStateOrThrow().Variables.Single(variable => variable.Name == "Label.Width").Value = float.Parse(staleWidth);
        tree.Project.SaveAndReload();

        ComponentSave reopened = tree.Project.Project.Components.Single(component => component.Name == "Card");
        tree.Click(tree.NodeFor(reopened));
        tree.ThrowIfCrashed();
        return reopened;
    }

    private static AvaloniaPluginTab ErrorsTab() =>
        ((AvaloniaTabManager)Services.GetRequiredService<ITabManager>()).AllTabs.Single(tab => tab.Title == "Errors");

    private static AllErrorsViewModel ErrorsViewModel() => (AllErrorsViewModel)((Control)ErrorsTab().Content).DataContext!;

    private static HeadlessWindowDriver HostErrorsTab() =>
        new HeadlessWindowDriver((Control)ErrorsTab().Content, 700, 400, framesFolderName: "GumApplyReferenceScenarios");

    private static Button ApplyButton(HeadlessWindowDriver window) =>
        window.Window.GetVisualDescendants().OfType<Button>().Single(button => button.IsEffectivelyVisible && button.Content as string == "Apply Reference");
}
