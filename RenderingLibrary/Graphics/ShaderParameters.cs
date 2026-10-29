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
public readonly struct ShaderParameterValue : System.IEquatable<ShaderParameterValue>
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

    /// <summary>Whether <paramref name="other"/> has the same kind, component count and components.</summary>
    public bool Equals(ShaderParameterValue other) =>
        Kind == other.Kind && Count == other.Count &&
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
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
/// parameter back (MonoGame, KNI) gives the history what the effect holds just before each Gum write
/// (<see cref="ObserveLive"/>) and just after it (<see cref="RecordHeld"/>). The first value seen is the baseline
/// that goes back; a name with no baseline (raylib cannot read one) goes back to zero. When the value seen before
/// a write differs from the one Gum left, the application wrote that name in between, and its value becomes the
/// new baseline. A name Gum never wrote is left alone.
/// </summary>
public sealed class ShaderParameterHistory
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, ShaderParameterHistory> _histories =
        new System.Runtime.CompilerServices.ConditionalWeakTable<object, ShaderParameterHistory>();

    private readonly Dictionary<string, ShaderParameterValue> _applied = new Dictionary<string, ShaderParameterValue>();
    private readonly Dictionary<string, ShaderParameterValue> _baselines = new Dictionary<string, ShaderParameterValue>();
    private readonly Dictionary<string, ShaderParameterValue> _held = new Dictionary<string, ShaderParameterValue>();
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
    /// Tells the history what the effect holds for <paramref name="name"/> right now, just before Gum writes it.
    /// The first value seen becomes the baseline. After that, a value that differs from the one
    /// <see cref="RecordHeld"/> saw means the application changed it since Gum's last write, so that value
    /// replaces the baseline.
    /// </summary>
    public void ObserveLive(string name, ShaderParameterValue live)
    {
        if (!_baselines.ContainsKey(name))
        {
            SetBaseline(name, live);
        }
        else if (_held.TryGetValue(name, out ShaderParameterValue held) && !held.Equals(live))
        {
            _baselines[name] = live;
        }
    }

    /// <summary>
    /// Notes what the effect holds for <paramref name="name"/> just after Gum wrote it. Read back rather than taken
    /// from what was written, because the effect may store it differently (an int truncates, a bool is 0 or 1).
    /// </summary>
    public void RecordHeld(string name, ShaderParameterValue held) => _held[name] = held;

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
