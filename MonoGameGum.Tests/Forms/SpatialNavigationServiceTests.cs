using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class SpatialNavigationServiceTests : BaseTestClass
{
    private static Button CreatePositionedButton(float x, float y)
    {
        Button button = new();
        button.AddToRoot();
        button.X = x;
        button.Y = y;
        return button;
    }

    [Fact]
    public void FindBestCandidate_DiagonalRequest_PicksDiagonallyPlacedCandidate()
    {
        Button origin = CreatePositionedButton(0, 0);
        Button rightOnAxis = CreatePositionedButton(150, 0);
        Button downRightDiagonal = CreatePositionedButton(100, 100);

        List<FrameworkElement> candidates = new() { rightOnAxis, downRightDiagonal };

        float fortyFiveDegrees = MathF.PI / 4f;
        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, fortyFiveDegrees, candidates);

        result.ShouldBe(downRightDiagonal);
    }

    [Fact]
    public void FindBestCandidate_ExcludesCandidateOutsideDirectionCone_EvenWhenClosest()
    {
        Button origin = CreatePositionedButton(0, 0);
        Button behind = CreatePositionedButton(-50, 0);
        Button ahead = CreatePositionedButton(200, 0);

        List<FrameworkElement> candidates = new() { behind, ahead };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, 0f, candidates);

        result.ShouldBe(ahead);
    }

    [Fact]
    public void FindBestCandidate_ExcludesAncestorsOfOrigin_EvenWhenGeometricallyBestScored()
    {
        // Reproduces a real finding: a large focusable container wrapping the origin near its top
        // edge has its own center positioned "downward" relative to the origin, and close by (the
        // origin is inside it) -- so without this exclusion, pressing Down from an item near the
        // top of a container can navigate back to that same container instead of a true sibling.
        Panel container = new();
        container.AddToRoot();
        container.X = 0;
        container.Y = 0;
        container.Width = 150;
        container.Height = 200;

        Button origin = new();
        container.AddChild(origin);
        origin.X = 2;
        origin.Y = 2;
        origin.Width = 10;
        origin.Height = 10;

        Button sibling = new();
        sibling.AddToRoot();
        sibling.X = 0;
        sibling.Y = 300;
        sibling.Width = 10;
        sibling.Height = 10;

        List<FrameworkElement> candidates = new() { container, sibling };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, MathF.PI / 2f, candidates);

        result.ShouldBe(sibling);
    }

    [Fact]
    public void FindBestCandidate_NeverReturnsOrigin_EvenIfPresentInCandidates()
    {
        Button origin = CreatePositionedButton(0, 0);
        Button only = CreatePositionedButton(100, 0);

        List<FrameworkElement> candidates = new() { origin, only };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, 0f, candidates);

        result.ShouldBe(only);
    }

    [Fact]
    public void FindBestCandidate_PicksNearerOnAxisCandidate_OverFartherOne()
    {
        Button origin = CreatePositionedButton(0, 0);
        Button near = CreatePositionedButton(100, 0);
        Button far = CreatePositionedButton(300, 0);

        List<FrameworkElement> candidates = new() { near, far };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, 0f, candidates);

        result.ShouldBe(near);
    }

    [Fact]
    public void FindBestCandidate_PrefersWellAlignedFartherCandidate_OverCloserOffAxisCandidate()
    {
        Button origin = CreatePositionedButton(0, 0);
        // Close, but nearly perpendicular to the rightward request:
        Button closeOffAxis = CreatePositionedButton(20, 100);
        // Farther, but exactly aligned with the rightward request:
        Button farOnAxis = CreatePositionedButton(150, 0);

        List<FrameworkElement> candidates = new() { closeOffAxis, farOnAxis };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, 0f, candidates);

        result.ShouldBe(farOnAxis);
    }

    [Fact]
    public void FindBestCandidate_ReturnsNull_WhenNoCandidatesQualify()
    {
        Button origin = CreatePositionedButton(0, 0);
        Button behind = CreatePositionedButton(-50, 0);

        List<FrameworkElement> candidates = new() { behind };

        FrameworkElement? result = SpatialNavigationService.FindBestCandidate(origin, 0f, candidates);

        result.ShouldBeNull();
    }
}
