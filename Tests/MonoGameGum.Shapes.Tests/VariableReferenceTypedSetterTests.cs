using Gum;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;

namespace MonoGameGum.Shapes.Tests;

/// <summary>
/// The Apos.Shapes counterpart of MonoGameGum.Tests' VariableReferenceTypedSetterTests: every typed property
/// of the Apos-backed shape runtimes that is also a Gum variable, and every variable it changes as a side
/// effect (Color and Red, Red and Red1), updates a same-component reference that reads it (#6410).
/// Gum.Expressions is not referenced here, so only the plain-lookup path runs.
/// </summary>
public class VariableReferenceTypedSetterTests : RegistrationIsolatedTest
{
    private static readonly Dictionary<string, Func<GraphicalUiElement>> Factories = new()
    {
        ["Arc"] = () => new ArcRuntime(),
#pragma warning disable CS0618 // still standard elements until they are removed
        ["ColoredCircle"] = () => new ColoredCircleRuntime(),
        ["Line"] = () => new LineRuntime(),
        ["RoundedRectangle"] = () => new RoundedRectangleRuntime(),
#pragma warning restore CS0618
        ["Svg"] = () => new SvgRuntime(),
    };

    public VariableReferenceTypedSetterTests()
    {
        StandardElementsManager.Self.Initialize();
        GumRuntime.ElementSaveExtensions.CustomEvaluateExpression = null;
        GumRuntime.ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public static IEnumerable<object[]> Variables() => TypedSetterSweep.Cases(Factories, includeExpressions: false);

    [Theory]
    [MemberData(nameof(Variables))]
    public void TypedSetAfterCreation_SameComponentReferenceFollows(string standard, string variable, int valueIndex, bool useExpressions)
    {
        Assert.False(useExpressions, "Gum.Expressions is not referenced by this project");
        TypedSetterSweep.Run(Factories, screen => Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(screen), standard, variable, valueIndex);
    }
}
