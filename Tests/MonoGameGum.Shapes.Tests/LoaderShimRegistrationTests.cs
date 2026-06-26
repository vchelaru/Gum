using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Shapes.Tests;

// Issue #3380 — the Apos.Shapes shape runtimes moved from MonoGameGum.GueDeriving to
// Gum.GueDeriving, and the old namespace was preserved as [Obsolete] derived shim subclasses.
// The project loader's registry (AposShapeRuntime.RegisterRuntimeTypes), however, instantiated the
// new *base* type, while already-generated consumer code (FlatRedBall2 / Solitaire) downcasts to
// the derived shim — `Visual.GetGraphicalUiElementByName("StockSlot") as
// global::MonoGameGum.GueDeriving.RoundedRectangleRuntime`. `base as derived` yields null, and the
// first dereference throws NullReferenceException. The loader must instantiate the most-derived
// shim so BOTH the old shim-namespace cast (exact type) AND any newer Gum.GueDeriving cast (the
// shim is-a base) succeed.
public class LoaderShimRegistrationTests
{
    public LoaderShimRegistrationTests()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
    }

#pragma warning disable CS0618 // intentionally pinning the obsolete back-compat shim types
    [Theory]
    [InlineData("Arc", typeof(global::MonoGameGum.GueDeriving.ArcRuntime))]
    [InlineData("ColoredCircle", typeof(global::MonoGameGum.GueDeriving.ColoredCircleRuntime))]
    [InlineData("Line", typeof(global::MonoGameGum.GueDeriving.LineRuntime))]
    [InlineData("RoundedRectangle", typeof(global::MonoGameGum.GueDeriving.RoundedRectangleRuntime))]
    public void CreateGueForElement_ForCollapsedShapeBaseType_ProducesObsoleteShimSubclass(
        string baseTypeName, Type expectedShimType)
    {
        StandardElementSave element = new StandardElementSave { Name = baseTypeName };

        GraphicalUiElement created = ElementSaveExtensions.CreateGueForElement(element);

        created.ShouldBeAssignableTo(expectedShimType);
    }
#pragma warning restore CS0618
}
