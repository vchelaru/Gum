using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WpfDataUi.Controls;

/// <summary>
/// The value logic of an angle displayer: converting between the degrees it shows and the unit it
/// writes (<see cref="AngleType"/>), parsing typed text, and turning a drag around the dial into an
/// angle that keeps winding past 180 degrees.
/// </summary>
public class AngleSelectorLogic
{
    // atan2 only returns (-180, 180]; the dial must keep winding past those bounds, so deltas
    // (unwrapped at the seam) accumulate onto the value the drag started from.
    private double? _previousDialAtan2Degrees;
    private decimal _unsnappedDialAngle;

    /// <summary>Rounds <paramref name="valueToRound"/> half away from zero to a multiple of <paramref name="multipleOf"/>.</summary>
    public static decimal RoundDecimal(decimal valueToRound, decimal multipleOf)
    {
        return ((int)(Math.Sign(valueToRound) * .5m + valueToRound / multipleOf)) * multipleOf;
    }

    /// <summary>The member value for an angle shown in degrees: a float in <paramref name="type"/>'s unit, or null.</summary>
    public object? ToInstanceValue(decimal? angleDegrees, AngleType type)
    {
        if (angleDegrees == null)
        {
            return null;
        }

        return type == AngleType.Radians
            ? (float)(Math.PI * (double)angleDegrees.Value / 180.0f)
            : (float)angleDegrees.Value;
    }

    /// <summary>
    /// The degrees to show for a member value: floats and ints in <paramref name="type"/>'s unit, or
    /// null for null. Returns false for any other value type.
    /// </summary>
    public bool TryGetDisplayedDegrees(object? value, AngleType type, out float? degrees)
    {
        switch (value)
        {
            case float asFloat:
                degrees = type == AngleType.Radians ? 180 * (float)(asFloat / Math.PI) : asFloat;
                return true;
            case int asInt:
                degrees = type == AngleType.Radians ? 180 * (float)(asInt / Math.PI) : asInt;
                return true;
            case null:
                degrees = null;
                return true;
            default:
                degrees = null;
                return false;
        }
    }

    /// <summary>
    /// Parses typed angle text: empty is null, a number is itself, and anything else is evaluated as
    /// arithmetic ("90*2"), giving null when it is not arithmetic. Returns false only when evaluating
    /// the arithmetic fails, in which case the shown angle should stay as it was.
    /// </summary>
    public bool TryParseAngleText(string? text, Type propertyType, out float? angle)
    {
        if (string.IsNullOrEmpty(text))
        {
            angle = null;
            return true;
        }

        if (float.TryParse(text, out float value))
        {
            angle = value;
            return true;
        }

        try
        {
            angle = TextBoxDisplayLogic.TryHandleMathOperation(text, propertyType) as float?;
            return true;
        }
        catch
        {
            angle = null;
            return false;
        }
    }

    /// <summary>Starts a drag around the dial from the current angle.</summary>
    public void BeginDialDrag(decimal? currentAngle)
    {
        _previousDialAtan2Degrees = null;
        _unsnappedDialAngle = currentAngle ?? 0;
    }

    /// <summary>
    /// Moves the drag to a pointer at (<paramref name="x"/>, <paramref name="y"/>) relative to the
    /// dial's center, with y growing downward as on screen. The first sample jumps to the clicked
    /// angle; later samples add the change. Snaps to <paramref name="snappingInterval"/>, or at least
    /// 15 degrees while Shift is held. Returns false for a pointer exactly on the center.
    /// </summary>
    public bool TryDragTo(double x, double y, bool isShiftDown, decimal? snappingInterval, out decimal newAngle)
    {
        if (x == 0 && y == 0)
        {
            newAngle = _unsnappedDialAngle;
            return false;
        }

        double currentAtan2Degrees = 180.0 * (Math.Atan2(-y, x) / Math.PI);

        if (_previousDialAtan2Degrees == null)
        {
            _unsnappedDialAngle = (decimal)currentAtan2Degrees;
        }
        else
        {
            double delta = currentAtan2Degrees - _previousDialAtan2Degrees.Value;
            if (delta > 180)
            {
                delta -= 360;
            }
            else if (delta < -180)
            {
                delta += 360;
            }
            _unsnappedDialAngle += (decimal)delta;
        }
        _previousDialAtan2Degrees = currentAtan2Degrees;

        decimal? effectiveSnappingInterval = snappingInterval;
        if (isShiftDown && (effectiveSnappingInterval == null || effectiveSnappingInterval < 15))
        {
            effectiveSnappingInterval = 15;
        }

        newAngle = effectiveSnappingInterval != null
            ? RoundDecimal(_unsnappedDialAngle, effectiveSnappingInterval.Value)
            : _unsnappedDialAngle;
        return true;
    }
}

/// <summary>A list of strings edited as text, one entry per line.</summary>
public class StringListLogic
{
    /// <summary>The non-empty, trimmed lines of <paramref name="text"/> (\r\n or \n separated).</summary>
    public List<string> ParseLines(string? text)
    {
        text ??= string.Empty;
        string separator = text.Contains("\r\n") ? "\r\n" : "\n";
        return text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();
    }

    /// <summary>The entries joined with the platform line break.</summary>
    public string JoinLines(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

    /// <summary>The line of <paramref name="text"/> containing <paramref name="caretIndex"/>, without its line break.</summary>
    public string GetLineAt(string? text, int caretIndex)
    {
        text ??= string.Empty;
        caretIndex = Math.Clamp(caretIndex, 0, text.Length);
        int start = caretIndex == 0 ? 0 : text.LastIndexOf('\n', caretIndex - 1) + 1;
        int end = text.IndexOf('\n', caretIndex);
        if (end < 0)
        {
            end = text.Length;
        }
        return text.Substring(start, end - start).TrimEnd('\r');
    }
}

/// <summary>
/// The value logic of a list displayer for <c>List&lt;string&gt;</c>, <c>List&lt;int&gt;</c>,
/// <c>List&lt;float&gt;</c>, and <c>List&lt;Vector2&gt;</c>: copying the member's list for editing,
/// reading the shown items back, and adding or replacing an entry from typed text.
/// </summary>
public class ListBoxDisplayLogic
{
    /// <summary>The message shown when a Vector2 entry does not parse.</summary>
    public const string Vector2ParseError = "Could not parse the values. Value must be two numbers separated by a comma, such as \"10,20\"";

    /// <summary>
    /// A copy of <paramref name="value"/> to edit (or an empty list of <paramref name="propertyType"/>),
    /// so edits never touch the member's own list until they are committed.
    /// </summary>
    public IList? CreateEditableCopy(object? value, Type? propertyType)
    {
        switch (value)
        {
            case List<string> strings:
                return new List<string>(strings);
            case List<int> ints:
                return new List<int>(ints);
            case List<float> floats:
                return new List<float>(floats);
            case List<Vector2> vectors:
                return new List<Vector2>(vectors);
        }

        if (propertyType == typeof(List<string>)) return new List<string>();
        if (propertyType == typeof(List<int>)) return new List<int>();
        if (propertyType == typeof(List<float>)) return new List<float>();
        if (propertyType == typeof(List<Vector2>)) return new List<Vector2>();

        if (value is IList genericList)
        {
            IList? copy = Activator.CreateInstance(value.GetType()) as IList;
            if (copy != null)
            {
                foreach (object? item in genericList)
                {
                    copy.Add(item);
                }
            }
            return copy;
        }

        if (propertyType != null)
        {
            return Activator.CreateInstance(propertyType) as IList;
        }

        throw new InvalidOperationException(
            "Could not set UI value on a list displayer because the value is null and the InstanceMember does not specify a property type");
    }

    /// <summary>
    /// Reads the shown <paramref name="items"/> back as a new list of <paramref name="propertyType"/>,
    /// skipping entries that do not parse. Returns false for an unsupported list type.
    /// </summary>
    public bool TryReadList(IEnumerable items, Type? propertyType, out object? list)
    {
        if (propertyType == typeof(List<string>))
        {
            list = items.Cast<object?>().Select(item => item?.ToString()).Where(text => text != null).Cast<string>().ToList();
            return true;
        }
        if (propertyType == typeof(List<int>))
        {
            List<int> ints = new List<int>();
            foreach (object? item in items)
            {
                if (int.TryParse(item?.ToString(), out int value))
                {
                    ints.Add(value);
                }
            }
            list = ints;
            return true;
        }
        if (propertyType == typeof(List<float>))
        {
            List<float> floats = new List<float>();
            foreach (object? item in items)
            {
                if (float.TryParse(item?.ToString(), out float value))
                {
                    floats.Add(value);
                }
            }
            list = floats;
            return true;
        }
        if (propertyType == typeof(List<Vector2>))
        {
            List<Vector2> vectors = new List<Vector2>();
            foreach (object? item in items)
            {
                if (TryParseVector2(item?.ToString(), out Vector2? value))
                {
                    vectors.Add(value!.Value);
                }
            }
            list = vectors;
            return true;
        }

        list = null;
        return false;
    }

    /// <summary>
    /// Appends <paramref name="text"/> to <paramref name="list"/>, or replaces the entry at
    /// <paramref name="indexEditing"/>. Text that does not parse for the list's type is ignored;
    /// an unparseable Vector2 returns <see cref="Vector2ParseError"/>.
    /// </summary>
    public string? AddOrReplace(IList list, int? indexEditing, string text)
    {
        switch (list)
        {
            case List<string> strings:
                Set(strings, indexEditing, text);
                return null;
            case List<int> ints:
                if (int.TryParse(text, out int intValue))
                {
                    Set(ints, indexEditing, intValue);
                }
                return null;
            case List<float> floats:
                if (float.TryParse(text, out float floatValue))
                {
                    Set(floats, indexEditing, floatValue);
                }
                return null;
            case List<Vector2> vectors:
                if (TryParseVector2(text, out Vector2? vectorValue))
                {
                    Set(vectors, indexEditing, vectorValue!.Value);
                    return null;
                }
                return Vector2ParseError;
            default:
                return null;
        }
    }

    /// <summary>Parses "10,20" or "&lt;10,20&gt;" into a vector.</summary>
    public static bool TryParseVector2(string? text, out Vector2? parsedValue)
    {
        parsedValue = null;
        text = StripAngleBrackets(text);

        if (text?.Contains(",") == true)
        {
            string[] splitValues = text.Split(',');
            if (splitValues.Length == 2 &&
                float.TryParse(splitValues[0], out float firstValue) &&
                float.TryParse(splitValues[1], out float secondValue))
            {
                parsedValue = new Vector2(firstValue, secondValue);
            }
        }

        return parsedValue != null;
    }

    /// <summary>Removes a leading "&lt;" and trailing "&gt;", as shown for vectors.</summary>
    public static string? StripAngleBrackets(string? text)
    {
        if (text?.StartsWith("<") == true)
        {
            text = text.Substring(1);
        }
        if (text?.EndsWith(">") == true)
        {
            text = text.Substring(0, text.Length - 1);
        }
        return text;
    }

    private static void Set<T>(List<T> list, int? index, T value)
    {
        if (index == null)
        {
            list.Add(value);
        }
        else
        {
            list[index.Value] = value;
        }
    }
}

/// <summary>
/// The ordered file list behind a multi-file displayer: add, remove, and move entries, and which
/// buttons apply to a selection. Order matters (e.g. last-write-wins localization merge order).
/// </summary>
public class MultiFileDisplayLogic
{
    /// <summary>Creates an empty list.</summary>
    public MultiFileDisplayLogic()
    {
        Entries = new List<string>();
    }

    /// <summary>The files, in order.</summary>
    public List<string> Entries { get; }

    /// <summary>Replaces the entries with <paramref name="value"/> when it is a list of strings, else clears them.</summary>
    public void SetEntries(object? value)
    {
        Entries.Clear();
        if (value is List<string> incoming)
        {
            Entries.AddRange(incoming);
        }
    }

    /// <summary>A copy of the entries, the value written to the member.</summary>
    public List<string> GetValue() => new List<string>(Entries);

    /// <summary>Removes the entry at <paramref name="index"/>; false when it is out of range.</summary>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= Entries.Count)
        {
            return false;
        }
        Entries.RemoveAt(index);
        return true;
    }

    /// <summary>Moves the entry at <paramref name="index"/> by <paramref name="direction"/>; false when it cannot move.</summary>
    public bool Move(int index, int direction, out int newIndex)
    {
        newIndex = index + direction;
        if (index < 0 || index >= Entries.Count || newIndex < 0 || newIndex >= Entries.Count)
        {
            newIndex = index;
            return false;
        }

        string value = Entries[index];
        Entries.RemoveAt(index);
        Entries.Insert(newIndex, value);
        return true;
    }

    /// <summary>Whether Remove applies with <paramref name="selectedIndex"/> selected.</summary>
    public bool IsRemoveVisible(int selectedIndex) => selectedIndex >= 0;

    /// <summary>Whether Up and Down apply with <paramref name="selectedIndex"/> selected.</summary>
    public bool IsMoveVisible(int selectedIndex) => selectedIndex >= 0 && Entries.Count > 1;
}

/// <summary>
/// One button of a toggle-button option displayer: the text (shown and as a tooltip), the value it
/// selects, and optional icon names a head resolves to its own imagery.
/// </summary>
public class ToggleButtonOption
{
    /// <summary>Creates an option.</summary>
    public ToggleButtonOption(string name, object value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>The option's text.</summary>
    public string Name { get; }

    /// <summary>The value the option selects.</summary>
    public object Value { get; }

    /// <summary>A tool icon name (the <c>GumIcon</c> set), if the option has one.</summary>
    public string? GumIconName { get; init; }

    /// <summary>A legacy icon-template name, if the option has one.</summary>
    public string? IconName { get; init; }

    /// <summary>A path to an image, relative to the application folder, if the option has one.</summary>
    public string? ImagePath { get; init; }
}
