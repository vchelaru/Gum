using Gum.DataTypes;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

// Issue #3380 (Skia parallel) — the standard runtime types moved from SkiaGum.GueDeriving to
// Gum.GueDeriving, and the old namespace was preserved as [Obsolete] derived shim subclasses.
// SystemManagers.RegisterComponentRuntimeInstantiations, however, instantiated the new *base*
// types (it has `using Gum.GueDeriving;`), while already-generated consumer code downcasts to the
// deprecated shim namespace (`... as SkiaGum.GueDeriving.ContainerRuntime`). `base as derived`
// yields null and the first dereference throws. The loader must instantiate the most-derived shim
// so BOTH the old shim-namespace cast (exact type) AND any newer Gum.GueDeriving cast (the shim
// is-a base) succeed. Mirrors the MonoGame fixes in RenderingLibrary.SystemManagers and
// AposShapeRuntime. "Rectangle" is intentionally excluded: it has no SkiaGum.GueDeriving shim
// (RectangleRuntime is a new-only type), so its base instantiation is already correct.
public class LoaderShimRegistrationTests
{
    public LoaderShimRegistrationTests()
    {
        // RegisterComponentRuntimeInstantiations is private and normally runs once behind
        // SystemManagers.Initialize's HasInitializedGlobal gate; invoke it directly via reflection
        // (same pattern as MonoGameGum.Tests' ElementSaveExtensionsTests) so registration is
        // deterministic and needs no GraphicsDevice.
        SystemManagers managers = new SystemManagers();
        MethodInfo? method = typeof(SystemManagers).GetMethod(
            "RegisterComponentRuntimeInstantiations",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull("RegisterComponentRuntimeInstantiations must exist on SystemManagers.");
        method!.Invoke(managers, parameters: null);
    }

#pragma warning disable CS0618 // intentionally pinning the obsolete back-compat shim types
    [Theory]
    [InlineData("Arc", typeof(global::SkiaGum.GueDeriving.ArcRuntime))]
    [InlineData("Circle", typeof(global::SkiaGum.GueDeriving.CircleRuntime))]
    [InlineData("ColoredCircle", typeof(global::SkiaGum.GueDeriving.ColoredCircleRuntime))]
    [InlineData("Container", typeof(global::SkiaGum.GueDeriving.ContainerRuntime))]
    [InlineData("Line", typeof(global::SkiaGum.GueDeriving.LineRuntime))]
    [InlineData("Polygon", typeof(global::SkiaGum.GueDeriving.PolygonRuntime))]
    [InlineData("Sprite", typeof(global::SkiaGum.GueDeriving.SpriteRuntime))]
    [InlineData("Text", typeof(global::SkiaGum.GueDeriving.TextRuntime))]
    public void CreateGueForElement_ForShimmedBaseType_ProducesObsoleteShimSubclass(
        string baseTypeName, Type expectedShimType)
    {
        StandardElementSave element = new StandardElementSave { Name = baseTypeName };

        GraphicalUiElement created = ElementSaveExtensions.CreateGueForElement(element);

        created.ShouldBeAssignableTo(expectedShimType);
    }
#pragma warning restore CS0618
}
