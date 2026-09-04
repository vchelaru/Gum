using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Localization;
using Gum.Wireframe;
using RaylibGum;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;

namespace RaylibGum.Tests;

/// <summary>
/// Exercises the real <see cref="GumService.Initialize(DefaultVisualsVersion)"/> path
/// on Raylib. Most other tests bypass this via <c>InitializeForTesting</c>, so
/// ordering bugs inside <c>InitializeInternal</c> (e.g. creating PopupRoot/ModalRoot
/// before the <c>GraphicalUiElement.AddRenderableToManagers</c> delegate is wired up)
/// are invisible to them.
/// </summary>
public class GumServiceInitializeTests
{
    [Fact]
    public void IGumServiceInitialize_OnRaylib_InitializesService()
    {
        GumService.Default.Uninitialize();

        try
        {
            IGumService service = GumService.Default;
            service.Initialize();

            service.IsInitialized.ShouldBeTrue();
            IGumService.Default.ShouldBeSameAs(service);
        }
        finally
        {
            GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void Initialize_ShouldAssignDefaultLocalizationService()
    {
        // Tear down the assembly-wide state so we can observe a cold init, and clear the
        // static LocalizationService so a leftover instance from another test can't mask
        // Initialize() failing to assign one (#4576 - GumUI.LocalizationService returned
        // null on raylib because SystemManagers.Initialize() never wired it up).
        // Qualified as Gum.GumService (the modern, non-obsolete class) to stay warning-free -
        // same reasoning as TestAssemblyInitialize.ApplyDefaultTestState.
        Gum.GumService.Default.Uninitialize();
        CustomSetPropertyOnRenderable.LocalizationService = null;

        try
        {
            Gum.GumService.Default.Initialize(DefaultVisualsVersion.V3);

            CustomSetPropertyOnRenderable.LocalizationService.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
            Gum.GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void Initialize_ShouldAssignThrowExceptionsForMissingFilesDelegate()
    {
        // Same shape as Initialize_ShouldAssignDefaultLocalizationService above: the docs
        // (files-and-fonts/font-strategies.md) tell users to call
        // GraphicalUiElement.ThrowExceptionsForMissingFiles(textRuntime) directly, but on raylib
        // SystemManagers.Initialize() never assigned the delegate, so calling it NREs.
        Gum.GumService.Default.Uninitialize();
        GraphicalUiElement.ThrowExceptionsForMissingFiles = null;

        try
        {
            Gum.GumService.Default.Initialize(DefaultVisualsVersion.V3);

            GraphicalUiElement.ThrowExceptionsForMissingFiles.ShouldNotBeNull();
        }
        finally
        {
            GraphicalUiElement.ThrowExceptionsForMissingFiles = null;
            Gum.GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void Initialize_ShouldSetMissingFileBehaviorToThrowException()
    {
        // Same shape as Initialize_ShouldAssignThrowExceptionsForMissingFilesDelegate above:
        // MonoGame/KNI/FNA's SystemManagers.Initialize() sets this to ThrowException, but raylib's
        // never did, leaving raylib on the shared ConsumeSilently default (#4577).
        Gum.GumService.Default.Uninitialize();
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;

        try
        {
            Gum.GumService.Default.Initialize(DefaultVisualsVersion.V3);

            GraphicalUiElement.MissingFileBehavior.ShouldBe(MissingFileBehavior.ThrowException);
        }
        finally
        {
            Gum.GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void Initialize_RegistersRootPopupRootAndModalRootInMainLayer()
    {
        // Tear down the assembly-wide state so we can observe a cold init.
        GumService.Default.Uninitialize();

        try
        {
            GumService.Default.Initialize(DefaultVisualsVersion.V3);

            var renderables = SystemManagers.Default.Renderer.MainLayer.Renderables;

            renderables.ShouldContain(
                GumService.Default.Root.RenderableComponent,
                "Root should be registered in the MainLayer so it renders.");
            renderables.ShouldContain(
                GumService.Default.PopupRoot.RenderableComponent,
                "PopupRoot should be registered in the MainLayer so popups render.");
            renderables.ShouldContain(
                GumService.Default.ModalRoot.RenderableComponent,
                "ModalRoot should be registered in the MainLayer so modals render.");
        }
        finally
        {
            GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }
}
