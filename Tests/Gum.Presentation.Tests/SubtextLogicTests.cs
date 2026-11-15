using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The detail text under a Variables tab row is the variable's own description plus any extra lines.</summary>
public class SubtextLogicTests
{
    private readonly SubtextLogic _sut = new SubtextLogic();

    private static ComponentSave CreateComponent()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        component.States.Add(new StateSave { Name = "Default" });
        return component;
    }

    [Fact]
    public void GetDefaultSubtext_WithNoSubtext_StartsWithTheDescriptionNotABlankLine()
    {
        VariableSave variable = new VariableSave { Name = "Flag", DetailText = "Describes the flag." };

        string? result = _sut.GetDefaultSubtext(variable, null, "Flag", CreateComponent(), null);

        result.ShouldBe("Describes the flag.");
    }

    [Fact]
    public void GetDefaultSubtext_WithSubtext_PutsTheDescriptionOnTheNextLine()
    {
        VariableSave variable = new VariableSave { Name = "Flag", DetailText = "Describes the flag." };

        string? result = _sut.GetDefaultSubtext(variable, "Extra.", "Flag", CreateComponent(), null);

        result.ShouldBe("Extra.\nDescribes the flag.");
    }
}
