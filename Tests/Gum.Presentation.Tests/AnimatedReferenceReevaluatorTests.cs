using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Shouldly;
using StateAnimationPlugin;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins which reference rows <see cref="AnimatedReferenceReevaluator"/> keeps for a preview tick: the rows
/// that read an animated variable, so the tick's cost follows what the animation touches.
/// </summary>
public class AnimatedReferenceReevaluatorTests : BaseTestClass
{
    public AnimatedReferenceReevaluatorTests()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();
    }

    [Fact]
    public void Apply_KeepsARowReadingAnAbsoluteValue_EvenWhenNoAnimatedNameAppearsInIt()
    {
        // Absolute* values come from the live layout, which an animated size or position changes.
        (ComponentSave element, StateSave state) = CreateElement("Alpha = AbsoluteWidth");

        AnimatedReferenceReevaluator.Apply(element, state, new HashSet<string> { "Rotation" },
            animatesUnknownVariables: false, liveRoot: null);

        RemainingRows(state).ShouldBe(new[] { "Alpha = AbsoluteWidth" });
    }

    [Fact]
    public void Apply_KeepsEveryRow_WhenTheAnimationCanSetUnknownVariables()
    {
        // A sub-animation sets variables the keyframe states don't list, so no row can be ruled out.
        (ComponentSave element, StateSave state) = CreateElement("Alpha = Rotation + 1");

        AnimatedReferenceReevaluator.Apply(element, state, new HashSet<string>(),
            animatesUnknownVariables: true, liveRoot: null);

        RemainingRows(state).ShouldBe(new[] { "Alpha = Rotation + 1" });
    }

    [Fact]
    public void Apply_DropsARowReadingAMemberOfAnotherInstance_WhoseNameMatchesAnAnimatedVariable()
    {
        // "Other.Width" reads Other's Width, not the element's own animated Width.
        (ComponentSave element, StateSave state) = CreateElement("Alpha = Other.Width");

        AnimatedReferenceReevaluator.Apply(element, state, new HashSet<string> { "Width" },
            animatesUnknownVariables: false, liveRoot: null);

        RemainingRows(state).ShouldBeEmpty();
    }

    [Fact]
    public void Apply_KeepsARowReadingAnInstanceVariable_WrittenWithTheElementQualifier()
    {
        (ComponentSave element, StateSave state) = CreateElement("Alpha = Components/Foo.Circle.Width");

        AnimatedReferenceReevaluator.Apply(element, state, new HashSet<string> { "Circle.Width" },
            animatesUnknownVariables: false, liveRoot: null);

        RemainingRows(state).ShouldBe(new[] { "Alpha = Components/Foo.Circle.Width" });
    }

    private static (ComponentSave, StateSave) CreateElement(string referenceRow)
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        ObjectFinder.Self.GumProjectSave = project;

        state.VariableLists.Add(new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences",
            Value = { referenceRow }
        });
        return (element, state);
    }

    private static List<string> RemainingRows(StateSave state)
    {
        List<string> rows = new List<string>();
        foreach (VariableListSave list in state.VariableLists)
        {
            if (list.GetRootName() == "VariableReferences")
            {
                foreach (object? row in list.ValueAsIList)
                {
                    rows.Add((string)row!);
                }
            }
        }
        return rows;
    }
}
