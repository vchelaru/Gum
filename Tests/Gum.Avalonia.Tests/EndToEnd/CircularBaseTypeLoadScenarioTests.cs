using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Plugins.Errors;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Found while testing #7005, fixed in #7020. Reopening a project in which two components name each other
/// as BaseType overflowed the stack (the whole process crashed). The project now loads with the cycle as an
/// error row, and the tool does not let the user create the cycle.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CircularBaseTypeLoadScenarioTests
{
    [AvaloniaFact]
    public void CircularBaseTypes_LoadWithoutDisablingTheTree()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave first = tree.Project.AddComponent("First");
        tree.Project.AddInstance(first, "A", "Container");
        ComponentSave second = tree.Project.AddComponent("Second");
        tree.Project.AddInstance(second, "B", "Container");
        tree.SaveAll();
        first.BaseType = "Second";
        second.BaseType = "First";
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        tree.Project.Project.Components.Single(component => component.Name == "First").Instances.Select(instance => instance.Name).ShouldBe(new[] { "B", "A" }, ignoreOrder: true);
        tree.Project.Project.Components.Single(component => component.Name == "First").Instances.ShouldAllBe(instance => ReferenceEquals(instance.ParentContainer, tree.Project.Project.Components.Single(component => component.Name == "First")));
    }

    [AvaloniaFact]
    public void CircularBaseTypes_ShowAnErrorRowNamingTheCycle_AfterReload()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave first = tree.Project.AddComponent("First");
        ComponentSave second = tree.Project.AddComponent("Second");
        tree.SaveAll();
        first.BaseType = "Second";
        second.BaseType = "First";
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.Project.SelectedState.SelectedElement = tree.Project.Project.Components.Single(component => component.Name == "First");

        AvaloniaPluginTab tab = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>().AllTabs.Single(item => item.Title == "Errors");
        AllErrorsViewModel errors = tab.Content as AllErrorsViewModel ?? (AllErrorsViewModel)((Control)tab.Content).DataContext!;
        errors.Errors.ShouldContain(error => error.Code == "GUM0014" && error.Message.Contains("First -> Second -> First"));
    }

    [AvaloniaFact]
    public void TypingABaseTypeThatDerivesFromTheElement_IsRejectedAndTheBaseTypeKept()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave first = tree.Project.AddComponent("First");
        tree.Project.AddComponent("Second", baseType: "First");
        tree.Click(tree.NodeFor(first));
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        TextBox typed = tree.Grid.Combo("BaseType").GetVisualDescendants().OfType<TextBox>().First();
        tree.Grid.Input.TypeAndEnter(typed, "Second");

        first.BaseType.ShouldBe("Container");
        tree.Dialogs.Messages.ShouldContain(message => message.Contains("circular reference"));
    }

    [AvaloniaFact]
    public void TheBasePicker_LeavesOutComponentsThatDeriveFromTheElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave first = tree.Project.AddComponent("First");
        tree.Project.AddComponent("Second", baseType: "First");
        tree.Project.AddComponent("Unrelated");
        tree.Click(tree.NodeFor(first));

        List<string?> offered = tree.Grid.Combo("BaseType").Items.Cast<object?>().Select(item => item?.ToString()).ToList();

        offered.ShouldContain("Unrelated");
        offered.ShouldNotContain("Second");
        offered.ShouldNotContain("First");
    }
}
