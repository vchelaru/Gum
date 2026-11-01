using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The demo screen of every bundled project must ship its Resolution ListBox and Difficulty
/// ComboBox with items already in them (#7365), authored as ListBoxItem instances parented through
/// the control's Default Slot chain, never directly to the control.
/// </summary>
public class DemoScreenItemsTests
{
    public static IEnumerable<object[]> ProjectDirectories => new[]
    {
        "FormsTemplate",
        "FormsThemes/Bubblegum", "FormsThemes/DarkPro", "FormsThemes/Editor", "FormsThemes/ForestGlade",
        "FormsThemes/Hazard", "FormsThemes/Meadow", "FormsThemes/Neon", "FormsThemes/Retro95"
    }.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(ProjectDirectories))]
    public void DemoScreen_ShouldHaveListBoxItemsInResolutionBoxSlot(string projectDirectory)
    {
        GumProjectSave project = Load(projectDirectory);

        List<string> parents = GetItemParents(project, "ResolutionBox");

        parents.Count.ShouldBeGreaterThanOrEqualTo(3);
        parents.ShouldAllBe(parent => parent == GetSlotPath(project, "ResolutionBox"));
    }

    [Theory]
    [MemberData(nameof(ProjectDirectories))]
    public void DemoScreen_ShouldHaveListBoxItemsInComboBoxSlot(string projectDirectory)
    {
        GumProjectSave project = Load(projectDirectory);

        List<string> parents = GetItemParents(project, "ComboBoxInstance");

        parents.Count.ShouldBeGreaterThanOrEqualTo(3);
        parents.ShouldAllBe(parent => parent == GetSlotPath(project, "ComboBoxInstance"));
    }

    private static GumProjectSave Load(string projectDirectory)
    {
        StandardElementsManager.Self.Initialize();
        string path = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            projectDirectory, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(path);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        return result.Project!;
    }

    private static ScreenSave GetDemoScreen(GumProjectSave project) =>
        project.Screens.Single(screen => screen.Name.EndsWith("DemoScreenGum"));

    private static List<string> GetItemParents(GumProjectSave project, string containerName)
    {
        ScreenSave screen = GetDemoScreen(project);
        return screen.Instances
            .Where(instance => instance.BaseType.EndsWith("/ListBoxItem"))
            .Select(instance => screen.DefaultState.GetValue($"{instance.Name}.Parent") as string ?? "")
            .Where(parent => parent == containerName || parent.StartsWith(containerName + "."))
            .ToList();
    }

    // Follows each Default Slot in turn: ComboBox -> ListBoxInstance (a ListBox) -> InnerPanelInstance.
    private static string GetSlotPath(GumProjectSave project, string containerName)
    {
        ScreenSave screen = GetDemoScreen(project);
        string path = containerName;
        ComponentSave? component = project.Components.Single(c => c.Name == screen.GetInstance(containerName)!.BaseType);
        while (component != null)
        {
            string? slot = component.DefaultState.GetValue("DefaultChildContainer") as string;
            if (string.IsNullOrEmpty(slot))
            {
                break;
            }
            path += "." + slot;
            string slotBaseType = component.GetInstance(slot)!.BaseType;
            component = project.Components.FirstOrDefault(c => c.Name == slotBaseType);
        }
        return path;
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
