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
    /// none qualify. Candidates outside the direction cone (<paramref name="maxAngleRadians"/> either
    /// side of the requested angle) are excluded so navigation never moves backwards; among the rest,
    /// the lowest <c>distance * (1 + angleWeight * angleDiff / maxAngleRadians)</c> score wins.
    /// </summary>
    /// <param name="origin">The currently-focused element navigation is relative to.</param>
    /// <param name="directionAngleRadians">
    /// The requested direction in screen space: 0 = right, increasing clockwise (Y grows downward).
    /// </param>
    /// <param name="candidates">
    /// The focusable elements to consider. <paramref name="origin"/>, any ancestor of
    /// <paramref name="origin"/> (e.g. a large focusable container the origin sits near the edge
    /// of), and any descendant of <paramref name="origin"/> (e.g. a composite control's own
    /// internal focusable part, such as a Slider's Thumb button) are skipped if present — an
    /// ancestor's own center can otherwise score better than a true sibling simply by virtue of
    /// containing the origin, and a descendant sitting right at the origin's edge can otherwise
    /// outscore a true sibling simply by virtue of being nested inside it — both send focus
    /// somewhere other than an actual neighboring control.
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
        (float originX, float originY) = GetCenter(origin);

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

            (float candidateX, float candidateY) = GetCenter(candidate);
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

    private static (float x, float y) GetCenter(FrameworkElement element)
    {
        float x = (element.Visual.AbsoluteLeft + element.Visual.AbsoluteRight) / 2f;
        float y = (element.Visual.AbsoluteTop + element.Visual.AbsoluteBottom) / 2f;
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
