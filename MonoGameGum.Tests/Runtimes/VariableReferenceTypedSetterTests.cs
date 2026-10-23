using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;
using GumRuntime;
using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Every typed property that is also a Gum variable updates a same-component reference that reads it:
/// a component with an instance <c>Shadow</c> holding the row <c>P = P</c>, and <c>label.P = value</c>
/// in code, must reach <c>Shadow.P</c>, for every enum value and a few float, int and bool values. The
/// variables come from the standard elements' defaults, so a new variable with a typed setter that does
/// not report is caught here (#6393). See <see cref="TypedSetterSweep"/>.
/// </summary>
public class VariableReferenceTypedSetterTests : BaseTestClass
{
    private static readonly Dictionary<string, Func<GraphicalUiElement>> Factories = new()
    {
        ["Text"] = () => new TextRuntime(),
        ["Container"] = () => new ContainerRuntime(),
        ["Sprite"] = () => new SpriteRuntime(),
        ["NineSlice"] = () => new NineSliceRuntime(),
#pragma warning disable CS0618 // still a standard element until it is removed
        ["ColoredRectangle"] = () => new ColoredRectangleRuntime(),
#pragma warning restore CS0618
        ["Rectangle"] = () => new RectangleRuntime(),
        ["Circle"] = () => new CircleRuntime(),
        ["Polygon"] = () => new PolygonRuntime(),
    };

    public VariableReferenceTypedSetterTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public static IEnumerable<object[]> Variables() => TypedSetterSweep.Cases(Factories, includeExpressions: true);

    [Theory]
    [MemberData(nameof(Variables))]
    public void TypedSetAfterCreation_SameComponentReferenceFollows(string standard, string variable, int valueIndex, bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }

        TypedSetterSweep.Run(Factories, screen => screen.ToGraphicalUiElement(), standard, variable, valueIndex);
    }

    // A channel write goes through its color, which reports the channels too; the reference still applies once.
    [Theory]
    [InlineData("Circle", "FillRed", "FillRed")]
    [InlineData("Circle", "StrokeRed", "StrokeRed")]
    [InlineData("Rectangle", "FillRed", "FillRed")]
    [InlineData("Circle", "FillColor", "FillRed")]
    public void TypedSet_ChannelReference_AppliesOnce(string standard, string variable, string referenced)
    {
        object value = variable.EndsWith("Color") ? new Microsoft.Xna.Framework.Color(1, 2, 3, 4) : (object)7;

        int applies = TypedSetterSweep.CountReferenceApplies(Factories, screen => screen.ToGraphicalUiElement(), standard, variable, value, referenced);

        applies.ShouldBe(1);
    }
}
