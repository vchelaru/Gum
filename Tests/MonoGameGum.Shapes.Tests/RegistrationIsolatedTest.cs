using Gum.GueDeriving;
using Gum.Renderables;
using MonoGameGum.TestsCommon;

namespace MonoGameGum.Shapes.Tests;

/// <summary>
/// Base for a test class that registers or clears process-wide state. xUnit runs classes in a random
/// order, so each such class restores on the way out what it changed: element-type registrations and
/// hooks (<see cref="StaticRegistrationScope"/>), <c>IsSvgRuntimeEnabled</c> and the shape renderer's
/// initialized flag.
/// </summary>
public abstract class RegistrationIsolatedTest : IDisposable
{
    private readonly StaticRegistrationScope _registrations = new();
    private readonly bool _svgRuntimeEnabled = AposShapeRuntime.IsSvgRuntimeEnabled;
    private readonly bool _shapeRendererInitialized = ShapeRenderer.Self.IsInitialized;

    public virtual void Dispose()
    {
        _registrations.Dispose();
        AposShapeRuntime.IsSvgRuntimeEnabled = _svgRuntimeEnabled;
        ShapeRenderer.Self.SetIsInitializedForTesting(_shapeRendererInitialized);
    }
}
