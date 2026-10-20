using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Every FormsProperty declared by a shipped behavior must name a writable property on the
/// Forms control the behavior is for, and its declared default must be what a code-created
/// control already has. The runtime applier writes the declared default onto every
/// tool-authored control that did not author a value, so a default that differs from the
/// control's own changes behavior for every project; a name the control does not have is
/// silently skipped, so the authored value never reaches the game.
/// </summary>
public class BehaviorFormsPropertyCatalogTests : BaseTestClass
{
    private static readonly Dictionary<string, Func<FrameworkElement>> ControlFactories =
        new Dictionary<string, Func<FrameworkElement>>
        {
            ["ButtonBehavior"] = () => new Button(),
            ["CheckBoxBehavior"] = () => new CheckBox(),
            ["ColorPickerBehavior"] = () => new ColorPicker(),
            ["ComboBoxBehavior"] = () => new ComboBox(),
            ["DialogBoxBehavior"] = () => new DialogBox(),
            ["ExpanderBehavior"] = () => new Expander(),
            ["ItemsControlBehavior"] = () => new ItemsControl(),
            ["LabelBehavior"] = () => new Label(),
            ["ListBoxBehavior"] = () => new ListBox(),
            ["ListBoxItemBehavior"] = () => new ListBoxItem(),
            ["MenuBehavior"] = () => new Menu(),
            ["MenuItemBehavior"] = () => new MenuItem(),
            ["PanelBehavior"] = () => new Panel(),
            ["PasswordBoxBehavior"] = () => new PasswordBox(),
            ["RadioButtonBehavior"] = () => new RadioButton(),
            ["ScrollBarBehavior"] = () => new ScrollBar(),
            ["ScrollViewerBehavior"] = () => new ScrollViewer(),
            ["SliderBehavior"] = () => new Slider(),
            ["SplitterBehavior"] = () => new Splitter(),
            ["StackPanelBehavior"] = () => new StackPanel(),
            ["TextBoxBehavior"] = () => new TextBox(),
            ["ToggleBehavior"] = () => new ToggleButton(),
            ["UserControlBehavior"] = () => new UserControl(),
            ["WindowBehavior"] = () => new Gum.Forms.Window(),
        };

    /// <summary>Declared defaults that differ from a computed runtime default; see #6255.</summary>
    private static readonly HashSet<string> DefaultNotYetPinned = new HashSet<string>(StringComparer.Ordinal)
    {
        "ScrollBarBehavior.LargeChange",
        "ScrollViewerBehavior.LargeChange",
    };

    public static IEnumerable<object[]> BehaviorProperties()
    {
        foreach (BehaviorSave behavior in LoadShippedBehaviors())
        {
            if (!ControlFactories.ContainsKey(behavior.Name))
            {
                continue;
            }
            foreach (VariableSave property in behavior.FormsProperties)
            {
                yield return new object[] { behavior.Name, property.Name };
            }
        }
    }

    [Theory]
    [MemberData(nameof(BehaviorProperties))]
    public void FormsProperty_ShouldBeWritableOnControlWithMatchingDefault(string behaviorName, string propertyName)
    {
        BehaviorSave behavior = LoadShippedBehaviors().First(item => item.Name == behaviorName);
        VariableSave declaration = behavior.FormsProperties.First(item => item.Name == propertyName);
        FrameworkElement control = ControlFactories[behaviorName]();

        PropertyInfo? property = control.GetType().GetProperty(propertyName);
        property.ShouldNotBeNull($"{behaviorName} declares {propertyName}, which {control.GetType().Name} does not have");
        property.CanWrite.ShouldBeTrue($"{control.GetType().Name}.{propertyName} has no public setter");

        if (declaration.Value == null || DefaultNotYetPinned.Contains($"{behaviorName}.{propertyName}"))
        {
            return;
        }

        Type target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        object expected = target.IsEnum
            ? Enum.Parse(target, declaration.Value.ToString()!, ignoreCase: true)
            : Convert.ChangeType(declaration.Value, target);
        object? actual = property.GetValue(control);

        actual.ShouldBe(expected, $"{behaviorName}.{propertyName} declares {declaration.Value}, but a new {control.GetType().Name} has {actual}");
    }

    private static List<BehaviorSave> LoadShippedBehaviors()
    {
        string directory = Path.Combine(FindTemplatesDirectory(), "FormsBehaviors");
        List<BehaviorSave> behaviors = new List<BehaviorSave>();
        foreach (string path in Directory.GetFiles(directory, "*.behx"))
        {
            BehaviorSave? behavior = GumFileSerializer.DeserializeBehaviorSave(File.ReadAllText(path), projectVersion: 2);
            behavior.ShouldNotBeNull(path);
            behaviors.Add(behavior);
        }
        return behaviors;
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
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
