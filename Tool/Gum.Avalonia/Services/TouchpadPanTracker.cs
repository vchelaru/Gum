using System;
using System.Collections.Generic;
using Avalonia;

namespace Gum.Avalonia.Services;

/// <summary>A finger touching a precision touchpad, positioned in millimeters from the pad's corner.</summary>
public readonly record struct TouchpadContact(int Id, double XMm, double YMm);

/// <summary>One finger slot of a touchpad HID report, which may be unused (not touching).</summary>
public readonly record struct TouchpadSlot(int ContactId, bool IsTouching, double XMm, double YMm);

/// <summary>
/// Assembles a precision touchpad's HID reports into frames of the fingers touching it. In parallel
/// mode one report holds every finger; in hybrid mode a frame spans several reports and only its
/// first carries the contact count. Either way, only the first contact-count slots are real.
/// </summary>
public sealed class TouchpadFrameAssembler
{
    private List<TouchpadContact> _frame = new List<TouchpadContact>();
    private int _remaining;

    /// <summary>Adds one report; returns the touching fingers once the report completes a frame.</summary>
    public IReadOnlyList<TouchpadContact>? AddReport(int contactCount, IReadOnlyList<TouchpadSlot> slots)
    {
        if (contactCount > 0)
        {
            _frame = new List<TouchpadContact>();
            _remaining = contactCount;
        }
        else if (_remaining == 0)
        {
            return null;
        }

        int count = Math.Min(_remaining, slots.Count);
        for (int i = 0; i < count; i++)
        {
            TouchpadSlot slot = slots[i];
            if (slot.IsTouching)
            {
                _frame.Add(new TouchpadContact(slot.ContactId, slot.XMm, slot.YMm));
            }
        }
        _remaining -= count;
        return _remaining == 0 ? _frame : null;
    }
}

/// <summary>
/// Turns a two-finger touchpad drag into canvas pan. Windows locks its wheel messages for a touchpad
/// scroll to one axis until the swipe strays far enough (about a second of a diagonal swipe), so the
/// pan comes from the fingers' own movement, and the wheel events only say when to apply it and
/// which way the user's scroll-direction setting points.
/// </summary>
public sealed class TouchpadPanTracker
{
    /// <summary>How far the canvas moves, in device-independent pixels, per millimeter of finger travel.</summary>
    public const double PixelsPerMillimeter = 8;

    // Finger travel below this can't reliably tell the scroll direction from noise.
    private const double MinimumDirectionTravelMm = 0.5;

    private IReadOnlyList<TouchpadContact>? _twoFingers;
    private Vector _pendingMm;
    // +1 when the content follows the fingers (Windows' default for touchpads), -1 when reversed.
    private int _direction = 1;

    /// <summary>Records a frame of the fingers on the pad.</summary>
    public void OnFrame(IReadOnlyList<TouchpadContact> contacts)
    {
        if (contacts.Count == 2 && _twoFingers != null && HaveSameIds(_twoFingers, contacts))
        {
            _pendingMm += Centroid(contacts) - Centroid(_twoFingers);
        }
        else
        {
            _pendingMm = default;
        }
        _twoFingers = contacts.Count == 2 ? contacts : null;
    }

    /// <summary>
    /// Returns the pan, in device-independent pixels, the fingers moved since the last call, or
    /// null when two fingers aren't on the pad. <paramref name="wheelDelta"/> is the wheel event
    /// carrying it, whose sign relative to the fingers gives the scroll direction.
    /// </summary>
    public Vector? TakePan(Vector wheelDelta)
    {
        if (_twoFingers == null)
        {
            return null;
        }

        if (wheelDelta.Y != 0 && Math.Abs(_pendingMm.Y) >= MinimumDirectionTravelMm)
        {
            _direction = Math.Sign(wheelDelta.Y) * Math.Sign(_pendingMm.Y);
        }
        else if (wheelDelta.X != 0 && Math.Abs(_pendingMm.X) >= MinimumDirectionTravelMm)
        {
            _direction = Math.Sign(wheelDelta.X) * Math.Sign(_pendingMm.X);
        }

        Vector pan = _pendingMm * (PixelsPerMillimeter * _direction);
        _pendingMm = default;
        return pan;
    }

    /// <summary>Drops finger travel not yet taken, such as a pinch's, so it doesn't pan later.</summary>
    public void Discard() => _pendingMm = default;

    private static Vector Centroid(IReadOnlyList<TouchpadContact> contacts) =>
        new Vector((contacts[0].XMm + contacts[1].XMm) / 2, (contacts[0].YMm + contacts[1].YMm) / 2);

    private static bool HaveSameIds(IReadOnlyList<TouchpadContact> a, IReadOnlyList<TouchpadContact> b) =>
        (a[0].Id == b[0].Id && a[1].Id == b[1].Id) || (a[0].Id == b[1].Id && a[1].Id == b[0].Id);
}
