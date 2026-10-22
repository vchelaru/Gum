using Gum.GueDeriving;
using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;

namespace SkiaGum.Tests.Runtimes;

/// <summary>
/// The Skia counterpart of MonoGameGum.Tests' VariableReferenceTypedSetterTests: every typed property that
/// is also a Gum variable updates a same-component reference that reads it (#6393). Gum.Expressions is
/// not referenced here, so only the plain-lookup path runs.
/// </summary>
public class VariableReferenceTypedSetterTests : IDisposable
{
    private static readonly Dictionary<string, Func<GraphicalUiElement>> Factories = new()
    {
        ["Text"] = () => new TextRuntime(),
        ["Container"] = () => new ContainerRuntime(),
        ["Sprite"] = () => new SpriteRuntime(),
        ["Rectangle"] = () => new RectangleRuntime(),
        ["Circle"] = () => new CircleRuntime(),
        ["Polygon"] = () => new PolygonRuntime(),
    };

    private readonly SystemManagers _systemManagers = new SystemManagers();

    public VariableReferenceTypedSetterTests()
    {
        // The first Initialize in the process wires the Skia renderable factory and property setter.
        _systemManagers.Initialize();
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public void Dispose()
    {
        // No ElementSaveExtensions.Reset(): it would drop the renderable factory the first Initialize installed.
        ObjectFinder.Self.GumProjectSave = null;
    }

    // On Skia, SetProperty of a Text's HasDropshadow, Dropshadow* and TextOverflowVerticalMode writes the Text
    // renderable and never the TextRuntime's own fields, so a reference cannot copy them whatever the
    // setters report (#6403).
    public static IEnumerable<object[]> Variables() => TypedSetterSweep.Cases(Factories, includeExpressions: false,
        isExcluded: (standard, variable) => standard == "Text" && (variable.Contains("Dropshadow") || variable == "TextOverflowVerticalMode"));

    [Theory]
    [MemberData(nameof(Variables))]
    public void TypedSetAfterCreation_SameComponentReferenceFollows(string standard, string variable, int valueIndex, bool useExpressions)
    {
        TypedSetterSweep.Run(Factories, screen => screen.ToGraphicalUiElement(_systemManagers, addToManagers: false), standard, variable, valueIndex);
    }
}
