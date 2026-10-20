using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A visual variable that a behavior derives from a Forms property (the left side of a
/// ToolOnlyVariableReference, e.g. LineModeCategoryState from TextWrapping) must be hidden from
/// instances on every template component using that behavior. The runtime reads the Forms
/// property, so an instance that sets the visual variable directly looks right in the tool and
/// behaves differently in the game.
/// </summary>
public class TemplateDerivedVariablesHiddenTests
{
    /// <summary>The Standard template and every theme folder, so a new theme is enforced without editing this list.</summary>
    public static IEnumerable<object[]> TemplateProjects
    {
        get
        {
            string templates = FindTemplatesDirectory();
            List<string> paths = new List<string> { "FormsTemplate" };
            paths.AddRange(Directory.GetDirectories(Path.Combine(templates, "FormsThemes"))
                .Select(directory => "FormsThemes/" + Path.GetFileName(directory))
                .OrderBy(path => path, StringComparer.Ordinal));
            return paths.Select(path => new object[] { path });
        }
    }

    /// <summary>
    /// Categories on a Forms control that an instance is meant to set: a ListBox's Fixed/FitChildren
    /// sizing and a Label's text styles. Every other category on a Forms control is driven by the
    /// control itself at runtime.
    /// </summary>
    private static readonly HashSet<string> InstanceAuthoredCategories = new HashSet<string>(StringComparer.Ordinal)
    {
        "SizingCategory",
        "TextCategory",
    };

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void DerivedVariables_ShouldBeHiddenFromInstances(string templateRelativePath)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindTemplatesDirectory(), templateRelativePath, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;

        List<string> unhidden = new List<string>();
        foreach (ComponentSave component in project.Components)
        {
            foreach (ElementBehaviorReference reference in component.Behaviors)
            {
                BehaviorSave? behavior = project.Behaviors.FirstOrDefault(item => item.Name == reference.BehaviorName);
                if (behavior == null)
                {
                    continue;
                }
                foreach (string assignment in behavior.ToolOnlyVariableReferences)
                {
                    string derivedVariable = assignment.Split('=')[0].Trim();
                    if (!component.VariablesHiddenFromInstances.Contains(derivedVariable))
                    {
                        unhidden.Add($"{component.Name}.{derivedVariable} (from {behavior.Name})");
                    }
                }
            }
        }

        unhidden.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void ControlDrivenStateCategories_ShouldBeHiddenFromInstances(string templateRelativePath)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindTemplatesDirectory(), templateRelativePath, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;

        List<string> unhidden = new List<string>();
        foreach (ComponentSave component in project.Components.Where(item => item.Behaviors.Count > 0))
        {
            foreach (StateSaveCategory category in component.Categories)
            {
                string stateVariable = category.Name + "State";
                if (!InstanceAuthoredCategories.Contains(category.Name)
                    && !component.VariablesHiddenFromInstances.Contains(stateVariable))
                {
                    unhidden.Add($"{component.Name}.{stateVariable}");
                }
            }
        }

        unhidden.ShouldBeEmpty();
    }

    private static readonly string[] AllStructureVariables =
    {
        "ContainedType",
        "ChildrenLayout",
        "ClipsChildren",
        "IsRenderTarget",
        "ExposeChildrenEvents",
    };

    private static readonly string[] NoStructureVariables = Array.Empty<string>();

    /// <summary>
    /// For every Forms control (the last segment of the component name), the container-structure
    /// variables it must hide from instances. A variable not listed may be exposed. Only a bare
    /// container, where the control is the visual itself, exposes any: its instance's structure is
    /// the author's to choose. A control with something internal (an inner panel, a clip container,
    /// scroll bars, a background) owns its structure, so an instance setting it would change that
    /// internal structure out from under the control. StackPanel is a bare container, but its
    /// Orientation drives ChildrenLayout through a behavior reference, so that one stays hidden.
    /// A control added to the template needs an entry here, which forces the decision.
    /// </summary>
    private static readonly Dictionary<string, string[]> MustHideStructureVariables = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        // Bare containers: the control is the visual.
        ["Panel"] = NoStructureVariables,
        ["UserControl"] = NoStructureVariables,
        ["StackPanel"] = new[] { "ChildrenLayout" },

        // Everything else has something internal.
        ["Button"] = AllStructureVariables,
        ["ButtonClose"] = AllStructureVariables,
        ["ButtonConfirm"] = AllStructureVariables,
        ["ButtonDeny"] = AllStructureVariables,
        ["ButtonIcon"] = AllStructureVariables,
        ["ButtonStandard"] = AllStructureVariables,
        ["ButtonStandardIcon"] = AllStructureVariables,
        ["ButtonTab"] = AllStructureVariables,
        ["CheckBox"] = AllStructureVariables,
        ["ColorPicker"] = AllStructureVariables,
        ["ColorPickerStandard"] = AllStructureVariables,
        ["ComboBox"] = AllStructureVariables,
        ["DialogBox"] = AllStructureVariables,
        ["Expander"] = AllStructureVariables,
        ["ExpanderStandard"] = AllStructureVariables,
        ["ItemsControl"] = AllStructureVariables,
        ["Keyboard"] = AllStructureVariables,
        ["KeyboardKey"] = AllStructureVariables,
        ["Label"] = AllStructureVariables,
        ["ListBox"] = AllStructureVariables,
        ["ListBoxItem"] = AllStructureVariables,
        ["Menu"] = AllStructureVariables,
        ["MenuItem"] = AllStructureVariables,
        ["PasswordBox"] = AllStructureVariables,
        ["RadioButton"] = AllStructureVariables,
        ["ScrollBar"] = AllStructureVariables,
        ["ScrollBarButton"] = AllStructureVariables,
        ["ScrollBarThumb"] = AllStructureVariables,
        ["ScrollViewer"] = AllStructureVariables,
        ["SettingsView"] = AllStructureVariables,
        ["Slider"] = AllStructureVariables,
        ["SliderThumb"] = AllStructureVariables,
        ["Splitter"] = AllStructureVariables,
        ["SplitterStandard"] = AllStructureVariables,
        ["TextBox"] = AllStructureVariables,
        ["Thumb"] = AllStructureVariables,
        ["Toast"] = AllStructureVariables,
        ["TreeView"] = AllStructureVariables,
        ["TreeViewItem"] = AllStructureVariables,
        ["TreeViewToggle"] = AllStructureVariables,
        ["Window"] = AllStructureVariables,
    };

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void ContainerStructureVariables_ShouldBeHiddenPerControlTable(string templateRelativePath)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindTemplatesDirectory(), templateRelativePath, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;

        List<string> violations = new List<string>();
        foreach (ComponentSave component in project.Components.Where(item => item.Behaviors.Count > 0))
        {
            string control = component.Name.Split('/').Last();
            if (!MustHideStructureVariables.TryGetValue(control, out string[]? mustHide))
            {
                violations.Add($"{component.Name}: no entry in {nameof(MustHideStructureVariables)}");
                continue;
            }

            foreach (string variable in mustHide.Where(item => !component.VariablesHiddenFromInstances.Contains(item)))
            {
                violations.Add($"{component.Name}.{variable}");
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void ControlTable_ShouldOnlyListControlsThatExistInATemplate()
    {
        StandardElementsManager.Self.Initialize();
        HashSet<string> controls = new HashSet<string>(StringComparer.Ordinal);
        foreach (object[] row in TemplateProjects)
        {
            string projectPath = Path.Combine(FindTemplatesDirectory(), (string)row[0], "GumProject.gumx");
            ProjectLoadResult result = new ProjectLoader().Load(projectPath);
            result.Success.ShouldBeTrue(result.ErrorMessage);
            foreach (ComponentSave component in result.Project!.Components.Where(item => item.Behaviors.Count > 0))
            {
                controls.Add(component.Name.Split('/').Last());
            }
        }

        MustHideStructureVariables.Keys.Where(item => !controls.Contains(item)).ShouldBeEmpty();
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate the templates directory from " + AppContext.BaseDirectory);
    }
}
