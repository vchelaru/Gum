using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Dock, Anchor, UpdateTextureValuesFrom and clearing Children all suspend the one element, run a body
// and resume it. A body that throws must not leave the element suspended: every later UpdateLayout on a
// suspended element early-outs, so it would keep stale positions and sizes for good.
public class ElementLayoutSuspensionExceptionSafetyTests : BaseTestClass
{
    private class ThrowingTextureCoordinate : ITextureCoordinate
    {
        public System.Drawing.Rectangle? SourceRectangle
        {
            get => throw new InvalidOperationException("source rectangle failed");
            set { }
        }
        public bool Wrap { get; set; }
        public float? TextureWidth => null;
        public float? TextureHeight => null;
        public bool FlipHorizontal { get; set; }
    }

    [Fact]
    public void Dock_ShouldResumeLayout_WhenTheDockIsNotDefined()
    {
        ContainerRuntime container = new();

        Should.Throw<NotImplementedException>(() => container.Dock((Dock)999));

        container.IsLayoutSuspended.ShouldBeFalse();
    }

    [Fact]
    public void Anchor_ShouldResumeLayout_WhenTheAnchorIsNotDefined()
    {
        ContainerRuntime container = new();

        Should.Throw<NotImplementedException>(() => container.Anchor((Anchor)999));

        container.IsLayoutSuspended.ShouldBeFalse();
    }

    [Fact]
    public void Anchor_ShouldStaySuspended_WhenSuspendedBeforeTheCallAndTheAnchorIsNotDefined()
    {
        ContainerRuntime container = new();
        container.SuspendLayout();

        Should.Throw<NotImplementedException>(() => container.Anchor((Anchor)999));

        container.IsLayoutSuspended.ShouldBeTrue();
    }

    [Fact]
    public void UpdateTextureValuesFrom_ShouldResumeLayout_WhenReadingTheSourceThrows()
    {
        SpriteRuntime sprite = new();

        Should.Throw<InvalidOperationException>(
            () => sprite.UpdateTextureValuesFrom(new ThrowingTextureCoordinate()));

        sprite.IsLayoutSuspended.ShouldBeFalse();
    }

    [Fact]
    public void ChildrenClear_ShouldResumeLayout_WhenAChildParentChangedHandlerThrows()
    {
        ContainerRuntime parent = new();
        ContainerRuntime first = new();
        ContainerRuntime second = new();
        parent.Children.Add(first);
        parent.Children.Add(second);
        first.ParentChanged += (_, _) => throw new InvalidOperationException("handler failed");

        Should.Throw<InvalidOperationException>(() => parent.Children.Clear());

        parent.IsLayoutSuspended.ShouldBeFalse();
    }
}
