using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// The scalar type of an <see cref="ShaderParameterValue"/>. A backend needs it because it cannot
/// always read the declared uniform type from the shader (raylib cannot), and int, bool and float
/// are different calls.
/// </summary>
public enum ShaderParameterKind
{
    /// <summary>A float, or a float vector of <see cref="ShaderParameterValue.Count"/> components.</summary>
    Float,
    /// <summary>An int, or an int vector of <see cref="ShaderParameterValue.Count"/> components.</summary>
    Int,
    /// <summary>A bool, stored as 1 or 0 in <see cref="ShaderParameterValue.X"/>.</summary>
    Bool,
}

/// <summary>
/// One shader parameter value: up to four components of one <see cref="ShaderParameterKind"/>,
/// stored inline so setting a parameter every frame does not allocate. Create one with the
/// <c>From*</c> methods.
/// </summary>
public readonly struct ShaderParameterValue
{
    /// <summary>The scalar type shared by every component.</summary>
    public ShaderParameterKind Kind { get; }

    /// <summary>The number of components in use, from 1 (a scalar) to 4.</summary>
    public int Count { get; }

    /// <summary>The first component. Int and bool values are stored as floats here.</summary>
    public float X { get; }

    /// <summary>The second component, or 0 when <see cref="Count"/> is below 2.</summary>
    public float Y { get; }

    /// <summary>The third component, or 0 when <see cref="Count"/> is below 3.</summary>
    public float Z { get; }

    /// <summary>The fourth component, or 0 when <see cref="Count"/> is below 4.</summary>
    public float W { get; }

    private ShaderParameterValue(ShaderParameterKind kind, int count, float x, float y, float z, float w)
    {
        Kind = kind;
        Count = count;
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>Creates a single float value.</summary>
    public static ShaderParameterValue FromFloat(float x) =>
        new ShaderParameterValue(ShaderParameterKind.Float, 1, x, 0, 0, 0);

    /// <summary>Creates a single int value.</summary>
    public static ShaderParameterValue FromInt(int x) =>
        new ShaderParameterValue(ShaderParameterKind.Int, 1, x, 0, 0, 0);

    /// <summary>Creates a bool value.</summary>
    public static ShaderParameterValue FromBool(bool x) =>
        new ShaderParameterValue(ShaderParameterKind.Bool, 1, x ? 1f : 0f, 0, 0, 0);

    /// <summary>Creates a two component float vector.</summary>
    public static ShaderParameterValue FromVector2(float x, float y) =>
        new ShaderParameterValue(ShaderParameterKind.Float, 2, x, y, 0, 0);

    /// <summary>Creates a three component float vector.</summary>
    public static ShaderParameterValue FromVector3(float x, float y, float z) =>
        new ShaderParameterValue(ShaderParameterKind.Float, 3, x, y, z, 0);

    /// <summary>Creates a four component float vector, which is also how a color is passed.</summary>
    public static ShaderParameterValue FromVector4(float x, float y, float z, float w) =>
        new ShaderParameterValue(ShaderParameterKind.Float, 4, x, y, z, w);
}

/// <summary>
/// Named shader parameter values for one effect owner. Names share a single namespace across all
/// kinds, and a backend applies the values to the effect just before it draws.
/// </summary>
public class ShaderParameterCollection
{
    private readonly Dictionary<string, ShaderParameterValue> _values;

    /// <summary>Creates an empty collection.</summary>
    public ShaderParameterCollection()
    {
        _values = new Dictionary<string, ShaderParameterValue>();
    }

    /// <summary>The number of parameters set.</summary>
    public int Count => _values.Count;

    /// <summary>
    /// Sets <paramref name="name"/> to <paramref name="value"/>, replacing any earlier value of any
    /// kind. Replacing an existing name does not allocate.
    /// </summary>
    public void Set(string name, ShaderParameterValue value)
    {
        _values[name] = value;
    }

    /// <summary>Gets the value of <paramref name="name"/>, returning false when it was never set.</summary>
    public bool TryGetValue(string name, out ShaderParameterValue value) =>
        _values.TryGetValue(name, out value);

    /// <summary>Removes <paramref name="name"/>, returning false when it was not set.</summary>
    public bool Remove(string name) => _values.Remove(name);

    /// <summary>Removes every parameter.</summary>
    public void Clear() => _values.Clear();

    /// <summary>
    /// Enumerates the parameters without allocating. Do not add or remove entries while enumerating.
    /// </summary>
    public Dictionary<string, ShaderParameterValue>.Enumerator GetEnumerator() => _values.GetEnumerator();
}
