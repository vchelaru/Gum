using Gum.DataTypes;
using Gum.Wireframe;
using Gum.DataTypes.Variables;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;
public class StateSaveCategoryTests
{
    [Fact]
    public void Constructor_ShouldSetStates()
    {
        StateSaveCategory sut = new ();
        sut.States.ShouldNotBeNull();
    }

    [Fact]
    public void SetValues_ShouldSetAllStateValues()
    {
        StateSaveCategory sut = new();
        sut.States.Add(new StateSave { Name = "State1" });
        sut.States.Add(new StateSave { Name = "State2" });
        sut.States.Add(new StateSave { Name = "State3" });
        sut.SetValues("TestVariable", 1f);
        foreach(var state in sut.States)
        {
            state.Variables.Count.ShouldBe(1);
            state.Variables[0].Name.ShouldBe("TestVariable");
            state.Variables[0].Value.ShouldBe(1f);
        }
    }

    [Fact]
    public void RemoveValue_ShouldRemoveAllStateValues()
    {
        StateSaveCategory sut = new();
        sut.States.Add(new StateSave { Name = "State1" });
        sut.States.Add(new StateSave { Name = "State2" });
        sut.States.Add(new StateSave { Name = "State3" });
        sut.SetValues("TestVariable", 1f);
        sut.RemoveValues("TestVariable");
        foreach (var state in sut.States)
        {
            state.Variables.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void ColorName_IsEditorOnlyMetadata_SoAColoredCategoryStillAppliesItsStatesAtRuntime()
    {
        ComponentSave component = new ComponentSave { Name = "Button", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default" });
        StateSaveCategory category = new StateSaveCategory { Name = "Size", ColorName = "Indigo" };
        StateSave large = new StateSave { Name = "Large" };
        large.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 200f, SetsValue = true });
        category.States.Add(large);
        component.Categories.Add(category);

        GraphicalUiElement gue = new GraphicalUiElement();
        gue.ElementSave = component;
        gue.ApplyState(large);

        gue.Width.ShouldBe(200f);
    }
}
