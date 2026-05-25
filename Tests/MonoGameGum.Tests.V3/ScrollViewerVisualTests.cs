using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Gum.GueDeriving;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ScrollViewerVisualTests
{
    private class ThemedScrollViewerVisual : ScrollViewerVisual
    {
        public int RefreshClipContainerMarginsCallCount;
        public int RefreshScrollBarLengthsCallCount;
        public int OnAfterInitializeCallCount;

        protected override void RefreshClipContainerMargins()
        {
            RefreshClipContainerMarginsCallCount++;
            base.RefreshClipContainerMargins();
        }

        protected override void RefreshScrollBarLengths()
        {
            RefreshScrollBarLengthsCallCount++;
            base.RefreshScrollBarLengths();
        }

        protected override void OnAfterInitialize()
        {
            OnAfterInitializeCallCount++;
            base.OnAfterInitialize();
        }
    }

    [Fact]
    public void RefreshClipContainerMargins_IsVirtual_AndInvokedDuringConstruction()
    {
        ThemedScrollViewerVisual themed = new();

        themed.RefreshClipContainerMarginsCallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RefreshScrollBarLengths_IsVirtual_AndInvokedDuringConstruction()
    {
        ThemedScrollViewerVisual themed = new();

        themed.RefreshScrollBarLengthsCallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OnAfterInitialize_IsVirtual_AndInvokedExactlyOnceDuringConstruction()
    {
        ThemedScrollViewerVisual themed = new();

        themed.OnAfterInitializeCallCount.ShouldBe(1);
    }

    [Fact]
    public void RefreshScrollBarLengths_WhenOverriddenAsNoOp_ThemeHeightSurvivesClipContainerRefresh()
    {
        // A theme that owns the vertical scroll bar's Height should be able to override
        // RefreshScrollBarLengths and have its values survive when V3 re-refreshes margins.
        ThemeOwningScrollBarHeight themed = new();

        // Simulate V3 re-running its margin refresh (e.g. as it does in scroll-bar visibility states).
        themed.InvokeRefreshClipContainerMargins();

        themed.VerticalScrollBarInstance.Height.ShouldBe(-10f);
    }

    private class ThemeOwningScrollBarHeight : ScrollViewerVisual
    {
        public ThemeOwningScrollBarHeight()
        {
            VerticalScrollBarInstance.Height = -10f;
        }

        protected override void RefreshScrollBarLengths()
        {
            // Theme owns scroll bar lengths; intentionally skip base.
        }

        public void InvokeRefreshClipContainerMargins() => RefreshClipContainerMargins();
    }

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollViewer scrollViewer = new();
        InteractiveGue visual = scrollViewer.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        // ThumbContainer is used by ScrollBar for clicking to change value
        foreach (var child in children)
        {
            if (child.Name != "ThumbContainer")
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindClipContainer()
    {
        ScrollViewer scrollViewer = new();

        scrollViewer.ClipContainer.ShouldNotBeNull();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindInnerPanel()
    {
        ScrollViewer scrollViewer = new();

        scrollViewer.InnerPanel.ShouldNotBeNull();
    }

    [Fact]
    public void ScrollViewer_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ScrollViewer sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ThumbContainer_HasEvents_ShouldBeTrue()
    {
        ScrollBar scrollBar = new();
        InteractiveGue thumbContainer = (InteractiveGue)scrollBar.Visual.Children.First(c => c.Name == "ThumbContainer");
        thumbContainer.HasEvents.ShouldBeTrue("Because ThumbContainer is what is used for clicking to move the value");

    }
}
