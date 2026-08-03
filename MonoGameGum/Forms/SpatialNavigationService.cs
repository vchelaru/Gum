using Gum.Forms.Controls;
using System;
using System.Collections.Generic;

namespace Gum.Forms;

/// <summary>
/// Scores focusable <see cref="FrameworkElement"/> candidates by on-screen distance and angular
/// alignment to a requested direction, and picks the best match — the geometric core of
/// direction-agnostic ("spatial") focus navigation (issue #4129). Stateless and read-only: it only
/// reads already-resolved absolute (screen-space) bounds off the visual tree and does not mutate
/// focus itself; callers (see <see cref="FrameworkElement"/>'s gamepad navigation methods) apply
/// the result.
/// </summary>
public static class SpatialNavigationService
{
    /// <summary>
    /// Returns whichever <paramref name="candidates"/> element best matches
    /// <paramref name="directionAngleRadians"/> from <paramref name="origin"/>'s center, or null if
    /// none qualify. Distance and angle to each candidate are measured to the closest point on that
    /// candidate's own rectangle to <paramref name="origin"/>'s center, not the candidate's raw
    /// center — a wide candidate (e.g. a full-width Slider) can otherwise have its center sit far
    /// enough to the side that the angle falls outside the direction cone, even though the
    /// candidate's rectangle spans directly across from the origin. Candidates outside the
    /// direction cone (<paramref name="maxAngleRadians"/> either side of the requested angle) are
    /// excluded so navigation never moves backwards; among the rest, the lowest
    /// <c>distance * (1 + angleWeight * angleDiff / maxAngleRadians)</c> score wins.
    /// </summary>
    /// <param name="origin">The currently-focused element navigation is relative to.</param>
    /// <param name="directionAngleRadians">
    /// The requested direction in screen space: 0 = right, increasing clockwise (Y grows downward).
    /// </param>
    /// <param name="candidates">
    /// The focusable elements to consider. <paramref name="origin"/>, any ancestor of
    /// <paramref name="origin"/> (e.g. a large focusable container the origin sits near the edge
    /// of), and any candidate nested inside <paramref name="origin"/> or inside another candidate
    /// (e.g. a composite control's own internal focusable part, such as a Slider's Thumb button)
    /// are skipped if present — an ancestor's own center can otherwise score better than a true
    /// sibling simply by virtue of containing the origin, and an internal part sitting right at its
    /// owning control's edge can otherwise outscore that control (or an unrelated sibling) simply
    /// by virtue of being nested inside it. This mirrors index-based <see cref="FrameworkElement.HandleTab"/>,
    /// which treats a focusable element as an opaque stop and never descends into it.
    /// </param>
    /// <param name="maxAngleRadians">Half-width of the direction cone; candidates outside are excluded.</param>
    /// <param name="angleWeight">How strongly angular misalignment penalizes an otherwise-close candidate.</param>
    public static FrameworkElement? FindBestCandidate(
        FrameworkElement origin,
        float directionAngleRadians,
        IEnumerable<FrameworkElement> candidates,
        float maxAngleRadians = MathF.PI / 4f,
        float angleWeight = 2f)
    {
        List<FrameworkElement> candidateList = candidates as List<FrameworkElement> ?? new List<FrameworkElement>(candidates);

        (float originX, float originY) = GetCenter(origin);

        FrameworkElement? best = null;
        float bestScore = float.MaxValue;

        foreach (FrameworkElement candidate in candidateList)
        {
            if (candidate == origin ||
                origin.Visual.IsInParentChain(candidate.Visual) ||
                candidate.Visual.IsInParentChain(origin.Visual) ||
                IsNestedUnderAnotherCandidate(candidate, candidateList))
            {
                continue;
            }

            (float candidateX, float candidateY) = GetClosestPoint(candidate, originX, originY);
            float dx = candidateX - originX;
            float dy = candidateY - originY;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance == 0f)
            {
                continue;
            }

            float candidateAngle = MathF.Atan2(dy, dx);
            float angleDiff = MathF.Abs(NormalizeAngle(candidateAngle - directionAngleRadians));

            if (angleDiff > maxAngleRadians)
            {
                continue;
            }

            float score = distance * (1f + angleWeight * (angleDiff / maxAngleRadians));

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    // Two independently-focusable elements can be in an ancestor/descendant relationship (a Slider
    // and its own Thumb button both pass the focusable filter), so excluding only origin-relative
    // ancestors/descendants isn't enough once neither is the origin. Only the outermost focusable
    // element in any such chain should compete for the score.
    private static bool IsNestedUnderAnotherCandidate(FrameworkElement candidate, List<FrameworkElement> candidates)
    {
        foreach (FrameworkElement other in candidates)
        {
            if (other != candidate && candidate.Visual.IsInParentChain(other.Visual))
            {
                return true;
            }
        }
        return false;
    }

    private static (float x, float y) GetCenter(FrameworkElement element)
    {
        float x = (element.Visual.AbsoluteLeft + element.Visual.AbsoluteRight) / 2f;
        float y = (element.Visual.AbsoluteTop + element.Visual.AbsoluteBottom) / 2f;
        return (x, y);
    }

    // The point on element's rectangle nearest to (fromX, fromY) -- equal to element's own center
    // when (fromX, fromY) falls outside its bounds on both axes, but clamped onto the rectangle's
    // edge on any axis where (fromX, fromY) already overlaps it.
    private static (float x, float y) GetClosestPoint(FrameworkElement element, float fromX, float fromY)
    {
        float left = element.Visual.AbsoluteLeft;
        float right = element.Visual.AbsoluteRight;
        float top = element.Visual.AbsoluteTop;
        float bottom = element.Visual.AbsoluteBottom;

        float x = Math.Clamp(fromX, left, right);
        float y = Math.Clamp(fromY, top, bottom);
        return (x, y);
    }

    // Wraps to (-π, π] so the angular difference between two directions is always the shorter way around.
    private static float NormalizeAngle(float angleRadians)
    {
        float twoPi = MathF.PI * 2f;
        float normalized = angleRadians % twoPi;
        if (normalized > MathF.PI)
        {
            normalized -= twoPi;
        }
        else if (normalized < -MathF.PI)
        {
            normalized += twoPi;
        }
        return normalized;
    }
}
