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

    /// <summary>Zero of the same kind and component count as <paramref name="like"/>.</summary>
    public static ShaderParameterValue ZeroLike(ShaderParameterValue like)
    {
        if (like.Kind == ShaderParameterKind.Int) return FromInt(0);
        if (like.Kind == ShaderParameterKind.Bool) return FromBool(false);
        switch (like.Count)
        {
            case 1: return FromFloat(0f);
            case 2: return FromVector2(0f, 0f);
            case 3: return FromVector3(0f, 0f, 0f);
            default: return FromVector4(0f, 0f, 0f, 0f);
        }
    }

    /// <summary>Creates a four component float vector, which is also how a color is passed.</summary>
    public static ShaderParameterValue FromVector4(float x, float y, float z, float w) =>
        new ShaderParameterValue(ShaderParameterKind.Float, 4, x, y, z, w);
}

/// <summary>
/// A parameter a shader declares: its name, scalar kind, number of components and the value the
/// shader starts with. The tool lists these so each one can be edited.
/// </summary>
public sealed class ShaderParameterInfo
{
    /// <summary>The parameter's name in the shader.</summary>
    public string Name { get; }

    /// <summary>The scalar type shared by every component.</summary>
    public ShaderParameterKind Kind { get; }

    /// <summary>The number of components, from 1 (a scalar) to 4.</summary>
    public int Count { get; }

    /// <summary>The value the shader uses until one is set.</summary>
    public ShaderParameterValue DefaultValue { get; }

    /// <summary>Creates a description of one declared parameter.</summary>
    public ShaderParameterInfo(string name, ShaderParameterKind kind, int count, ShaderParameterValue defaultValue)
    {
        Name = name;
        Kind = kind;
        Count = count;
        DefaultValue = defaultValue;
    }
}

/// <summary>
/// Lists the parameters of a platform effect for the tool. The effect type differs per backend, so the
/// tool registers a <see cref="Reader"/> for the one it draws with.
/// </summary>
public static class ShaderParameterReader
{
    /// <summary>Reads the parameters of the platform effect it was registered for, or null for any other object.</summary>
    public static System.Func<object, System.Collections.Generic.IReadOnlyList<ShaderParameterInfo>>? Reader { get; set; }

    /// <summary>The parameters <paramref name="effect"/> declares, or none when there is no effect or no reader.</summary>
    public static System.Collections.Generic.IReadOnlyList<ShaderParameterInfo> Read(object? effect)
    {
        if (effect == null || Reader == null)
        {
            return new List<ShaderParameterInfo>();
        }
        return Reader(effect);
    }
}

/// <summary>
/// Named shader parameter values for one effect owner. Names share a single namespace across all
/// kinds, and a backend applies the values to the effect just before it draws.
/// </summary>
public class ShaderParameterCollection
{
    /// <summary>
    /// The name of a <c>float2</c> parameter every backend fills in with one pixel of the render
    /// target as a fraction (1 / width, 1 / height) when the effect declares it and nothing was set
    /// for it, so a shader can step whole pixels without the host passing a size.
    /// </summary>
    public const string TexelSizeName = "TexelSize";

    private readonly Dictionary<string, ShaderParameterValue> _values;

    /// <summary>Creates an empty collection.</summary>
    public ShaderParameterCollection()
    {
        _values = new Dictionary<string, ShaderParameterValue>();
    }

    private const string IsSetSuffix = "IsSet";
    private static readonly Dictionary<string, string> _isSetNames = new Dictionary<string, string>();

    /// <summary>
    /// The name of the companion <c>float</c> parameter a backend sets to 1 whenever the user set
    /// <paramref name="name"/> (for example <c>ThicknessIsSet</c> for <c>Thickness</c>), so a shader can
    /// tell an unset parameter from one explicitly set to zero. Skipped when the shader does not declare it.
    /// </summary>
    public static string GetIsSetName(string name)
    {
        lock (_isSetNames)
        {
            if (!_isSetNames.TryGetValue(name, out string? isSetName))
            {
                isSetName = name + IsSetSuffix;
                _isSetNames[name] = isSetName;
            }
            return isSetName;
        }
    }

    /// <summary>Whether <paramref name="name"/> is a companion set by <see cref="GetIsSetName"/>.</summary>
    public static bool IsIsSetName(string name) => name.EndsWith(IsSetSuffix, System.StringComparison.Ordinal);

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

/// <summary>
/// Remembers which parameter values Gum last wrote to one shared effect or shader, so the next
/// container that draws with it can put back the ones it did not set itself. A backend that can read a
/// parameter back (MonoGame, KNI) records its value from just before Gum first wrote it as the baseline,
/// and that is what goes back; a name with no baseline (raylib cannot read one) goes back to zero. A value the
/// application wrote to the effect directly before Gum's first write of that name is captured as the baseline.
/// Values the application wrote to the effect directly are not in the history, so they are left alone, except
/// that once Gum has written a name, a later container that does not set it restores the baseline even if the
/// application wrote that name in between.
/// </summary>
public sealed class ShaderParameterHistory
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, ShaderParameterHistory> _histories =
        new System.Runtime.CompilerServices.ConditionalWeakTable<object, ShaderParameterHistory>();

    private readonly Dictionary<string, ShaderParameterValue> _applied = new Dictionary<string, ShaderParameterValue>();
    private readonly Dictionary<string, ShaderParameterValue> _baselines = new Dictionary<string, ShaderParameterValue>();
    private readonly List<KeyValuePair<string, ShaderParameterValue>> _stale = new List<KeyValuePair<string, ShaderParameterValue>>();

    /// <summary>The history for <paramref name="effect"/> (a MonoGame Effect or a raylib shader, boxed once per call site).</summary>
    public static ShaderParameterHistory For(object effect) => _histories.GetValue(effect, _ => new ShaderParameterHistory());

    /// <summary>Notes that <paramref name="name"/> was written with <paramref name="value"/>.</summary>
    public void Record(string name, ShaderParameterValue value) => _applied[name] = value;

    /// <summary>Whether a baseline was stored for <paramref name="name"/>.</summary>
    public bool HasBaseline(string name) => _baselines.ContainsKey(name);

    /// <summary>
    /// Stores <paramref name="value"/>, which <paramref name="name"/> held before Gum first wrote it, as the value
    /// to restore. Only the first call for a name counts, so a later write is never mistaken for the original.
    /// </summary>
    public void SetBaseline(string name, ShaderParameterValue value)
    {
        if (!_baselines.ContainsKey(name))
        {
            _baselines[name] = value;
        }
    }

    /// <summary>
    /// The value to put back on <paramref name="name"/> when a container does not set it: its baseline, or
    /// zero shaped like <paramref name="lastWritten"/> when no baseline was stored.
    /// </summary>
    public ShaderParameterValue GetRestoreValue(string name, ShaderParameterValue lastWritten) =>
        _baselines.TryGetValue(name, out ShaderParameterValue baseline) ? baseline : ShaderParameterValue.ZeroLike(lastWritten);

    /// <summary>
    /// Returns, and forgets, the values written earlier for names not in <paramref name="current"/>.
    /// The returned list is reused by the next call.
    /// </summary>
    public List<KeyValuePair<string, ShaderParameterValue>> TakeStale(ShaderParameterCollection? current)
    {
        _stale.Clear();
        foreach (KeyValuePair<string, ShaderParameterValue> pair in _applied)
        {
            if (current == null || !current.TryGetValue(pair.Key, out _))
            {
                _stale.Add(pair);
            }
        }
        foreach (KeyValuePair<string, ShaderParameterValue> pair in _stale)
        {
            _applied.Remove(pair.Key);
        }
        return _stale;
    }
}

/// <summary>
/// The value a shader declares for one uniform (<c>float Thickness = 1.0;</c>), for a backend that
/// cannot read it back from the compiled shader (SkiaSharp and raylib). The values are in the layout
/// ShadowDusk reports: array element after array element, each element's components in order.
/// </summary>
public sealed class ShaderUniformDefault
{
    /// <summary>The uniform's name in the shader.</summary>
    public string Name { get; }

    /// <summary>Float, Int or Bool (Bool stands for an unsigned or bool uniform).</summary>
    public ShaderParameterKind Kind { get; }

    /// <summary>Components per element, from 1 (a scalar) to 4.</summary>
    public int ComponentCount { get; }

    /// <summary>The array length, or 0 when the uniform is not an array.</summary>
    public int ArrayLength { get; }

    /// <summary>True for a matrix uniform, whose values are the matrix cells in row-major order.</summary>
    public bool IsMatrix { get; }

    /// <summary>The default, <see cref="ComponentCount"/> times the larger of 1 and <see cref="ArrayLength"/> values.</summary>
    public float[] Values { get; }

    /// <summary>Creates a declared default.</summary>
    public ShaderUniformDefault(string name, ShaderParameterKind kind, int componentCount, int arrayLength, float[] values, bool isMatrix = false)
    {
        IsMatrix = isMatrix;
        Name = name;
        Kind = kind;
        ComponentCount = componentCount;
        ArrayLength = arrayLength;
        Values = values;
    }

    /// <summary>
    /// Creates a default from a converted shader's uniform type name (<c>float</c>, <c>vec3</c>, <c>ivec2</c>,
    /// <c>uint</c>, <c>int2</c>, ...). Returns null when <paramref name="values"/> is null (no declared default).
    /// </summary>
    public static ShaderUniformDefault? FromTypeName(string name, string typeName, int arrayLength, System.Collections.Generic.IReadOnlyList<float>? values)
    {
        if (values == null)
        {
            return null;
        }

        ShaderParameterKind kind = ShaderParameterKind.Float;
        if (typeName.StartsWith("uint", System.StringComparison.Ordinal) || typeName.StartsWith("uvec", System.StringComparison.Ordinal) ||
            typeName.StartsWith("bool", System.StringComparison.Ordinal) || typeName.StartsWith("bvec", System.StringComparison.Ordinal))
        {
            kind = ShaderParameterKind.Bool;
        }
        else if (typeName.StartsWith("int", System.StringComparison.Ordinal) || typeName.StartsWith("ivec", System.StringComparison.Ordinal))
        {
            kind = ShaderParameterKind.Int;
        }

        char last = typeName[typeName.Length - 1];
        bool isMatrix = typeName.StartsWith("mat", System.StringComparison.Ordinal) || typeName.Contains("x");
        int components = last >= '2' && last <= '4' && !isMatrix ? last - '0' : 1;

        float[] copy = new float[values.Count];
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = values[i];
        }
        return new ShaderUniformDefault(name, kind, components, arrayLength, copy, isMatrix);
    }
}

/// <summary>The declared defaults of one shader, by uniform name.</summary>
public sealed class ShaderUniformDefaults : IEnumerable<ShaderUniformDefault>
{
    private readonly Dictionary<string, ShaderUniformDefault> _defaults = new Dictionary<string, ShaderUniformDefault>();

    /// <summary>The number of uniforms with a default.</summary>
    public int Count => _defaults.Count;

    /// <summary>Adds or replaces the default for its uniform.</summary>
    public void Add(ShaderUniformDefault value) => _defaults[value.Name] = value;

    /// <summary>Gets the default declared for <paramref name="name"/>.</summary>
    public bool TryGetValue(string name, out ShaderUniformDefault? value) => _defaults.TryGetValue(name, out value);

    /// <inheritdoc/>
    public IEnumerator<ShaderUniformDefault> GetEnumerator() => _defaults.Values.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
