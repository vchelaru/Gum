#if KEYED
using System.Collections.ObjectModel;
using Gum.Mvvm;

namespace Gum.Mvvm.Benchmarks;

/// <summary>
/// The same properties as <see cref="BenchViewModel"/> written in the keyed style: one static readonly
/// <see cref="ViewModelProperty{T}"/> per property, passed to Get and Set.
/// </summary>
public class KeyedBenchViewModel : ViewModel
{
    private static readonly ViewModelProperty<string> TextKey = new(nameof(TextValue));
    private static readonly ViewModelProperty<ObservableCollection<int>> ItemsKey = new(nameof(ItemsValue));
    private static readonly ViewModelProperty<int[]> ArrayKey = new(nameof(ArrayValue));
    private static readonly ViewModelProperty<Payload> ReferenceKey = new(nameof(ReferenceValue));
    private static readonly ViewModelProperty<int> IntKey = new(nameof(IntValue));
    private static readonly ViewModelProperty<float> FloatKey = new(nameof(FloatValue));
    private static readonly ViewModelProperty<bool> BoolKey = new(nameof(BoolValue));
    private static readonly ViewModelProperty<double> DoubleKey = new(nameof(DoubleValue));
    private static readonly ViewModelProperty<BenchEnum> EnumKey = new(nameof(EnumValue));
    private static readonly ViewModelProperty<SmallStruct> SmallKey = new(nameof(SmallValue));
    private static readonly ViewModelProperty<LargeStruct> LargeKey = new(nameof(LargeValue));
    private static readonly ViewModelProperty<UnequatableStruct> UnequatableKey = new(nameof(UnequatableValue));

    public static ViewModelProperty<T> KeyFor<T>(string type) => (ViewModelProperty<T>)(type switch
    {
        "string" => TextKey,
        "ObservableCollection<int>" => ItemsKey,
        "int[]" => ArrayKey,
        "class" => ReferenceKey,
        "int" => IntKey,
        "float" => FloatKey,
        "bool" => BoolKey,
        "double" => DoubleKey,
        "enum" => EnumKey,
        "struct 8 B" => SmallKey,
        "struct 32 B" => LargeKey,
        _ => (object)UnequatableKey,
    });

    public string TextValue { get => GetValue(TextKey); set => SetValue(TextKey, value); }
    public ObservableCollection<int> ItemsValue { get => GetValue(ItemsKey); set => SetValue(ItemsKey, value); }
    public int[] ArrayValue { get => GetValue(ArrayKey); set => SetValue(ArrayKey, value); }
    public Payload ReferenceValue { get => GetValue(ReferenceKey); set => SetValue(ReferenceKey, value); }
    public int IntValue { get => GetValue(IntKey); set => SetValue(IntKey, value); }
    public float FloatValue { get => GetValue(FloatKey); set => SetValue(FloatKey, value); }
    public bool BoolValue { get => GetValue(BoolKey); set => SetValue(BoolKey, value); }
    public double DoubleValue { get => GetValue(DoubleKey); set => SetValue(DoubleKey, value); }
    public BenchEnum EnumValue { get => GetValue(EnumKey); set => SetValue(EnumKey, value); }
    public SmallStruct SmallValue { get => GetValue(SmallKey); set => SetValue(SmallKey, value); }
    public LargeStruct LargeValue { get => GetValue(LargeKey); set => SetValue(LargeKey, value); }
    public UnequatableStruct UnequatableValue { get => GetValue(UnequatableKey); set => SetValue(UnequatableKey, value); }

    public T GetByKey<T>(ViewModelProperty<T> key) => GetValue(key);
    public void SetByKey<T>(ViewModelProperty<T> key, T value) => SetValue(key, value);
    public bool SetQuietByKey<T>(ViewModelProperty<T> key, T value) => SetValueWithoutNotifying(key, value);

    public KeyedBenchViewModel()
    {
        TextValue = Values.TextA;
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
}

/// <summary>Keyed properties with one [DependsOn] dependent each; dependents are still declared by name.</summary>
public sealed class KeyedBenchViewModelWithDependents : KeyedBenchViewModel
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

public readonly struct KeyedAccessor<T>(KeyedBenchViewModel m, ViewModelProperty<T> key) : IAccessor<T>
{
    public T Get() => m.GetByKey(key);
    public void Set(T value) => m.SetByKey(key, value);
    public bool SetQuiet(T value) => m.SetQuietByKey(key, value);
}
#endif
