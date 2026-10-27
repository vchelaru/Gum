using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// A component's Default view shows a Forms-assigned variable's value whatever the Enabled/Disabled category
/// state holds for it, whether the state sets it as a plain variable or as a reference row, and whether the
/// Forms value is authored or unset (#6908). A Default reference row on the name sets the final value. A Forms
/// value left unset shows what Default stores: its own value until the tool materializes the Forms default.
/// </summary>
public partial class StateSelectionCanvasSplashTests
{
    private const float FormsWidth = 77f;
    private const float FormsDefaultWidth = 10f;
    private const float DefaultOwnWidth = 64f;
    private const float DefaultRowWidth = 33f;
    private const float EnabledStateWidth = 5f;
    private const float DisabledStateWidth = 6f;

    public static IEnumerable<object[]> FormsValueCategoryRowCases()
    {
        foreach (string stateForm in new[] { "plain", "row", "crossElementRow" })
        {
            foreach (bool formsAuthored in new[] { true, false })
            {
                foreach (bool defaultRow in new[] { false, true })
                {
                    foreach (string isEnabled in new[] { "unset", "true", "false" })
                    {
                        // A Forms value that is set was materialized into Default; an unset one may not have been.
                        foreach (bool materialized in formsAuthored ? new[] { true } : new[] { true, false })
                        {
                            yield return new object[] { stateForm, formsAuthored, defaultRow, isEnabled, materialized };
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(FormsValueCategoryRowCases))]
    public void RefreshAll_DefaultSelectedAndCategoryStateSetsAFormsAssignedWidth_ShowsTheFormsOrDefaultValue(
        string stateForm, bool formsAuthored, bool defaultRow, string isEnabled, bool materialized)
    {
        ComponentSave button = BuildButtonWithFormsAssignedWidth(stateForm, formsAuthored, defaultRow, isEnabled, materialized);

        float expected = defaultRow ? DefaultRowWidth : formsAuthored ? FormsWidth : materialized ? FormsDefaultWidth : DefaultOwnWidth;
        ShowSelectedAndReadBackgroundWidth(button, button.DefaultState!).ShouldBe(expected);
    }

    /// <summary>A Forms-assigned color channel removes only that channel from a state's <c>Color</c> row.</summary>
    [Fact]
    public void RemoveSettersOf_ColorRowAndOneChannelNamed_KeepsTheOtherChannels()
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: false);
        StateSave disabled = button.Categories[0].States.Single(item => item.Name == "Disabled");

        Gum.Wireframe.SelectedStateReferenceMerger.RemoveSettersOf(disabled, new HashSet<string> { "Background.Red" });

        string[] lines = disabled.VariableLists.OfType<VariableListSave<string>>().SelectMany(list => list.Value).ToArray();
        lines.Length.ShouldBe(2);
        lines.ShouldNotContain(line => line.StartsWith("Red"));
        lines.ShouldContain(line => line.StartsWith("Green"));
        lines.ShouldContain(line => line.StartsWith("Blue"));
    }

    private static ComponentSave BuildButtonWithFormsAssignedWidth(string stateForm, bool formsAuthored, bool defaultRow, string isEnabled, bool materialized)
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: true);
        BehaviorSave behavior = ObjectFinder.Self.GumProjectSave!.Behaviors.Single();
        behavior.FormsProperties.Add(new VariableSave { Type = "float", Name = "BackgroundWidth", Value = 10f });
        behavior.ToolOnlyVariableReferences.Add("Background.Width = BackgroundWidth");

        foreach (string name in new[] { "StateSource", "DefaultSource" })
        {
            button.Instances.Add(new InstanceSave { Name = name, BaseType = "Rectangle", ParentContainer = button });
        }

        StateSave defaultState = button.DefaultState!;
        SetNumber(defaultState, "Background.Width", DefaultOwnWidth);
        SetNumber(defaultState, "DefaultSource.Width", DefaultRowWidth);
        if (defaultRow)
        {
            AddReference(defaultState, "Background", "Width = DefaultSource.Width");
        }
        if (formsAuthored)
        {
            defaultState.SetValue("BackgroundWidth", FormsWidth, "float");
        }
        if (isEnabled != "unset")
        {
            defaultState.SetValue("IsEnabled", isEnabled == "true", "bool");
        }

        foreach ((string stateName, float width) in new[] { ("Enabled", EnabledStateWidth), ("Disabled", DisabledStateWidth) })
        {
            StateSave state = button.Categories[0].States.Single(item => item.Name == stateName);
            if (stateForm == "plain")
            {
                SetNumber(state, "Background.Width", width);
            }
            else if (stateForm == "crossElementRow")
            {
                ComponentSave styles = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Styles");
                styles.Instances.Add(new InstanceSave { Name = stateName + "Width", BaseType = "Rectangle", ParentContainer = styles });
                SetNumber(styles.DefaultState!, stateName + "Width.Width", width);
                AddReference(state, "Background", $"Width = Components/Styles.{stateName}Width.Width");
            }
            else
            {
                SetNumber(defaultState, stateName + "Source.Width", width);
                button.Instances.Add(new InstanceSave { Name = stateName + "Source", BaseType = "Rectangle", ParentContainer = button });
                AddReference(state, "Background", $"Width = {stateName}Source.Width");
            }
        }

        if (materialized)
        {
            BehaviorToolOnlyReferencesApplier.Apply(button, defaultState);
        }
        return button;
    }
}
