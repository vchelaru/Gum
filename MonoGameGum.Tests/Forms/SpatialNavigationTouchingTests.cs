using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6961: controls that touch (zero spacing) are neighbors, while controls whose interiors
/// overlap stay excluded.
/// </summary>
public class SpatialNavigationTouchingTests : BaseTestClass
{
    private const float Down = MathF.PI / 2f;
    private const float Up = -MathF.PI / 2f;
    private const float Right = 0f;
    private const float Left = MathF.PI;

    private static Button Create(float x, float y, float width, float height, FrameworkElement? parent = null)
    {
        Button button = new();
        if (parent == null)
        {
            button.AddToRoot();
        }
        else
        {
            parent.AddChild(button);
        }
        button.WidthUnits = DimensionUnitType.Absolute;
        button.HeightUnits = DimensionUnitType.Absolute;
        button.X = x;
        button.Y = y;
        button.Width = width;
        button.Height = height;
        return button;
    }

    private static List<FrameworkElement> All(params FrameworkElement[] elements) => new List<FrameworkElement>(elements);

    private static StackPanel CreateStack(bool vertical, int count, out List<FrameworkElement> buttons)
    {
        StackPanel stack = new();
        stack.AddToRoot();
        stack.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
        stack.Spacing = 0;
        buttons = new List<FrameworkElement>();
        for (int i = 0; i < count; i++)
        {
            Button button = new();
            stack.AddChild(button);
            button.WidthUnits = DimensionUnitType.Absolute;
            button.HeightUnits = DimensionUnitType.Absolute;
            button.Width = 100;
            button.Height = 30;
            buttons.Add(button);
        }
        stack.Visual.UpdateLayout();
        return stack;
    }

    [Fact]
    public void FindBestCandidate_VerticalStackZeroSpacing_DownAndUpMoveBetweenNeighbors()
    {
        CreateStack(true, 3, out List<FrameworkElement> b);

        SpatialNavigationService.FindBestCandidate(b[0], Down, b).ShouldBe(b[1]);
        SpatialNavigationService.FindBestCandidate(b[1], Down, b).ShouldBe(b[2]);
        SpatialNavigationService.FindBestCandidate(b[2], Up, b).ShouldBe(b[1]);
        SpatialNavigationService.FindBestCandidate(b[1], Up, b).ShouldBe(b[0]);
    }

    [Fact]
    public void FindBestCandidate_HorizontalStackZeroSpacing_RightAndLeftMoveBetweenNeighbors()
    {
        CreateStack(false, 3, out List<FrameworkElement> b);

        SpatialNavigationService.FindBestCandidate(b[0], Right, b).ShouldBe(b[1]);
        SpatialNavigationService.FindBestCandidate(b[1], Right, b).ShouldBe(b[2]);
        SpatialNavigationService.FindBestCandidate(b[2], Left, b).ShouldBe(b[1]);
        SpatialNavigationService.FindBestCandidate(b[1], Left, b).ShouldBe(b[0]);
    }

    [Fact]
    public void FindBestCandidate_ZeroSpacingGrid_MovesToEdgeNeighborNotCornerNeighbor()
    {
        Button[,] cells = new Button[3, 3];
        List<FrameworkElement> all = new();
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                cells[row, col] = Create(col * 100, row * 30, 100, 30);
                all.Add(cells[row, col]);
            }
        }

        Button middle = cells[1, 1];
        SpatialNavigationService.FindBestCandidate(middle, Down, all).ShouldBe(cells[2, 1]);
        SpatialNavigationService.FindBestCandidate(middle, Up, all).ShouldBe(cells[0, 1]);
        SpatialNavigationService.FindBestCandidate(middle, Left, all).ShouldBe(cells[1, 0]);
        SpatialNavigationService.FindBestCandidate(middle, Right, all).ShouldBe(cells[1, 2]);
    }

    [Fact]
    public void FindBestCandidate_CornerTouchingOnly_ReachableThroughHalfPlaneFallback()
    {
        Button origin = Create(0, 0, 100, 30);
        Button diagonal = Create(100, 30, 100, 30);
        List<FrameworkElement> all = All(origin, diagonal);

        SpatialNavigationService.FindBestCandidate(origin, Down, all).ShouldBe(diagonal);
        SpatialNavigationService.FindBestCandidate(origin, Right, all).ShouldBe(diagonal);
        SpatialNavigationService.FindBestCandidate(origin, Up, all).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(origin, Left, all).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(diagonal, Up, all).ShouldBe(origin);
    }

    [Fact]
    public void FindBestCandidate_TouchingAndFartherCandidateInSameDirection_TouchingWinsInAnyOrder()
    {
        Button origin = Create(0, 0, 100, 30);
        Button touching = Create(0, 30, 100, 30);
        Button farther = Create(0, 200, 100, 30);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(farther, touching, origin)).ShouldBe(touching);
        SpatialNavigationService.FindBestCandidate(origin, Down, All(origin, touching, farther)).ShouldBe(touching);
    }

    [Fact]
    public void FindBestCandidate_WideOriginOverTwoTouchingButtons_PicksBestAlignedRegardlessOfOrder()
    {
        Button origin = Create(0, 0, 200, 30);
        Button left = Create(0, 30, 100, 30);
        Button right = Create(100, 30, 100, 30);

        // Equal lateral offset from the origin's center, so the leftmost wins in either order.
        SpatialNavigationService.FindBestCandidate(origin, Down, All(left, right)).ShouldBe(left);
        SpatialNavigationService.FindBestCandidate(origin, Down, All(right, left)).ShouldBe(left);

        Button narrowOrigin = Create(150, 300, 60, 30);
        Button a = Create(100, 330, 100, 30);
        Button c = Create(200, 330, 100, 30);
        // narrowOrigin center x = 180; a center 150 (offset 30), c center 250 (offset 70).
        SpatialNavigationService.FindBestCandidate(narrowOrigin, Down, All(c, a)).ShouldBe(a);
        SpatialNavigationService.FindBestCandidate(narrowOrigin, Down, All(a, c)).ShouldBe(a);
    }

    [Fact]
    public void FindBestCandidate_TouchingSideNeighbor_DoesNotCountForVerticalPress()
    {
        Button origin = Create(0, 0, 100, 30);
        Button side = Create(100, 0, 100, 30);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(origin, side)).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(origin, Up, All(origin, side)).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(origin, Right, All(origin, side)).ShouldBe(side);
    }

    [Fact]
    public void FindBestCandidate_OverlappingRectangles_StayExcluded()
    {
        Button origin = Create(0, 0, 100, 30);
        Button identical = Create(0, 0, 100, 30);
        Button partial = Create(50, 15, 100, 30);
        Button covering = Create(-10, -10, 200, 100);
        List<FrameworkElement> all = All(origin, identical, partial, covering);

        foreach (float direction in new[] { Up, Down, Left, Right })
        {
            SpatialNavigationService.FindBestCandidate(origin, direction, all).ShouldBeNull();
        }
    }

    [Fact]
    public void FindBestCandidate_ChildInsideParent_StaysExcluded()
    {
        Button parent = Create(0, 0, 300, 100);
        Button child = Create(0, 0, 100, 30, parent);
        Button below = Create(0, 100, 100, 30);
        List<FrameworkElement> all = All(parent, child, below);

        SpatialNavigationService.FindBestCandidate(child, Down, all).ShouldBe(below);
        SpatialNavigationService.FindBestCandidate(parent, Down, all).ShouldBe(below);
        SpatialNavigationService.FindBestCandidate(below, Up, all).ShouldBe(parent);
    }

    [Fact]
    public void FindBestCandidate_TouchingCandidateNestedInsideAnotherCandidate_StaysExcluded()
    {
        Button origin = Create(0, 0, 100, 30);
        Button container = Create(0, 30, 100, 60);
        Button nested = Create(0, 0, 100, 30, container);
        List<FrameworkElement> all = All(origin, nested, container);

        SpatialNavigationService.FindBestCandidate(origin, Down, all).ShouldBe(container);
    }

    [Fact]
    public void FindBestCandidate_ScrollViewerContentZeroSpacing_MovesBetweenButtons()
    {
        ScrollViewer scroll = new();
        scroll.AddToRoot();
        scroll.Width = 200;
        scroll.Height = 200;
        scroll.Visual.WidthUnits = DimensionUnitType.Absolute;
        scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        List<FrameworkElement> buttons = new();
        for (int i = 0; i < 3; i++)
        {
            Button button = new();
            scroll.AddChild(button);
            button.WidthUnits = DimensionUnitType.Absolute;
            button.HeightUnits = DimensionUnitType.Absolute;
            button.Width = 100;
            button.Height = 30;
            buttons.Add(button);
        }
        scroll.Visual.UpdateLayout();

        SpatialNavigationService.FindBestCandidate(buttons[0], Down, buttons).ShouldBe(buttons[1]);
        SpatialNavigationService.FindBestCandidate(buttons[1], Up, buttons).ShouldBe(buttons[0]);
    }

    [Fact]
    public void HandleGamepadNavigation_ZeroSpacingStack_DPadDownMovesFocus()
    {
        StackPanel stack = CreateStack(true, 2, out List<FrameworkElement> b);
        stack.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        ((Button)b[0]).IsFocused = true;

        GamePad gamepad = new();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        ((Button)b[0]).OnFocusUpdate();

        ((Button)b[1]).IsFocused.ShouldBeTrue();
    }

    private static float Gap(float cMin, float cMax, float oMin, float oMax)
    {
        if (cMax < oMin)
        {
            return cMax - oMin;
        }
        if (cMin > oMax)
        {
            return cMin - oMax;
        }
        return 0f;
    }

    private static bool IsLeftOrAbove(FrameworkElement c, FrameworkElement? o) =>
        o == null || (c.Visual.AbsoluteLeft != o.Visual.AbsoluteLeft ? c.Visual.AbsoluteLeft < o.Visual.AbsoluteLeft : c.Visual.AbsoluteTop < o.Visual.AbsoluteTop);

    private static float Normalize(float a)
    {
        float twoPi = MathF.PI * 2f;
        float n = a % twoPi;
        if (n > MathF.PI)
        {
            n -= twoPi;
        }
        else if (n < -MathF.PI)
        {
            n += twoPi;
        }
        return n;
    }

    /// <summary>The algorithm as it was on main before #6961 (without the nested-candidate filter).</summary>
    private static FrameworkElement? OldFindBestCandidate(FrameworkElement origin, float direction, List<FrameworkElement> candidates)
    {
        float maxAngle = MathF.PI / 4f;
        float angleWeight = 2f;
        float halfPlane = MathF.PI / 2f - 0.0001f;
        FrameworkElement? best = null;
        float bestScore = float.MaxValue;
        FrameworkElement? bestHalf = null;
        float bestHalfDistance = float.MaxValue;
        float bestHalfAngle = float.MaxValue;
        foreach (FrameworkElement candidate in candidates)
        {
            if (candidate == origin ||
                origin.Visual.IsInParentChain(candidate.Visual) ||
                candidate.Visual.IsInParentChain(origin.Visual))
            {
                continue;
            }
            float dx = Gap(candidate.Visual.AbsoluteLeft, candidate.Visual.AbsoluteRight, origin.Visual.AbsoluteLeft, origin.Visual.AbsoluteRight);
            float dy = Gap(candidate.Visual.AbsoluteTop, candidate.Visual.AbsoluteBottom, origin.Visual.AbsoluteTop, origin.Visual.AbsoluteBottom);
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance == 0f)
            {
                continue;
            }
            float diff = MathF.Abs(Normalize(MathF.Atan2(dy, dx) - direction));
            if (diff > maxAngle)
            {
                if (diff < halfPlane &&
                    (distance < bestHalfDistance ||
                    (distance == bestHalfDistance && diff < bestHalfAngle) ||
                    (distance == bestHalfDistance && diff == bestHalfAngle && IsLeftOrAbove(candidate, bestHalf))))
                {
                    bestHalf = candidate;
                    bestHalfDistance = distance;
                    bestHalfAngle = diff;
                }
                continue;
            }
            float score = distance * (1f + angleWeight * (diff / maxAngle));
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best ?? bestHalf;
    }

    [Fact]
    public void FindBestCandidate_LayoutsWithoutTouchingPairs_MatchesPreviousBehavior()
    {
        Random random = new(6961);
        float[] directions = { Right, Down, Left, Up };
        int cases = SweepScale.Cases(400);
        int compared = 0;

        for (int i = 0; i < cases; i++)
        {
            List<FrameworkElement> all = new();
            int count = random.Next(2, 8);
            for (int c = 0; c < count; c++)
            {
                all.Add(Create(random.Next(0, 700), random.Next(0, 400), random.Next(20, 200), random.Next(20, 120)));
            }

            if (HasZeroGapPair(all))
            {
                continue;
            }

            foreach (FrameworkElement origin in all)
            {
                foreach (float direction in directions)
                {
                    SpatialNavigationService.FindBestCandidate(origin, direction, all)
                        .ShouldBe(OldFindBestCandidate(origin, direction, all));
                    compared++;
                }
            }
        }

        compared.ShouldBeGreaterThan(0);
    }

    private static bool HasZeroGapPair(List<FrameworkElement> all)
    {
        foreach (FrameworkElement a in all)
        {
            foreach (FrameworkElement b in all)
            {
                if (a != b &&
                    Gap(b.Visual.AbsoluteLeft, b.Visual.AbsoluteRight, a.Visual.AbsoluteLeft, a.Visual.AbsoluteRight) == 0f &&
                    Gap(b.Visual.AbsoluteTop, b.Visual.AbsoluteBottom, a.Visual.AbsoluteTop, a.Visual.AbsoluteBottom) == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
