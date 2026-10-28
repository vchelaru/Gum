using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Found while testing #7005. Reopening a project in which two components name each other as BaseType
/// never finishes loading: the test hangs inside SaveAndReload, so it must be run with a hang timeout
/// (dotnet test --blame-hang-timeout 60s).
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
}
