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
/// Issue #6936: when no candidate is inside the direction cone, spatial navigation falls back to the
/// nearest candidate in the half-plane of the pressed direction.
/// </summary>
public class SpatialNavigationFallbackTests : BaseTestClass
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

    [Fact]
    public void FindBestCandidate_CardsLayout_DownFromOuterCards_MovesToCenteredButton()
    {
        Button cardA = Create(208, 80, 120, 160);
        Button cardB = Create(340, 80, 120, 160);
        Button cardC = Create(472, 80, 120, 160);
        Button begin = Create(350, 256, 100, 32);
        List<FrameworkElement> all = All(cardA, cardB, cardC, begin);

        SpatialNavigationService.FindBestCandidate(cardA, Down, all).ShouldBe(begin);
        SpatialNavigationService.FindBestCandidate(cardC, Down, all).ShouldBe(begin);
    }

    [Fact]
    public void FindBestCandidate_OptionsLayout_DownFromLastCheckBox_MovesToBack()
    {
        Button shake = Create(40, 200, 150, 30);
        Button slider1 = Create(300, 80, 400, 30);
        Button slider2 = Create(300, 130, 400, 30);
        Button back = Create(350, 300, 100, 32);
        List<FrameworkElement> all = All(shake, slider1, slider2, back);

        SpatialNavigationService.FindBestCandidate(shake, Down, all).ShouldBe(back);
    }

    [Fact]
    public void FindBestCandidate_LoneCornerButton_DownAndUpMoveBetweenQuitAndApply()
    {
        Button quit = Create(40, 200, 150, 30);
        Button apply = Create(660, 420, 100, 32);
        List<FrameworkElement> all = All(quit, apply);

        SpatialNavigationService.FindBestCandidate(quit, Down, all).ShouldBe(apply);
        SpatialNavigationService.FindBestCandidate(apply, Up, all).ShouldBe(quit);
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_PicksNearestInHalfPlane_NotFirstOrFarthest()
    {
        Button origin = Create(0, 0, 50, 50);
        Button far = Create(300, 400, 50, 50);
        Button near = Create(100, 100, 50, 50);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(far, near)).ShouldBe(near);
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_CandidateBehind_IsNotChosen()
    {
        Button origin = Create(0, 300, 50, 50);
        Button above = Create(150, 0, 50, 50);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(above)).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(above, Up, All(origin)).ShouldBeNull();
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_CandidateExactlyBeside_IsNotChosen()
    {
        Button origin = Create(0, 0, 50, 50);
        Button beside = Create(200, 0, 50, 50);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(beside)).ShouldBeNull();
        SpatialNavigationService.FindBestCandidate(origin, Up, All(beside)).ShouldBeNull();
    }

    [Fact]
    public void FindBestCandidate_ConeHasCandidate_FarInConeWinsOverNearerHalfPlaneCandidate()
    {
        Button origin = Create(0, 0, 50, 50);
        // Half-plane only (gap (50, 10), about 79 degrees off Down), very close.
        Button nearOffAxis = Create(100, 60, 50, 50);
        // In cone, far away.
        Button farInCone = Create(0, 600, 50, 50);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(nearOffAxis, farInCone)).ShouldBe(farInCone);
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_EqualDistanceTie_PicksSameCandidateRegardlessOfListOrder()
    {
        Button origin = Create(0, 0, 50, 50);
        Button steeper = Create(110, 110, 50, 50);
        Button flatter = Create(160, 60, 50, 50);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(steeper, flatter)).ShouldBe(
            SpatialNavigationService.FindBestCandidate(origin, Down, All(flatter, steeper)));
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_MirroredCandidatesWithEqualDistanceAndAngle_PickSameCandidateRegardlessOfListOrder()
    {
        // Gaps (-80, 40) and (80, 40): same distance, same angle off Down, both outside the cone.
        Button origin = Create(200, 0, 50, 50);
        Button left = Create(70, 90, 50, 50);
        Button right = Create(330, 90, 50, 50);

        FrameworkElement? leftFirst = SpatialNavigationService.FindBestCandidate(origin, Down, All(left, right));
        FrameworkElement? rightFirst = SpatialNavigationService.FindBestCandidate(origin, Down, All(right, left));

        leftFirst.ShouldNotBeNull();
        leftFirst.ShouldBe(rightFirst);
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_OverlappingCandidate_IsStillSkipped()
    {
        Button origin = Create(0, 0, 100, 100);
        Button overlapping = Create(50, 50, 100, 100);

        SpatialNavigationService.FindBestCandidate(origin, Down, All(overlapping)).ShouldBeNull();
    }

    [Fact]
    public void FindBestCandidate_EmptyCone_NestedCandidate_IsStillExcluded()
    {
        Button origin = Create(0, 0, 50, 50);
        Button outer = Create(300, 100, 100, 100);
        Button inner = Create(0, 0, 20, 20, outer);

        // Only the outer control may compete; inner sits at the outer's top-left, nearer to origin.
        SpatialNavigationService.FindBestCandidate(origin, Down, All(outer, inner)).ShouldBe(outer);
    }

    [Fact]
    public void HandleGamepadNavigation_CardsLayout_DownFromOuterCard_MovesFocusToButton()
    {
        Panel panel = new();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        Button cardA = Create(208, 80, 120, 160, panel);
        Create(340, 80, 120, 160, panel);
        Create(472, 80, 120, 160, panel);
        Button begin = Create(350, 256, 100, 32, panel);
        cardA.IsFocused = true;

        GamePad gamepad = new();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        cardA.OnFocusUpdate();

        begin.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleGamepadNavigation_ExplicitOverride_StillWinsOverFallback()
    {
        Panel panel = new();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        Button cardA = Create(208, 80, 120, 160, panel);
        Create(350, 256, 100, 32, panel);
        cardA.SpatialNavigationDown = cardA;
        cardA.IsFocused = true;

        GamePad gamepad = new();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        cardA.OnFocusUpdate();

        cardA.IsFocused.ShouldBeTrue();
    }

    /// <summary>The algorithm as it was before #6936 (cone only, no fallback).</summary>
    private static FrameworkElement? OldFindBestCandidate(FrameworkElement origin, float direction, List<FrameworkElement> candidates)
    {
        float maxAngle = MathF.PI / 4f;
        float angleWeight = 2f;
        FrameworkElement? best = null;
        float bestScore = float.MaxValue;
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

            float diff = MathF.Atan2(dy, dx) - direction;
            float twoPi = MathF.PI * 2f;
            diff %= twoPi;
            if (diff > MathF.PI)
            {
                diff -= twoPi;
            }
            else if (diff < -MathF.PI)
            {
                diff += twoPi;
            }
            diff = MathF.Abs(diff);
            if (diff > maxAngle)
            {
                continue;
            }

            float score = distance * (1f + angleWeight * (diff / maxAngle));
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
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

    [Fact]
    public void FindBestCandidate_RandomLayouts_MatchesPreviousBehaviorWhenConeHasCandidate_AndNeverMovesBackwardsOtherwise()
    {
        Random random = new(6936);
        float[] directions = { Right, Down, Left, Up };
        int cases = SweepScale.Cases(400);
        int coneCases = 0;
        int fallbackCases = 0;

        for (int i = 0; i < cases; i++)
        {
            Button origin = Create(random.Next(0, 700), random.Next(0, 400), random.Next(20, 200), random.Next(20, 120));
            List<FrameworkElement> candidates = new();
            int count = random.Next(1, 7);
            for (int c = 0; c < count; c++)
            {
                candidates.Add(Create(random.Next(0, 700), random.Next(0, 400), random.Next(20, 200), random.Next(20, 120)));
            }

            // #6961: a candidate with a zero gap to the origin that only touches it is now a neighbor,
            // which the old algorithm excluded, so the old-behavior comparison only covers origins
            // with no zero-gap candidate.
            bool originHasZeroGapCandidate = false;
            foreach (FrameworkElement candidate in candidates)
            {
                if (Gap(candidate.Visual.AbsoluteLeft, candidate.Visual.AbsoluteRight, origin.Visual.AbsoluteLeft, origin.Visual.AbsoluteRight) == 0f &&
                    Gap(candidate.Visual.AbsoluteTop, candidate.Visual.AbsoluteBottom, origin.Visual.AbsoluteTop, origin.Visual.AbsoluteBottom) == 0f)
                {
                    originHasZeroGapCandidate = true;
                }
            }

            foreach (float direction in directions)
            {
                FrameworkElement? old = OldFindBestCandidate(origin, direction, candidates);
                FrameworkElement? actual = SpatialNavigationService.FindBestCandidate(origin, direction, candidates);
                if (old != null && !originHasZeroGapCandidate)
                {
                    coneCases++;
                    actual.ShouldBe(old);
                }
                else if (actual != null && (old == null || originHasZeroGapCandidate))
                {
                    fallbackCases++;
                    float dx = Gap(actual.Visual.AbsoluteLeft, actual.Visual.AbsoluteRight, origin.Visual.AbsoluteLeft, origin.Visual.AbsoluteRight);
                    float dy = Gap(actual.Visual.AbsoluteTop, actual.Visual.AbsoluteBottom, origin.Visual.AbsoluteTop, origin.Visual.AbsoluteBottom);
                    float axisX = MathF.Round(MathF.Cos(direction));
                    float axisY = MathF.Round(MathF.Sin(direction));
                    float along = dx * axisX + dy * axisY;
                    if (along == 0f)
                    {
                        // Touching neighbor (zero gap along the pressed axis): its center must lie
                        // in the pressed direction from the origin's center.
                        float centerDx = (actual.Visual.AbsoluteLeft + actual.Visual.AbsoluteRight - origin.Visual.AbsoluteLeft - origin.Visual.AbsoluteRight) / 2f;
                        float centerDy = (actual.Visual.AbsoluteTop + actual.Visual.AbsoluteBottom - origin.Visual.AbsoluteTop - origin.Visual.AbsoluteBottom) / 2f;
                        (centerDx * axisX + centerDy * axisY).ShouldBeGreaterThan(0f);
                    }
                    along.ShouldBeGreaterThanOrEqualTo(0f);
                }
            }
        }

        coneCases.ShouldBeGreaterThan(0);
        fallbackCases.ShouldBeGreaterThan(0);
    }
}
