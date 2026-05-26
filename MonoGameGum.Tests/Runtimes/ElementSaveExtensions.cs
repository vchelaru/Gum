using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class ElementSaveExtensionsTests : IDisposable
{
    public void Dispose()
    {
        ElementSaveExtensions.Reset();
    }



    [Fact]
    public void CreateGueForElement_ShouldCreateGraphicalUiElement_IfNoTypesAreRegistered()
    {
        var element = new ComponentSave();
        element.Name = "Test1";
        var gue = ElementSaveExtensions.CreateGueForElement(element);
        gue.GetType().ShouldBe(typeof(GraphicalUiElement));
    }

    [Fact]
    public void CreateGueForElement_ShouldCreateGraphicalUiElement_IfGenericTypeIsSpecified()
    {
        ElementSaveExtensions.RegisterGueInstantiation("GenericGueInstantiation", () =>
        {
            return new GraphicalUiElement(new InvisibleRenderable()) { Name = "Registered Gue Type for generic" };

        });

        var elementSave = new ComponentSave { Name = "GenericGueInstantiation" };


        var runtime = ElementSaveExtensions.CreateGueForElement(elementSave, genericType: "Unused");

        runtime.Name.ShouldBe("Registered Gue Type for generic");
    }


    [Fact]
    public void RegisterGueInstantiation_ShouldCreateGue_ThroughDelegate()
    {
        ElementSaveExtensions.RegisterGueInstantiation("GueInstantiation", () =>
        {
            return new GraphicalUiElement(new InvisibleRenderable()) { Name = "Registered Gue Type" };
        });

        var element = new ComponentSave { Name = "GueInstantiation" };

        var runtime = ElementSaveExtensions.CreateGueForElement(element);
        runtime.Name.ShouldBe("Registered Gue Type");
    }

    // Issue #2925 — SystemManagers.RegisterComponentRuntimeInstantiations is the canonical
    // primary-registry hook for the MonoGame/KNI/FNA backends; everything the Apos.Shapes
    // package overrides must be reached through it, otherwise CircleRuntime is bypassed in
    // favor of FallbackRenderableFactory's LineCircle. Method is private, so we invoke it
    // via reflection — same pattern other tests use for SystemManagers internals.
    [Fact]
    public void RegisterComponentRuntimeInstantiations_ShouldRegisterCircleRuntime_ForCircleBaseType()
    {
        InvokeRegisterComponentRuntimeInstantiations();

        var element = new StandardElementSave { Name = "Circle" };

        var created = ElementSaveExtensions.CreateGueForElement(element);

        // RegisterComponentRuntimeInstantiations constructs the obsolete-shim subclass
        // (MonoGameGum.GueDeriving.CircleRuntime) during the deprecation window — see the
        // file-level comment on SystemManagers.cs. Use ShouldBeAssignableTo so the test
        // pins the base contract without coupling to the shim type.
        created.ShouldBeAssignableTo<CircleRuntime>();
    }

    // Symmetric coverage for Rectangle — the registration already exists, but until now no
    // test pinned it, which is how Circle's omission slipped past review.
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

    [Fact]
    public void RegisterGueInstantiation_ShouldConsiderInheritance()
    {
        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () => new TextRuntime());

        var element = new ComponentSave { Name = "Label" };
        element.BaseType = "Text";

        var gumProject = new GumProjectSave();

        ObjectFinder.Self.GumProjectSave = gumProject;

        gumProject.Components.Add(element);
        gumProject.StandardElements.Add(new StandardElementSave
        {
            Name = "Text"
        });

        var createdElement = ElementSaveExtensions.CreateGueForElement(element);

        createdElement.ShouldBeOfType<TextRuntime>();
    }


}
