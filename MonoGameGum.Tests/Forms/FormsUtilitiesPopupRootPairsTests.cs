using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

// Covers issue #3585: FormsUtilities.Update's modal/popup special-casing extended to FrameworkElement.AdditionalPopupRootPairs.
public class FormsUtilitiesPopupRootPairsTests : BaseTestClass
{
    [Fact]
    public void Update_ModalExclusivity_AppliesToAdditionalModalRoot()
    {
        ContainerRuntime customPopupRoot = new();
        ContainerRuntime customModalRoot = new();
        customPopupRoot.AddToManagers(SystemManagers.Default);
        customModalRoot.AddToManagers(SystemManagers.Default);
        FrameworkElement.AdditionalPopupRootPairs.Add((customPopupRoot, customModalRoot));

        Button modalChild = new();
        customModalRoot.Children.Add(modalChild.Visual);

        Button normalRootChild = new();
        normalRootChild.AddToRoot();

        customModalRoot.X = 999;
        customModalRoot.Width = 1;

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        customModalRoot.X.ShouldBe(0);
        customModalRoot.Width.ShouldBe(GraphicalUiElement.CanvasWidth);

        // Only the additional modal root's own content should be dispatched to — everything
        // else, including the normal root's content, is excluded while it has children.
        FormsUtilities.LastEventRoots.ShouldContain(modalChild.Visual);
        FormsUtilities.LastEventRoots.ShouldNotContain(normalRootChild.Visual);
    }

    [Fact]
    public void Update_ModalExclusivity_FallsThroughToAdditionalModalRoot_WhenGlobalModalRootHasNoVisibleChildren()
    {
        ContainerRuntime customPopupRoot = new();
        ContainerRuntime customModalRoot = new();
        customPopupRoot.AddToManagers(SystemManagers.Default);
        customModalRoot.AddToManagers(SystemManagers.Default);
        FrameworkElement.AdditionalPopupRootPairs.Add((customPopupRoot, customModalRoot));

        Button invisibleGlobalModalChild = new();
        invisibleGlobalModalChild.Visual.Visible = false;
        FrameworkElement.ModalRoot.Children.Add(invisibleGlobalModalChild.Visual);

        Button additionalModalChild = new();
        customModalRoot.Children.Add(additionalModalChild.Visual);

        Button normalRootChild = new();
        normalRootChild.AddToRoot();

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        // The global ModalRoot has children but none visible, so it isn't a candidate winner —
        // exclusivity falls through to the additional modal root instead of to the normal root.
        FormsUtilities.LastEventRoots.ShouldContain(additionalModalChild.Visual);
        FormsUtilities.LastEventRoots.ShouldNotContain(normalRootChild.Visual);
    }

    [Fact]
    public void Update_ModalExclusivity_GlobalModalRootWinsOverAdditionalModalRoot_WhenBothHaveChildren()
    {
        ContainerRuntime customPopupRoot = new();
        ContainerRuntime customModalRoot = new();
        customPopupRoot.AddToManagers(SystemManagers.Default);
        customModalRoot.AddToManagers(SystemManagers.Default);
        FrameworkElement.AdditionalPopupRootPairs.Add((customPopupRoot, customModalRoot));

        Button additionalModalChild = new();
        customModalRoot.Children.Add(additionalModalChild.Visual);

        Button globalModalChild = new();
        FrameworkElement.ModalRoot.Children.Add(globalModalChild.Visual);

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        // The global ModalRoot is checked first, so it keeps input exclusivity over an additional
        // modal root even when both have children in the same frame.
        FormsUtilities.LastEventRoots.ShouldContain(globalModalChild.Visual);
        FormsUtilities.LastEventRoots.ShouldNotContain(additionalModalChild.Visual);
    }

    [Fact]
    public void Update_SizesToCanvas_ForAdditionalPopupRoot()
    {
        ContainerRuntime customPopupRoot = new();
        ContainerRuntime customModalRoot = new();
        customPopupRoot.AddToManagers(SystemManagers.Default);
        customModalRoot.AddToManagers(SystemManagers.Default);
        FrameworkElement.AdditionalPopupRootPairs.Add((customPopupRoot, customModalRoot));

        Button popupChild = new();
        customPopupRoot.Children.Add(popupChild.Visual);

        customPopupRoot.X = 999;
        customPopupRoot.Y = 999;
        customPopupRoot.Width = 1;
        customPopupRoot.Height = 1;

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        customPopupRoot.X.ShouldBe(0);
        customPopupRoot.Y.ShouldBe(0);
        customPopupRoot.Width.ShouldBe(GraphicalUiElement.CanvasWidth);
        customPopupRoot.Height.ShouldBe(GraphicalUiElement.CanvasHeight);
        FormsUtilities.LastEventRoots.ShouldContain(popupChild.Visual);
    }
}
