using System.Collections.ObjectModel;

namespace Gum.Mvvm.Benchmarks;

/// <summary>
/// Reads and writes one property. Implemented by structs and used as a generic argument, so the JIT
/// specializes each loop and inlines the accessor: the plain-property rows measure the property, not a
/// delegate or interface call.
/// </summary>
public interface IAccessor<T>
{
    T Get();
    void Set(T value);

    /// <summary>The SetWithoutNotifying path. Plain and field-backed accessors just store.</summary>
    bool SetQuiet(T value);
}

// View model accessors (Get/Set backed by the property dictionary).
public readonly struct VmText(BenchViewModel m) : IAccessor<string>
{
    public string Get() => m.TextValue;
    public void Set(string value) => m.TextValue = value;
    public bool SetQuiet(string value) => m.SetTextQuiet(value);
}

public readonly struct VmItems(BenchViewModel m) : IAccessor<ObservableCollection<int>>
{
    public ObservableCollection<int> Get() => m.ItemsValue;
    public void Set(ObservableCollection<int> value) => m.ItemsValue = value;
    public bool SetQuiet(ObservableCollection<int> value) => m.SetItemsQuiet(value);
}

public readonly struct VmArray(BenchViewModel m) : IAccessor<int[]>
{
    public int[] Get() => m.ArrayValue;
    public void Set(int[] value) => m.ArrayValue = value;
    public bool SetQuiet(int[] value) => m.SetArrayQuiet(value);
}

public readonly struct VmReference(BenchViewModel m) : IAccessor<Payload>
{
    public Payload Get() => m.ReferenceValue;
    public void Set(Payload value) => m.ReferenceValue = value;
    public bool SetQuiet(Payload value) => m.SetReferenceQuiet(value);
}

public readonly struct VmInt(BenchViewModel m) : IAccessor<int>
{
    public int Get() => m.IntValue;
    public void Set(int value) => m.IntValue = value;
    public bool SetQuiet(int value) => m.SetIntQuiet(value);
}

public readonly struct VmFloat(BenchViewModel m) : IAccessor<float>
{
    public float Get() => m.FloatValue;
    public void Set(float value) => m.FloatValue = value;
    public bool SetQuiet(float value) => m.SetFloatQuiet(value);
}

public readonly struct VmBool(BenchViewModel m) : IAccessor<bool>
{
    public bool Get() => m.BoolValue;
    public void Set(bool value) => m.BoolValue = value;
    public bool SetQuiet(bool value) => m.SetBoolQuiet(value);
}

public readonly struct VmDouble(BenchViewModel m) : IAccessor<double>
{
    public double Get() => m.DoubleValue;
    public void Set(double value) => m.DoubleValue = value;
    public bool SetQuiet(double value) => m.SetDoubleQuiet(value);
}

public readonly struct VmEnum(BenchViewModel m) : IAccessor<BenchEnum>
{
    public BenchEnum Get() => m.EnumValue;
    public void Set(BenchEnum value) => m.EnumValue = value;
    public bool SetQuiet(BenchEnum value) => m.SetEnumQuiet(value);
}

public readonly struct VmSmall(BenchViewModel m) : IAccessor<SmallStruct>
{
    public SmallStruct Get() => m.SmallValue;
    public void Set(SmallStruct value) => m.SmallValue = value;
    public bool SetQuiet(SmallStruct value) => m.SetSmallQuiet(value);
}

public readonly struct VmLarge(BenchViewModel m) : IAccessor<LargeStruct>
{
    public LargeStruct Get() => m.LargeValue;
    public void Set(LargeStruct value) => m.LargeValue = value;
    public bool SetQuiet(LargeStruct value) => m.SetLargeQuiet(value);
}

public readonly struct VmUnequatable(BenchViewModel m) : IAccessor<UnequatableStruct>
{
    public UnequatableStruct Get() => m.UnequatableValue;
    public void Set(UnequatableStruct value) => m.UnequatableValue = value;
    public bool SetQuiet(UnequatableStruct value) => m.SetUnequatableQuiet(value);
}

// Plain auto-property accessors.
public readonly struct PlainText(PlainModel m) : IAccessor<string>
{
    public string Get() => m.TextValue;
    public void Set(string value) => m.TextValue = value;
    public bool SetQuiet(string value) { m.TextValue = value; return true; }
}

public readonly struct PlainItems(PlainModel m) : IAccessor<ObservableCollection<int>>
{
    public ObservableCollection<int> Get() => m.ItemsValue;
    public void Set(ObservableCollection<int> value) => m.ItemsValue = value;
    public bool SetQuiet(ObservableCollection<int> value) { m.ItemsValue = value; return true; }
}

public readonly struct PlainArray(PlainModel m) : IAccessor<int[]>
{
    public int[] Get() => m.ArrayValue;
    public void Set(int[] value) => m.ArrayValue = value;
    public bool SetQuiet(int[] value) { m.ArrayValue = value; return true; }
}

public readonly struct PlainReference(PlainModel m) : IAccessor<Payload>
{
    public Payload Get() => m.ReferenceValue;
    public void Set(Payload value) => m.ReferenceValue = value;
    public bool SetQuiet(Payload value) { m.ReferenceValue = value; return true; }
}

public readonly struct PlainInt(PlainModel m) : IAccessor<int>
{
    public int Get() => m.IntValue;
    public void Set(int value) => m.IntValue = value;
    public bool SetQuiet(int value) { m.IntValue = value; return true; }
}

public readonly struct PlainFloat(PlainModel m) : IAccessor<float>
{
    public float Get() => m.FloatValue;
    public void Set(float value) => m.FloatValue = value;
    public bool SetQuiet(float value) { m.FloatValue = value; return true; }
}

public readonly struct PlainBool(PlainModel m) : IAccessor<bool>
{
    public bool Get() => m.BoolValue;
    public void Set(bool value) => m.BoolValue = value;
    public bool SetQuiet(bool value) { m.BoolValue = value; return true; }
}

public readonly struct PlainDouble(PlainModel m) : IAccessor<double>
{
    public double Get() => m.DoubleValue;
    public void Set(double value) => m.DoubleValue = value;
    public bool SetQuiet(double value) { m.DoubleValue = value; return true; }
}

public readonly struct PlainEnum(PlainModel m) : IAccessor<BenchEnum>
{
    public BenchEnum Get() => m.EnumValue;
    public void Set(BenchEnum value) => m.EnumValue = value;
    public bool SetQuiet(BenchEnum value) { m.EnumValue = value; return true; }
}

public readonly struct PlainSmall(PlainModel m) : IAccessor<SmallStruct>
{
    public SmallStruct Get() => m.SmallValue;
    public void Set(SmallStruct value) => m.SmallValue = value;
    public bool SetQuiet(SmallStruct value) { m.SmallValue = value; return true; }
}

public readonly struct PlainLarge(PlainModel m) : IAccessor<LargeStruct>
{
    public LargeStruct Get() => m.LargeValue;
    public void Set(LargeStruct value) => m.LargeValue = value;
    public bool SetQuiet(LargeStruct value) { m.LargeValue = value; return true; }
}

public readonly struct PlainUnequatable(PlainModel m) : IAccessor<UnequatableStruct>
{
    public UnequatableStruct Get() => m.UnequatableValue;
    public void Set(UnequatableStruct value) => m.UnequatableValue = value;
    public bool SetQuiet(UnequatableStruct value) { m.UnequatableValue = value; return true; }
}

// ChangeAndNotify (field-backed) accessors.
public readonly struct FieldFloat(FieldBackedViewModel m) : IAccessor<float>
{
    public float Get() => m.FloatValue;
    public void Set(float value) => m.FloatValue = value;
    public bool SetQuiet(float value) { m.FloatValue = value; return true; }
}

public readonly struct FieldText(FieldBackedViewModel m) : IAccessor<string>
{
    public string Get() => m.TextValue;
    public void Set(string value) => m.TextValue = value;
    public bool SetQuiet(string value) { m.TextValue = value; return true; }
}
