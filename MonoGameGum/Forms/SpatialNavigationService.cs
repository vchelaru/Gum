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
    // A candidate outside the cone still lies in the pressed direction when its gap angle is under
    // 90 degrees off it. The small margin keeps a candidate exactly beside the origin (90 degrees,
    // up to float rounding) out, so Down never turns into a sideways move.
    private const float HalfPlaneLimitRadians = MathF.PI / 2f - 0.0001f;

    /// <summary>
    /// Returns whichever <paramref name="candidates"/> element best matches
    /// <paramref name="directionAngleRadians"/> from <paramref name="origin"/>, or null if none
    /// qualify. Distance and angle are measured between the closest points of the origin's and each
    /// candidate's rectangles (the AABB gap along each axis, zero on an axis where the two rectangles'
    /// ranges overlap), not raw center-to-center — either rectangle can be wide (e.g. a full-width
    /// Slider as the origin or as a candidate) and have its center sit far enough to the side that a
    /// center-based angle falls outside the direction cone, even though the rectangles are directly
    /// across from each other. Candidates outside the direction cone
    /// (<paramref name="maxAngleRadians"/> either side of the requested angle) are excluded so
    /// navigation never moves backwards; among the rest, the lowest
    /// <c>distance * (1 + angleWeight * angleDiff / maxAngleRadians)</c> score wins. Only when no
    /// candidate is inside the cone does the search fall back to the nearest candidate whose gap
    /// angle is less than 90 degrees off the requested direction (ties go to the smaller angle, then
    /// the leftmost, then topmost candidate), so a control that lies on the pressed side is still reachable.
    /// A candidate whose rectangle only touches the origin's along an edge (zero spacing) lies in the
    /// direction of its center and outranks every candidate with a gap; several are ranked by center
    /// alignment across the pressed axis. A corner-only touch is reachable only through the fallback.
    /// Rectangles whose interiors overlap are never neighbors.
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
    /// by virtue of being nested inside it. This mirrors index-based <see cref="FrameworkElement.HandleTab(TabDirection, FrameworkElement?, bool)"/>,
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

        FrameworkElement? best = null;
        float bestScore = float.MaxValue;
        FrameworkElement? bestInHalfPlane = null;
        float bestHalfPlaneDistance = float.MaxValue;
        float bestHalfPlaneAngleDiff = float.MaxValue;
        FrameworkElement? bestTouching = null;
        float bestTouchingLateral = float.MaxValue;

        foreach (FrameworkElement candidate in candidateList)
        {
            if (candidate == origin ||
                origin.Visual.IsInParentChain(candidate.Visual) ||
                candidate.Visual.IsInParentChain(origin.Visual) ||
                IsNestedUnderAnotherCandidate(candidate, origin, candidateList))
            {
                continue;
            }

            (float dx, float dy) = GetClosestGap(origin, candidate);
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            float candidateAngle;
            bool isCornerTouch = false;
            float touchingLateral = 0f;
            if (distance == 0f)
            {
                // Zero gap: the rectangles either overlap (interiors intersect: stays excluded, as a
                // child, a covering rectangle or a stacked window has no direction) or only touch
                // along an edge or corner (zero-spacing stack or grid), which is a valid neighbor.
                if (!TryGetTouchDirection(origin, candidate, out float touchX, out float touchY))
                {
                    continue;
                }
                candidateAngle = MathF.Atan2(touchY, touchX);
                isCornerTouch = touchX != 0f && touchY != 0f;
                // Offset across the pressed axis; picks the best-aligned of several touching candidates.
                touchingLateral = touchX == 0f
                    ? MathF.Abs(GetCenterX(candidate) - GetCenterX(origin))
                    : MathF.Abs(GetCenterY(candidate) - GetCenterY(origin));
            }
            else
            {
                candidateAngle = MathF.Atan2(dy, dx);
            }

            float angleDiff = MathF.Abs(NormalizeAngle(candidateAngle - directionAngleRadians));

            if (distance == 0f && !isCornerTouch && angleDiff <= maxAngleRadians)
            {
                // Edge-touching neighbor in the pressed direction: distance 0 beats any gap, so it
                // outranks every scored candidate. Ties between several go to the best-aligned
                // (smallest lateral center offset), then leftmost, then topmost.
                if (touchingLateral < bestTouchingLateral ||
                    (touchingLateral == bestTouchingLateral && IsLeftOrAbove(candidate, bestTouching)))
                {
                    bestTouching = candidate;
                    bestTouchingLateral = touchingLateral;
                }
                continue;
            }

            // A corner-touching neighbor lies on the diagonal (45 degrees off both axes), so it never
            // counts as in the cone; it is only reachable through the half-plane fallback below.
            if (isCornerTouch || angleDiff > maxAngleRadians)
            {
                if (angleDiff < HalfPlaneLimitRadians &&
                    (distance < bestHalfPlaneDistance ||
                    (distance == bestHalfPlaneDistance && angleDiff < bestHalfPlaneAngleDiff) ||
                    (distance == bestHalfPlaneDistance && angleDiff == bestHalfPlaneAngleDiff &&
                        IsLeftOrAbove(candidate, bestInHalfPlane))))
                {
                    bestInHalfPlane = candidate;
                    bestHalfPlaneDistance = distance;
                    bestHalfPlaneAngleDiff = angleDiff;
                }
                continue;
            }

            float score = distance * (1f + angleWeight * (angleDiff / maxAngleRadians));

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return bestTouching ?? best ?? bestInHalfPlane;
    }

    // Two independently-focusable elements can be in an ancestor/descendant relationship (a Slider
    // and its own Thumb button both pass the focusable filter), so excluding only origin-relative
    // ancestors/descendants isn't enough once neither is the origin. Only the outermost focusable
    // element in any such chain should compete for the score.
    // Final fallback tie-break so equal-distance, equal-angle candidates resolve the same way
    // regardless of list order.
    private static bool IsLeftOrAbove(FrameworkElement candidate, FrameworkElement? other)
    {
        if (other == null)
        {
            return true;
        }
        if (candidate.Visual.AbsoluteLeft != other.Visual.AbsoluteLeft)
        {
            return candidate.Visual.AbsoluteLeft < other.Visual.AbsoluteLeft;
        }
        if (candidate.Visual.AbsoluteTop != other.Visual.AbsoluteTop)
        {
            return candidate.Visual.AbsoluteTop < other.Visual.AbsoluteTop;
        }
        // Same top-left corner: the smaller rectangle (width, then height) wins.
        float candidateWidth = candidate.Visual.AbsoluteRight - candidate.Visual.AbsoluteLeft;
        float otherWidth = other.Visual.AbsoluteRight - other.Visual.AbsoluteLeft;
        if (candidateWidth != otherWidth)
        {
            return candidateWidth < otherWidth;
        }
        return candidate.Visual.AbsoluteBottom - candidate.Visual.AbsoluteTop <
            other.Visual.AbsoluteBottom - other.Visual.AbsoluteTop;
    }

    // The origin's own ancestors never count as the outer element: they are excluded from scoring, and a
    // focusable ScrollViewer containing the origin would otherwise swallow every sibling inside it (#6939).
    private static bool IsNestedUnderAnotherCandidate(FrameworkElement candidate, FrameworkElement origin, List<FrameworkElement> candidates)
    {
        foreach (FrameworkElement other in candidates)
        {
            if (other != candidate && !origin.Visual.IsInParentChain(other.Visual) && candidate.Visual.IsInParentChain(other.Visual))
            {
                return true;
            }
        }
        return false;
    }

    // For a zero-gap pair: false when the interiors intersect (overlap). Otherwise the pair touches,
    // and (x, y) is the unit-sign direction from origin to candidate: an edge touch is purely along
    // one axis (toward the candidate's center), a corner touch is the diagonal toward the candidate's
    // center. False too when the centers coincide (degenerate zero-size rectangles), which has no
    // direction.
    private static bool TryGetTouchDirection(FrameworkElement origin, FrameworkElement candidate, out float x, out float y)
    {
        float overlapWidth = MathF.Min(origin.Visual.AbsoluteRight, candidate.Visual.AbsoluteRight) -
            MathF.Max(origin.Visual.AbsoluteLeft, candidate.Visual.AbsoluteLeft);
        float overlapHeight = MathF.Min(origin.Visual.AbsoluteBottom, candidate.Visual.AbsoluteBottom) -
            MathF.Max(origin.Visual.AbsoluteTop, candidate.Visual.AbsoluteTop);
        x = 0f;
        y = 0f;

        if (overlapWidth > 0f && overlapHeight > 0f)
        {
            return false;
        }

        float centerDx = GetCenterX(candidate) - GetCenterX(origin);
        float centerDy = GetCenterY(candidate) - GetCenterY(origin);
        // Touching along a horizontal edge (ranges overlap on X) means the neighbor lies up or down.
        bool touchesHorizontalEdge = overlapWidth > 0f;
        bool touchesVerticalEdge = overlapHeight > 0f;
        if (!touchesVerticalEdge)
        {
            y = MathF.Sign(centerDy);
        }
        if (!touchesHorizontalEdge)
        {
            x = MathF.Sign(centerDx);
        }
        return x != 0f || y != 0f;
    }

    private static float GetCenterX(FrameworkElement element) =>
        (element.Visual.AbsoluteLeft + element.Visual.AbsoluteRight) / 2f;

    private static float GetCenterY(FrameworkElement element) =>
        (element.Visual.AbsoluteTop + element.Visual.AbsoluteBottom) / 2f;

    // The (dx, dy) gap from origin's rectangle to candidate's rectangle: zero on any axis where the
    // two rectangles' ranges overlap on that axis, and the true edge-to-edge gap (signed, positive
    // toward candidate) on any axis where they don't. This is symmetric in rectangle size -- unlike
    // measuring from one side's raw center, a wide rectangle on EITHER side (origin or candidate)
    // can't push the angle off-axis on an axis where the two actually line up.
    private static (float dx, float dy) GetClosestGap(FrameworkElement origin, FrameworkElement candidate)
    {
        float originLeft = origin.Visual.AbsoluteLeft;
        float originRight = origin.Visual.AbsoluteRight;
        float originTop = origin.Visual.AbsoluteTop;
        float originBottom = origin.Visual.AbsoluteBottom;

        float candidateLeft = candidate.Visual.AbsoluteLeft;
        float candidateRight = candidate.Visual.AbsoluteRight;
        float candidateTop = candidate.Visual.AbsoluteTop;
        float candidateBottom = candidate.Visual.AbsoluteBottom;

        float dx;
        if (candidateRight < originLeft)
        {
            dx = candidateRight - originLeft;
        }
        else if (candidateLeft > originRight)
        {
            dx = candidateLeft - originRight;
        }
        else
        {
            dx = 0f;
        }

        float dy;
        if (candidateBottom < originTop)
        {
            dy = candidateBottom - originTop;
        }
        else if (candidateTop > originBottom)
        {
            dy = candidateTop - originBottom;
        }
        else
        {
            dy = 0f;
        }

        return (dx, dy);
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
