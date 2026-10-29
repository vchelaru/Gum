#nullable enable
using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Tests.Shared;

/// <summary>
/// Builds components whose base type changed from Text to Container (or whose Text instance became a
/// Container), leaving Text-only variables behind. The tool keeps those variables on purpose, in case the
/// type change was accidental, and the tests that use this project pin that.
/// <para>
/// With <c>typeChanged: false</c> the project is the state just before the user changes the types; with
/// <c>true</c> it is what the tool saves just after. The variables are identical in both.
/// </para>
/// <list type="bullet">
/// <item><c>Label</c>: Text and FontSize in Default, Text in category state Looks/Big, shared Width.</item>
/// <item><c>Card</c>: instance <c>Caption</c>; Caption.Text (exposed as CaptionText) and Caption.FontSize in
/// Default, Caption.Text in Mode/Alt.</item>
/// <item><c>Outer</c>: instance Card1 of Card that sets the exposed CaptionText.</item>
/// <item><c>Reader</c>: a <c>VariableReferences</c> line that reads Card.CaptionText.</item>
/// <item><c>Wrapper</c>: leftover Text and FontSize, plus a Text instance named <c>Inner</c>; always saved
/// in its after state.</item>
/// </list>
/// </summary>
public static class LeftoverVariablesProject
{
    public const string LabelText = "Greetings";
    public const string LabelBigText = "BigHello";
    public const float LabelFontSize = 33f;
    public const string CaptionTextValue = "Hi";
    public const string CaptionAltText = "AltHi";
    public const string OuterCaptionText = "FromOuter";
    public const string ReferenceLine = "Text = Components/Card.CaptionText";

    public static void AddComponents(GumProjectSave project, bool typeChanged)
    {
        string textOrContainer = typeChanged ? "Container" : "Text";

        ComponentSave label = AddComponent(project, "Label", textOrContainer);
        Add(label.DefaultState!, "Text", "string", LabelText);
        Add(label.DefaultState!, "FontSize", "int", 33);
        Add(label.DefaultState!, "Width", "float", 77f);
        StateSave big = AddCategoryState(label, "Looks", "Big");
        Add(big, "Text", "string", LabelBigText);

        ComponentSave card = AddComponent(project, "Card", "Container");
        card.Instances.Add(new InstanceSave { Name = "Caption", BaseType = textOrContainer, ParentContainer = card });
        Add(card.DefaultState!, "Caption.Text", "string", CaptionTextValue).ExposedAsName = "CaptionText";
        Add(card.DefaultState!, "Caption.FontSize", "int", 44);
        Add(card.DefaultState!, "Caption.Width", "float", 55f);
        StateSave alt = AddCategoryState(card, "Mode", "Alt");
        Add(alt, "Caption.Text", "string", CaptionAltText).ExposedAsName = "CaptionText";

        ComponentSave outer = AddComponent(project, "Outer", "Container");
        outer.Instances.Add(new InstanceSave { Name = "Card1", BaseType = "Card", ParentContainer = outer });
        Add(outer.DefaultState!, "Card1.CaptionText", "string", OuterCaptionText);

        ComponentSave reader = AddComponent(project, "Reader", textOrContainer);
        Add(reader.DefaultState!, "Text", "string", CaptionTextValue);
        VariableListSave<string> references = new VariableListSave<string> { Name = "VariableReferences", Type = "string" };
        references.Value.Add(ReferenceLine);
        reader.DefaultState!.VariableLists.Add(references);

        ComponentSave wrapper = AddComponent(project, "Wrapper", "Container");
        Add(wrapper.DefaultState!, "Text", "string", LabelText);
        Add(wrapper.DefaultState!, "FontSize", "int", 33);
        wrapper.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Text", ParentContainer = wrapper });
        Add(wrapper.DefaultState!, "Inner.Text", "string", "InnerText");
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static StateSave AddCategoryState(ComponentSave component, string categoryName, string stateName)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        StateSave state = new StateSave { Name = stateName, ParentContainer = component };
        category.States.Add(state);
        component.Categories.Add(category);
        return state;
    }

    private static VariableSave Add(StateSave state, string name, string type, object value)
    {
        VariableSave variable = new VariableSave { Name = name, Type = type, Value = value, SetsValue = true };
        state.Variables.Add(variable);
        return variable;
    }

    /// <summary>
    /// Every variable and variable list of every state of every component, as sorted text lines, so two
    /// projects can be compared with one assertion.
    /// </summary>
    public static List<string> Snapshot(GumProjectSave project)
    {
        List<string> lines = new List<string>();
        foreach (ComponentSave component in project.Components)
        {
            foreach (StateSave state in component.AllStates)
            {
                string owner = component.Name + "/" + state.Name;
                foreach (VariableSave variable in state.Variables)
                {
                    lines.Add($"{owner}|{variable.Name}|{variable.Type}|{variable.Value}|sets={variable.SetsValue}|exposed={variable.ExposedAsName}");
                }
                foreach (VariableListSave list in state.VariableLists)
                {
                    string values = list is VariableListSave<string> strings ? string.Join(";", strings.Value) : string.Empty;
                    lines.Add($"{owner}|{list.Name}|list|{values}");
                }
            }
        }
        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
