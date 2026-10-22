using Gum.Mvvm;

namespace Gum.Mvvm.Benchmarks;

// The field style (#6356). One property per view model, generic over T, so one set of classes covers
// every benchmarked type.

/// <summary><c>T _v; Value { get => _v; set => Set(ref _v, value); }</c> with the name passed by [CallerMemberName].</summary>
public class RefViewModel<T> : ViewModel
{
    private T _v;

    public RefViewModel(T initial) => _v = initial;

    public T Value { get => _v; set => Set(ref _v, value); }

    public bool SetQuiet(T value) => SetWithoutNotifying(ref _v, value, nameof(Value));
}

/// <summary>One [DependsOn] dependent, declared by name like every other style.</summary>
public sealed class RefViewModelWithDependent<T> : RefViewModel<T>
{
    public RefViewModelWithDependent(T initial) : base(initial) { }

    [DependsOn(nameof(Value))] public int Dependent => 0;
}

public readonly struct RefAccessor<T>(RefViewModel<T> m) : IAccessor<T>
{
    public T Get() => m.Value;
    public void Set(T value) => m.Value = value;
    public bool SetQuiet(T value) => m.SetQuiet(value);
}
