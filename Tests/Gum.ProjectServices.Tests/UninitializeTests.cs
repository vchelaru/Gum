using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Content;
using Shouldly;
using System;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests covering the state-reset behavior of subsystems touched by
/// <c>GumService.Uninitialize()</c> that do not require MonoGame/GPU.
/// GPU-dependent subsystems (Renderer, FormsUtilities, GumService itself)
/// are tested in MonoGameGum.Tests.V2.
/// </summary>
public class UninitializeTests : IDisposable
{
    public void Dispose()
    {
        // Restore shared static state modified by any test in this class.
        ObjectFinder.Self.GumProjectSave = null;
        ElementSaveExtensions.ClearRegistrations();
    }

    // -------------------------------------------------------------------------
    // ElementSaveExtensions.ClearRegistrations
    // -------------------------------------------------------------------------

    [Fact]
    public void ClearRegistrations_ClearsCustomCreateGraphicalComponentFunc()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (_, _) => null!;

        ElementSaveExtensions.ClearRegistrations();

        ElementSaveExtensions.CustomCreateGraphicalComponentFunc.ShouldBeNull();
    }

    [Fact]
    public void ClearRegistrations_ClearsCustomEvaluateExpression()
    {
        ElementSaveExtensions.CustomEvaluateExpression = (_, _, _) => null!;

        ElementSaveExtensions.ClearRegistrations();

        ElementSaveExtensions.CustomEvaluateExpression.ShouldBeNull();
    }

    [Fact]
    public void ClearRegistrations_ClearsRegisteredFuncSoElementFallsBackToBaseGue()
    {
        // RegisterGueInstantiation stores into mElementToGueTypeFuncs.
        // After clearing, CreateGueForElement must no longer call the registered func.
        bool wasCalled = false;
        ElementSaveExtensions.RegisterGueInstantiation<GraphicalUiElement>("TestElement", () =>
        {
            wasCalled = true;
            return new GraphicalUiElement();
        });

        ElementSaveExtensions.ClearRegistrations();

        // CreateGueForElement falls back to new GraphicalUiElement() when nothing is registered.
        ElementSave elementSave = new ComponentSave { Name = "TestElement" };
        GraphicalUiElement result = ElementSaveExtensions.CreateGueForElement(elementSave);

        wasCalled.ShouldBeFalse();
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ClearRegistrations_ClearsRegisteredTypeSoElementFallsBackToBaseGue()
    {
        // Register a custom subtype. After clearing, CreateGueForElement must NOT
        // instantiate it — it should fall back to the plain GraphicalUiElement base.
        ElementSaveExtensions.RegisterGueInstantiationType("TypedElement", typeof(CustomTestGue));

        ElementSaveExtensions.ClearRegistrations();

        ElementSave elementSave = new ComponentSave { Name = "TypedElement" };
        GraphicalUiElement result = ElementSaveExtensions.CreateGueForElement(elementSave);

        result.ShouldNotBeNull();
        // Must NOT be the registered custom type — the registration was cleared.
        result.GetType().ShouldBe(typeof(GraphicalUiElement));
    }

    [Fact]
    public void RegisterDefaultInstantiationType_ThenClearRegistrations_NullsTemplateFunc()
    {
        // TemplateFunc is private; we verify its effect indirectly: after clearing,
        // CreateGueForElement should return a plain GraphicalUiElement, not the custom type.
        bool templateWasCalled = false;
        ElementSaveExtensions.RegisterDefaultInstantiationType(() =>
        {
            templateWasCalled = true;
            return new GraphicalUiElement();
        });

        ElementSaveExtensions.ClearRegistrations();

        ElementSave elementSave = new ComponentSave { Name = "AnyElement" };
        ElementSaveExtensions.CreateGueForElement(elementSave);

        templateWasCalled.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // LoaderManager.DisposeAndClear
    // -------------------------------------------------------------------------

    [Fact]
    public void DisposeAndClear_ClearsCachedDisposables()
    {
        LoaderManager loaderManager = LoaderManager.Self;
        loaderManager.AddDisposable("test-key", new DisposableSpy(), LoaderManager.ExistingContentBehavior.Replace);

        loaderManager.DisposeAndClear();

        loaderManager.GetDisposable("test-key").ShouldBeNull();
    }

    [Fact]
    public void DisposeAndClear_DisposesEachCachedEntry()
    {
        LoaderManager loaderManager = LoaderManager.Self;
        DisposableSpy spy = new DisposableSpy();
        loaderManager.AddDisposable("spy-key", spy, LoaderManager.ExistingContentBehavior.Replace);

        loaderManager.DisposeAndClear();

        spy.WasDisposed.ShouldBeTrue();
    }

    [Fact]
    public void DisposeAndClear_ClearsContentFromTryGetCachedDisposable()
    {
        LoaderManager loaderManager = LoaderManager.Self;
        DisposableSpy spy = new DisposableSpy();
        loaderManager.AddDisposable("cached-content-key", spy, LoaderManager.ExistingContentBehavior.Replace);
        loaderManager.TryGetCachedDisposable<DisposableSpy>("cached-content-key").ShouldNotBeNull();

        loaderManager.DisposeAndClear();

        loaderManager.TryGetCachedDisposable<DisposableSpy>("cached-content-key").ShouldBeNull();
    }

    [Fact]
    public void DisposeAndClear_CalledTwice_DoesNotThrow()
    {
        LoaderManager loaderManager = LoaderManager.Self;

        loaderManager.DisposeAndClear();

        // Second call should be a no-op, not throw.
        Should.NotThrow(() => loaderManager.DisposeAndClear());
    }

    // -------------------------------------------------------------------------
    // ObjectFinder.GumProjectSave = null
    // -------------------------------------------------------------------------

    [Fact]
    public void GumProjectSave_SetToNull_ComponentLookupReturnsNull()
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(new ComponentSave { Name = "Button" });
        ObjectFinder.Self.GumProjectSave = project;
        // Verify it was found before the test.
        ObjectFinder.Self.GetElementSave("Button").ShouldNotBeNull();

        ObjectFinder.Self.GumProjectSave = null;

        ObjectFinder.Self.GetElementSave("Button").ShouldBeNull();
    }

    [Fact]
    public void GumProjectSave_SetToNull_ScreenLookupReturnsNull()
    {
        GumProjectSave project = new GumProjectSave();
        project.Screens.Add(new ScreenSave { Name = "MainMenu" });
        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.GetElementSave("MainMenu").ShouldNotBeNull();

        ObjectFinder.Self.GumProjectSave = null;

        ObjectFinder.Self.GetElementSave("MainMenu").ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Helper types
    // -------------------------------------------------------------------------

    /// <summary>
    /// A concrete subclass of <see cref="GraphicalUiElement"/> used to verify that
    /// the registered type is no longer instantiated after <c>ClearRegistrations</c>.
    /// </summary>
    private class CustomTestGue : GraphicalUiElement
    {
    }

    private sealed class DisposableSpy : IDisposable
    {
        public bool WasDisposed { get; private set; }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}
