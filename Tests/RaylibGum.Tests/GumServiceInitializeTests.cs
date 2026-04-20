using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RaylibGum;
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
    public void Initialize_RegistersRootPopupRootAndModalRootInMainLayer()
    {
        // Tear down the assembly-wide state so we can observe a cold init.
        GumService.Default.Uninitialize();

        try
        {
            GumService.Default.Initialize(DefaultVisualsVersion.V2);

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
