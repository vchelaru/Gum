using System.Collections.ObjectModel;
using Gum.Mvvm;

namespace Gum.Mvvm.Benchmarks;

public enum BenchEnum { First, Second, Third }

/// <summary>8 bytes, implements IEquatable like Vector2 or Color do.</summary>
public readonly struct SmallStruct : IEquatable<SmallStruct>
{
    public readonly int X;
    public readonly int Y;

    public SmallStruct(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(SmallStruct other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is SmallStruct other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

/// <summary>32 bytes, implements IEquatable.</summary>
public readonly struct LargeStruct : IEquatable<LargeStruct>
{
    public readonly double A;
    public readonly double B;
    public readonly double C;
    public readonly double D;

    public LargeStruct(double a, double b, double c, double d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public bool Equals(LargeStruct other) => A == other.A && B == other.B && C == other.C && D == other.D;
    public override bool Equals(object? obj) => obj is LargeStruct other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B, C, D);
}

/// <summary>16 bytes, does not implement IEquatable, so EqualityComparer falls back to ValueType.Equals.</summary>
public readonly struct UnequatableStruct
{
    public readonly double A;
    public readonly double B;

    public UnequatableStruct(double a, double b)
    {
        A = a;
        B = b;
    }
}

public sealed class Payload
{
    public int Value;
    public Payload(int value) => Value = value;
}

/// <summary>The two values each benchmarked type alternates between.</summary>
public static class Values
{
    public static readonly string TextA = "alpha";
    public static readonly string TextB = "beta";
    public static readonly ObservableCollection<int> ItemsA = new() { 1, 2, 3 };
    public static readonly ObservableCollection<int> ItemsB = new() { 4, 5, 6 };
    public static readonly int[] ArrayA = { 1, 2, 3 };
    public static readonly int[] ArrayB = { 4, 5, 6 };
    public static readonly Payload ReferenceA = new(1);
    public static readonly Payload ReferenceB = new(2);
    public static readonly SmallStruct SmallA = new(1, 2);
    public static readonly SmallStruct SmallB = new(3, 4);
    public static readonly LargeStruct LargeA = new(1, 2, 3, 4);
    public static readonly LargeStruct LargeB = new(5, 6, 7, 8);
    public static readonly UnequatableStruct UnequatableA = new(1, 2);
    public static readonly UnequatableStruct UnequatableB = new(3, 4);
}

/// <summary>A view model holding a mix of reference and value type properties, all backed by Get/Set.</summary>
public class BenchViewModel : ViewModel
{
    public string TextValue { get => Get<string>(); set => Set(value); }
    public ObservableCollection<int> ItemsValue { get => Get<ObservableCollection<int>>(); set => Set(value); }
    public int[] ArrayValue { get => Get<int[]>(); set => Set(value); }
    public Payload ReferenceValue { get => Get<Payload>(); set => Set(value); }
    public int IntValue { get => Get<int>(); set => Set(value); }
    public float FloatValue { get => Get<float>(); set => Set(value); }
    public bool BoolValue { get => Get<bool>(); set => Set(value); }
    public double DoubleValue { get => Get<double>(); set => Set(value); }
    public BenchEnum EnumValue { get => Get<BenchEnum>(); set => Set(value); }
    public SmallStruct SmallValue { get => Get<SmallStruct>(); set => Set(value); }
    public LargeStruct LargeValue { get => Get<LargeStruct>(); set => Set(value); }
    public UnequatableStruct UnequatableValue { get => Get<UnequatableStruct>(); set => Set(value); }

    public BenchViewModel()
    {
        TextValue = Values.TextA;
        // Own collection: a shared one would collect a subscription from every view model instance.
        ItemsValue = new ObservableCollection<int>();
        ArrayValue = Values.ArrayA;
        ReferenceValue = Values.ReferenceA;
        IntValue = 1;
        FloatValue = 1f;
        BoolValue = true;
        DoubleValue = 1d;
        EnumValue = BenchEnum.First;
        SmallValue = Values.SmallA;
        LargeValue = Values.LargeA;
        UnequatableValue = Values.UnequatableA;
    }

    public bool SetTextQuiet(string v) => SetWithoutNotifying(v, nameof(TextValue));
    public bool SetItemsQuiet(ObservableCollection<int> v) => SetWithoutNotifying(v, nameof(ItemsValue));
    public bool SetArrayQuiet(int[] v) => SetWithoutNotifying(v, nameof(ArrayValue));
    public bool SetReferenceQuiet(Payload v) => SetWithoutNotifying(v, nameof(ReferenceValue));
    public bool SetIntQuiet(int v) => SetWithoutNotifying(v, nameof(IntValue));
    public bool SetFloatQuiet(float v) => SetWithoutNotifying(v, nameof(FloatValue));
    public bool SetBoolQuiet(bool v) => SetWithoutNotifying(v, nameof(BoolValue));
    public bool SetDoubleQuiet(double v) => SetWithoutNotifying(v, nameof(DoubleValue));
    public bool SetEnumQuiet(BenchEnum v) => SetWithoutNotifying(v, nameof(EnumValue));
    public bool SetSmallQuiet(SmallStruct v) => SetWithoutNotifying(v, nameof(SmallValue));
    public bool SetLargeQuiet(LargeStruct v) => SetWithoutNotifying(v, nameof(LargeValue));
    public bool SetUnequatableQuiet(UnequatableStruct v) => SetWithoutNotifying(v, nameof(UnequatableValue));
}

/// <summary>Same properties, each with one [DependsOn] dependent that is notified when its parent changes.</summary>
public sealed class BenchViewModelWithDependents : BenchViewModel
{
    [DependsOn(nameof(TextValue))] public int TextDependent => 0;
    [DependsOn(nameof(ItemsValue))] public int ItemsDependent => 0;
    [DependsOn(nameof(ArrayValue))] public int ArrayDependent => 0;
    [DependsOn(nameof(ReferenceValue))] public int ReferenceDependent => 0;
    [DependsOn(nameof(IntValue))] public int IntDependent => 0;
    [DependsOn(nameof(FloatValue))] public int FloatDependent => 0;
    [DependsOn(nameof(BoolValue))] public int BoolDependent => 0;
    [DependsOn(nameof(DoubleValue))] public int DoubleDependent => 0;
    [DependsOn(nameof(EnumValue))] public int EnumDependent => 0;
    [DependsOn(nameof(SmallValue))] public int SmallDependent => 0;
    [DependsOn(nameof(LargeValue))] public int LargeDependent => 0;
    [DependsOn(nameof(UnequatableValue))] public int UnequatableDependent => 0;
}

/// <summary>The baseline: the same properties as plain auto-properties.</summary>
public sealed class PlainModel
{
    public string TextValue { get; set; } = Values.TextA;
    public ObservableCollection<int> ItemsValue { get; set; } = new();
    public int[] ArrayValue { get; set; } = Values.ArrayA;
    public Payload ReferenceValue { get; set; } = Values.ReferenceA;
    public int IntValue { get; set; } = 1;
    public float FloatValue { get; set; } = 1f;
    public bool BoolValue { get; set; } = true;
    public double DoubleValue { get; set; } = 1d;
    public BenchEnum EnumValue { get; set; } = BenchEnum.First;
    public SmallStruct SmallValue { get; set; } = Values.SmallA;
    public LargeStruct LargeValue { get; set; } = Values.LargeA;
    public UnequatableStruct UnequatableValue { get; set; } = Values.UnequatableA;
}

/// <summary>
/// A second baseline for notifying sets: the existing <c>ChangeAndNotify</c> API over a backing field,
/// which raises PropertyChanged without the property dictionary.
/// </summary>
public sealed class FieldBackedViewModel : ViewModel
{
    private float _floatValue = 1f;
    private string _textValue = Values.TextA;

    public float FloatValue
    {
        get => _floatValue;
        set => ChangeAndNotify(ref _floatValue, value);
    }

    public string TextValue
    {
        get => _textValue;
        set => ChangeAndNotify(ref _textValue, value);
    }
}
