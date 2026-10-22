using Gum.GueDeriving;
using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using RenderingLibrary;
using RenderingLibrary.Content;
using SkiaSharp.Skottie;
using Svg.Skia;

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
        ["NineSlice"] = () => new NineSliceRuntime(),
        ["ColoredCircle"] = () => new ColoredCircleRuntime(),
        ["Arc"] = () => new ArcRuntime(),
        ["Line"] = () => new LineRuntime(),
        ["RoundedRectangle"] = () => new RoundedRectangleRuntime(),
        ["Svg"] = () => new SvgRuntime(),
        ["LottieAnimation"] = () => new LottieAnimationRuntime(),
    };

    private readonly SystemManagers _systemManagers = new SystemManagers();
    private readonly IContentLoader? _originalContentLoader = LoaderManager.Self.ContentLoader;

    public VariableReferenceTypedSetterTests()
    {
        // The first Initialize in the process wires the Skia renderable factory and property setter.
        _systemManagers.Initialize();
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        // The sweep assigns made-up file names; a missing Svg or Lottie file throws, so those load as nothing.
        LoaderManager.Self.ContentLoader = new MissingDocumentContentLoader(_originalContentLoader);
    }

    public void Dispose()
    {
        // No ElementSaveExtensions.Reset(): it would drop the renderable factory the first Initialize installed.
        ObjectFinder.Self.GumProjectSave = null;
        LoaderManager.Self.ContentLoader = _originalContentLoader;
    }

    private sealed class MissingDocumentContentLoader(IContentLoader? inner) : IContentLoader
    {
        // Only a path that is not a file is treated as missing, so a file that exists and fails to load still throws.
        private static bool IsMissingDocument<T>(string contentName) =>
            (typeof(T) == typeof(Animation) || typeof(T) == typeof(SKSvg)) && !File.Exists(contentName);

        public T? LoadContent<T>(string contentName) =>
            IsMissingDocument<T>(contentName) || inner == null ? default : inner.LoadContent<T>(contentName);

        public T? TryLoadContent<T>(string contentName) =>
            IsMissingDocument<T>(contentName) || inner == null ? default : inner.TryLoadContent<T>(contentName);
    }

    // SpriteRuntime.SourceFile always reads null (it loads a texture or animation chains), so there is no value to follow.
    private static bool IsExcluded(string standard, string variable) => standard == "Sprite" && variable == "SourceFile";

    public static IEnumerable<object[]> Variables()
    {
        // Test discovery runs before any instance: Initialize wires the Skia-only shape default states (Arc,
        // ColoredCircle, RoundedRectangle, Line) that the case list asks the StandardElementsManager for.
        new SystemManagers().Initialize();
        return TypedSetterSweep.Cases(Factories, includeExpressions: false, IsExcluded);
    }

    [Theory]
    [MemberData(nameof(Variables))]
    public void TypedSetAfterCreation_SameComponentReferenceFollows(string standard, string variable, int valueIndex, bool useExpressions)
    {
        useExpressions.ShouldBeFalse("Gum.Expressions is not referenced by this project");
        TypedSetterSweep.Run(Factories, screen => screen.ToGraphicalUiElement(_systemManagers, addToManagers: false), standard, variable, valueIndex);
    }
}
