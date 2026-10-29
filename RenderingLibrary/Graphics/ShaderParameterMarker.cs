using System;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Says which shader parameter a saved variable holds, in the text stored in
/// <c>VariableSave.ShaderParameter</c>: <c>Thickness</c> for a scalar, or <c>OutlineColor#1/4</c> for
/// component 1 (zero based) of a four component vector parameter. A variable without the text is not a
/// shader variable.
/// </summary>
/// <remarks>
/// The marker is saved next to the variable name because the display name, the component index and the
/// component count must be known without taking apart a name whose shader part has been sanitized (see
/// <see cref="ShaderVariableNaming"/>). The count lets a headless check notice a partly saved vector
/// without compiling the shader. This type is the only place the text format is written or read.
/// </remarks>
public readonly struct ShaderParameterMarker : IEquatable<ShaderParameterMarker>
{
    /// <summary>The fewest components a vector parameter has (a float2).</summary>
    public const int MinimumComponentCount = 2;

    /// <summary>The most components a vector parameter has (a float4).</summary>
    public const int MaximumComponentCount = 4;

    private readonly string? _parameter;
    private readonly int _componentIndex;
    private readonly int _componentCount;

    /// <summary>Creates the marker of a scalar parameter.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="parameter"/> is not a supported name; see <see cref="ShaderVariableNaming.IsSupportedParameterName"/>.
    /// </exception>
    public ShaderParameterMarker(string parameter)
    {
        ThrowIfUnsupported(parameter);
        _parameter = parameter;
        _componentIndex = 0;
        _componentCount = 0;
    }

    /// <summary>Creates the marker of one component of a vector parameter.</summary>
    /// <param name="parameter">The parameter name as declared in the shader.</param>
    /// <param name="componentIndex">The zero based component, 0 for X up to 3 for W.</param>
    /// <param name="componentCount">How many components the vector has, 2 to 4.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="parameter"/> is not a supported name; see <see cref="ShaderVariableNaming.IsSupportedParameterName"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is not 2 to 4, or the index is outside the count.</exception>
    public ShaderParameterMarker(string parameter, int componentIndex, int componentCount)
    {
        ThrowIfUnsupported(parameter);
        if (componentCount < MinimumComponentCount || componentCount > MaximumComponentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(componentCount), componentCount,
                "A vector parameter has " + MinimumComponentCount + " to " + MaximumComponentCount + " components.");
        }
        if (componentIndex < 0 || componentIndex >= componentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(componentIndex), componentIndex,
                "The component index must be at least 0 and less than the component count (" + componentCount + ").");
        }

        _parameter = parameter;
        _componentIndex = componentIndex;
        _componentCount = componentCount;
    }

    /// <summary>The parameter name as declared in the shader. Empty only for <c>default</c>.</summary>
    public string Parameter => _parameter ?? "";

    /// <summary>Whether this marks one component of a vector parameter rather than a scalar.</summary>
    public bool IsComponent => _componentCount > 0;

    /// <summary>The zero based component, or -1 for a scalar.</summary>
    public int ComponentIndex => IsComponent ? _componentIndex : -1;

    /// <summary>How many components the vector has, or 0 for a scalar.</summary>
    public int ComponentCount => _componentCount;

    /// <summary>The text saved in <c>VariableSave.ShaderParameter</c>.</summary>
    public string Format()
    {
        if (!IsComponent)
        {
            return Parameter;
        }
        return Parameter + "#" + _componentIndex.ToString() + "/" + _componentCount.ToString();
    }

    /// <summary>
    /// Reads the text saved in <c>VariableSave.ShaderParameter</c>. Returns false, with a default marker,
    /// for null, empty or malformed text; never throws.
    /// </summary>
    public static bool TryParse(string? text, out ShaderParameterMarker marker)
    {
        marker = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string markerText = text!;
        int hashIndex = markerText.IndexOf('#');
        if (hashIndex < 0)
        {
            if (!ShaderVariableNaming.IsSupportedParameterName(markerText))
            {
                return false;
            }
            marker = new ShaderParameterMarker(markerText);
            return true;
        }

        string parameter = markerText.Substring(0, hashIndex);
        string component = markerText.Substring(hashIndex + 1);
        int slashIndex = component.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == component.Length - 1)
        {
            return false;
        }

        if (!ShaderVariableNaming.IsSupportedParameterName(parameter)
            || !TryParseNonNegative(component.Substring(0, slashIndex), out int index)
            || !TryParseNonNegative(component.Substring(slashIndex + 1), out int count)
            || count < MinimumComponentCount
            || count > MaximumComponentCount
            || index >= count)
        {
            return false;
        }

        marker = new ShaderParameterMarker(parameter, index, count);
        return true;
    }

    public bool Equals(ShaderParameterMarker other) =>
        string.Equals(Parameter, other.Parameter, StringComparison.Ordinal)
        && ComponentIndex == other.ComponentIndex
        && _componentCount == other._componentCount;

    public override bool Equals(object? obj) => obj is ShaderParameterMarker other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(Parameter);
            hash = (hash * 397) ^ ComponentIndex;
            hash = (hash * 397) ^ _componentCount;
            return hash;
        }
    }

    public override string ToString() => Format();

    public static bool operator ==(ShaderParameterMarker left, ShaderParameterMarker right) => left.Equals(right);

    public static bool operator !=(ShaderParameterMarker left, ShaderParameterMarker right) => !left.Equals(right);

    private static void ThrowIfUnsupported(string parameter)
    {
        if (!ShaderVariableNaming.IsSupportedParameterName(parameter))
        {
            throw new ArgumentException(
                "'" + parameter + "' cannot be saved as a shader variable: a shader parameter name must be " +
                "letters, digits and single underscores only.", nameof(parameter));
        }
    }

    private static bool TryParseNonNegative(string text, out int value)
    {
        value = 0;
        // Plain digits only: no sign, no spaces, no culture-specific digits, and short enough not to overflow.
        if (text.Length == 0 || text.Length > 3)
        {
            return false;
        }
        foreach (char character in text)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
            value = (value * 10) + (character - '0');
        }
        return true;
    }
}
