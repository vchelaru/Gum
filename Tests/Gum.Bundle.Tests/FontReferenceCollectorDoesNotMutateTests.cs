using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Collecting fonts is a read of the project (an error check runs it on every selection), so it
/// must resolve <c>VariableReferences</c> without writing the evaluated values into the saved
/// state (#5736).
/// </summary>
public class FontReferenceCollectorDoesNotMutateTests : IDisposable
{
    public FontReferenceCollectorDoesNotMutateTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_leaves_saved_state_unchanged_but_still_collects_the_referenced_font()
    {
        const int storedFontSize = 18;
        const int referencedFontSize = 30;

        ComponentSave panel = TestProjectBuilder.BuildComponent("Panel");
        StateSave state = panel.DefaultState!;
        panel.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = panel });
        SetVariable(state, "HeaderSize", referencedFontSize, "int");
        SetVariable(state, "Label.Font", "Arial", "string");
        SetVariable(state, "Label.FontSize", storedFontSize, "int");

        VariableListSave<string> references = new VariableListSave<string>
        {
            Name = "Label.VariableReferences",
            Type = "string"
        };
        references.ValueAsIList.Add("FontSize = HeaderSize");
        state.VariableLists.Add(references);

        VariableSave labelFontSize = state.Variables.Single(v => v.Name == "Label.FontSize");
        int variableCountBefore = state.Variables.Count;

        Dictionary<string, BmfcSave> fonts = Collect(panel);

        labelFontSize.Value.ShouldBe(storedFontSize);
        state.Variables.Count.ShouldBe(variableCountBefore);
        fonts.Values.Select(f => f.FontSize).ShouldBe(new float[] { referencedFontSize });
    }

    private static void SetVariable(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value, Type = type });
    }

    private static Dictionary<string, BmfcSave> Collect(ComponentSave component)
    {
        GumProjectSave project = TestProjectBuilder.BuildProject(
            components: new[] { component },
            standards: new[] { TestProjectBuilder.BuildStandard("Text") });
        ObjectFinder.Self.GumProjectSave = project;

        Gum.Expressions.GumExpressionService.Initialize();
        try
        {
            FontReferenceCollector collector = new FontReferenceCollector(
                instance => ObjectFinder.Self.GetElementSave(instance));
            return collector.Collect(project, new[] { (ElementSave)component });
        }
        finally
        {
            GumRuntime.ElementSaveExtensions.ClearRegistrations();
        }
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
