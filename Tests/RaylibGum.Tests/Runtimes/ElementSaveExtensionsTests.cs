using Gum.DataTypes;
using Gum.GueDeriving;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Reflection;

namespace RaylibGum.Tests.Runtimes;

// Issue #3574 — SystemManagers.RegisterComponentRuntimeInstantiations is the canonical
// primary-registry hook on the raylib backend (mirrors MonoGame's #2925 fix). The Rectangle and
// Polygon registrations were previously commented out with no test pinning them, so a
// .gumx-project-loaded "Rectangle"/"Polygon" standard element silently fell back to a plain
// GraphicalUiElement instead of RectangleRuntime/PolygonRuntime. Method is private, so we invoke
// it via reflection — same pattern MonoGameGum.Tests.Runtimes.ElementSaveExtensionsTests uses.
public class ElementSaveExtensionsTests
{
    [Fact]
    public void RegisterComponentRuntimeInstantiations_ShouldRegisterPolygonRuntime_ForPolygonBaseType()
    {
        InvokeRegisterComponentRuntimeInstantiations();

        var element = new StandardElementSave { Name = "Polygon" };

        var created = ElementSaveExtensions.CreateGueForElement(element);

        created.ShouldBeAssignableTo<PolygonRuntime>();
    }

    [Fact]
    public void RegisterComponentRuntimeInstantiations_ShouldRegisterRectangleRuntime_ForRectangleBaseType()
    {
        InvokeRegisterComponentRuntimeInstantiations();

        var element = new StandardElementSave { Name = "Rectangle" };

        var created = ElementSaveExtensions.CreateGueForElement(element);

        created.ShouldBeAssignableTo<RectangleRuntime>();
    }

    private static void InvokeRegisterComponentRuntimeInstantiations()
    {
        var managers = new SystemManagers();
        var method = typeof(SystemManagers).GetMethod(
            "RegisterComponentRuntimeInstantiations",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull("RegisterComponentRuntimeInstantiations must exist on SystemManagers.");
        method!.Invoke(managers, parameters: null);
    }
}
