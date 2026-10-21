using Gum.Mvvm;

namespace Gum.Mvvm.Benchmarks;

// Spike #6397: the field style. One property per view model, generic over T, so one set of classes covers
// every benchmarked type. The generic class shares code for reference types the same way the real,
// many-property view models do (Set<T> is a shared generic method either way).

/// <summary><c>T _v; Value { get => _v; set => Set(ref _v, value); }</c> with the name passed by [CallerMemberName].</summary>
public class RefVm<T> : ViewModel
{
    private T _v;

    public RefVm(T initial) => _v = initial;

    public T Value { get => _v; set => Set(ref _v, value); }

    public bool SetQuiet(T value) => SetWithoutNotifying(ref _v, value, nameof(Value));
}

/// <summary>One [DependsOn] dependent, declared by name like every other style.</summary>
public sealed class RefVmWithDependent<T> : RefVm<T>
{
    public RefVmWithDependent(T initial) : base(initial) { }

    [DependsOn(nameof(Value))] public int Dependent => 0;
}

/// <summary>Field style plus a static key, so the cached PropertyChangedEventArgs is reused (no name hash on a notifying set).</summary>
public class RefKeyVm<T> : ViewModel
{
    private static readonly ViewModelProperty<T> ValueKey = new("Value");
    private T _v;

    public RefKeyVm(T initial) => _v = initial;

    public T Value { get => _v; set => Set(ref _v, value, ValueKey); }

    public bool SetQuiet(T value) => SetWithoutNotifying(ref _v, value, nameof(Value));
}

public sealed class RefKeyVmWithDependent<T> : RefKeyVm<T>
{
    public RefKeyVmWithDependent(T initial) : base(initial) { }

    [DependsOn("Value")] public int Dependent => 0;
}

/// <summary>Per-instance property object: <c>_v.Value</c>.</summary>
public class ObjVm<T> : ViewModel
{
    private readonly ViewModelField<T> _v;

    public ObjVm(T initial) => _v = new ViewModelField<T>(this, nameof(Value), initial);

    public T Value { get => _v.Value; set => _v.Value = value; }

    public bool SetQuiet(T value) => _v.SetWithoutNotifying(value);
}

public sealed class ObjVmWithDependent<T> : ObjVm<T>
{
    public ObjVmWithDependent(T initial) : base(initial) { }

    [DependsOn(nameof(Value))] public int Dependent => 0;
}

public readonly struct RefAccessor<T>(RefVm<T> m) : IAccessor<T>
{
    public T Get() => m.Value;
    public void Set(T value) => m.Value = value;
    public bool SetQuiet(T value) => m.SetQuiet(value);
}

public readonly struct RefKeyAccessor<T>(RefKeyVm<T> m) : IAccessor<T>
{
    public T Get() => m.Value;
    public void Set(T value) => m.Value = value;
    public bool SetQuiet(T value) => m.SetQuiet(value);
}

public readonly struct ObjAccessor<T>(ObjVm<T> m) : IAccessor<T>
{
    public T Get() => m.Value;
    public void Set(T value) => m.Value = value;
    public bool SetQuiet(T value) => m.SetQuiet(value);
}
