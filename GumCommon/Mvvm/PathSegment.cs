using System;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Mvvm;
#endif

/// <summary>
/// Represents a single segment of a binding path, consisting of a property/field name
/// and an optional integer index (e.g. "Items[0]" has Name="Items", Index=0).
/// </summary>
public readonly struct PathSegment
{
    /// <summary>
    /// The property or field name represented by this segment.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The integer index, if this segment includes indexer access (e.g. "Items[0]").
    /// Null when the segment is a simple property/field access.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Creates a new <see cref="PathSegment"/>.
    /// </summary>
    public PathSegment(string name, int? index = null)
    {
        Name = name;
        Index = index;
    }
}

/// <summary>
/// Helpers for parsing binding paths into <see cref="PathSegment"/> arrays.
/// </summary>
public static class PathSegmentParser
{
    /// <summary>
    /// Parses a binding path string into an array of <see cref="PathSegment"/> values.
    /// Supports dotted property access and integer indexers (e.g. "Items[0].Text").
    /// </summary>
    public static PathSegment[] ParseSegments(string path)
    {
        string[] dotParts = path.Split('.');
        PathSegment[] result = new PathSegment[dotParts.Length];

        for (int i = 0; i < dotParts.Length; i++)
        {
            string part = dotParts[i];
            int bracketStart = part.IndexOf('[');
            if (bracketStart >= 0)
            {
                int bracketEnd = part.IndexOf(']', bracketStart);
                if (bracketEnd < 0)
                {
                    throw new FormatException($"Missing closing bracket in path segment '{part}'.");
                }

                string name = part.Substring(0, bracketStart);
                string indexStr = part.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                int index = int.Parse(indexStr);
                result[i] = new PathSegment(name, index);
            }
            else
            {
                result[i] = new PathSegment(part);
            }
        }

        return result;
    }
}
