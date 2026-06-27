using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using StateAnimationPlugin;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Pins <see cref="MainStateAnimationPlugin.BuildAvailableStateNames"/>: the list that backs the
/// StateView state ComboBox must keep any state name a keyframe still references even after that
/// state is renamed or deleted. Otherwise the two-way-bound ComboBox nulls the selected keyframe's
/// StateName when its value leaves the list, silently turning a broken state keyframe into an event
/// keyframe (issue #3386).
/// </summary>
public class AvailableStateNamesTests : BaseTestClass
{
    [Fact]
    public void BuildAvailableStateNames_IncludesElementStatesAndCategories()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        element.States.Add(new StateSave { Name = "Default" });
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = "Walk" });
        element.Categories.Add(category);

        List<string> result = MainStateAnimationPlugin.BuildAvailableStateNames(element, new List<string>());

        result.ShouldContain("Default");
        result.ShouldContain("Cat/Walk");
    }

    [Fact]
    public void BuildAvailableStateNames_KeepsReferencedState_WhenItNoLongerExists()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = "Walk" });
        element.Categories.Add(category);

        // A keyframe still points at "Cat/Idle" after that state was renamed to "Cat/Walk".
        List<string> result = MainStateAnimationPlugin.BuildAvailableStateNames(element, new[] { "Cat/Idle" });

        result.ShouldContain("Cat/Idle");
    }
}
