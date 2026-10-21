using System.Reflection;
using System.Text;

namespace Gum.Mvvm.Benchmarks;

/// <summary>
/// Counts string hashes and dictionary operations per call, the proxy for browser (WebAssembly) cost where
/// hashing a property name is slow. The ViewModel's private dictionaries are swapped (by reflection) for
/// copies that use a counting comparer, so the numbers come from the real code path, not from reading it.
/// A "hash" is one IEqualityComparer.GetHashCode call; an "op" is one dictionary lookup or store that
/// reached the comparer. The static PropertyChangedEventArgs cache is a ConcurrentDictionary that cannot be
/// swapped, so its one string hash per delivered notification is added by hand (see Notes in the report).
/// </summary>
public static class Counting
{
    private sealed class CountingComparer : IEqualityComparer<string>
    {
        public int Hashes;
        public int Equality;
        public bool Equals(string? x, string? y) { Equality++; return string.Equals(x, y); }
        public int GetHashCode(string s) { Hashes++; return s.GetHashCode(); }
    }

    private static CountingComparer Instrument(ViewModel vm)
    {
        CountingComparer comparer = new();
        foreach (string fieldName in new[] { "propertyDictionary", "notifyRelationships" })
        {
            FieldInfo field = typeof(ViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;
            object dictionary = field.GetValue(vm)!;
            Type[] args = dictionary.GetType().GetGenericArguments();
            Type counted = typeof(Dictionary<,>).MakeGenericType(args);
            object copy = Activator.CreateInstance(counted, dictionary, comparer)!;
            field.SetValue(vm, copy);
        }
        return comparer;
    }

    public static string Run()
    {
        StringBuilder sb = new();
        sb.AppendLine("Per call: string hashes in the ViewModel's own dictionaries (static event-args cache added separately).");
        sb.AppendLine();
        sb.AppendLine("| Case | string hashes | notes |");
        sb.AppendLine("|---|---|---|");

        void Report(string name, Func<ViewModel> make, Action<ViewModel, int> body, bool subscribe, bool keyedStyle = false)
        {
            ViewModel vm = make();
            Subscriber? sub = null;
            if (subscribe)
            {
                sub = new Subscriber();
                vm.PropertyChanged += sub.OnPropertyChanged;
            }
            CountingComparer comparer = Instrument(vm);
            body(vm, 1);
            comparer.Hashes = 0;
            const int n = 1000;
            body(vm, n);
            double perCall = comparer.Hashes / (double)n;
            // ConcurrentDictionary<string,...>.GetOrAdd in NotifyPropertyChanged: one hash per delivered
            // notification when a PropertyChanged subscriber exists and the call is not keyed.
            double eventArgs = subscribe && !keyedStyle ? 1 : 0;
            sb.AppendLine($"| {name} | {perCall + eventArgs:0.##} | dictionary {perCall:0.##}" + (eventArgs > 0 ? " + event-args cache 1" : "") + " |");
        }

        BenchViewModel Plain() => new();
        BenchViewModel Dep() => new BenchViewModelWithDependents();

        Report("old get int", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) _ = m.IntValue; }, false);
        Report("old set same int", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 1; }, false);
        Report("old set new int", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, false);
        Report("old set new int + subscriber", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, true);
        Report("old set new int + [DependsOn]", Dep, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, false);
        Report("old get string", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) _ = m.TextValue; }, false);
        Report("old set same string", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = Values.TextA; }, false);
        Report("old set new string", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, false);
        Report("old set new string + subscriber", Plain, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, true);
        Report("old set new string + [DependsOn]", Dep, (v, n) => { BenchViewModel m = (BenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, false);
#if KEYED
        Report("keyed get int", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) _ = m.IntValue; }, false, true);
        Report("keyed set same int", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 1; }, false, true);
        Report("keyed set new int", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, false, true);
        Report("keyed set new int + subscriber", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, true, true);
        Report("keyed set new int + [DependsOn]", () => new KeyedBenchViewModelWithDependents(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.IntValue = 2 + (i & 1); }, false, true);
        Report("keyed get string", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) _ = m.TextValue; }, false, true);
        Report("keyed set new string", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, false, true);
        Report("keyed set new string + subscriber", () => new KeyedBenchViewModel(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, true, true);
        Report("keyed set new string + [DependsOn]", () => new KeyedBenchViewModelWithDependents(), (v, n) => { KeyedBenchViewModel m = (KeyedBenchViewModel)v; for (int i = 0; i < n; i++) m.TextValue = (i & 1) == 0 ? Values.TextB : Values.TextA; }, false, true);
#endif
#if REFFIELD
        void Styles<T>(string type, T a, T b)
        {
            Report($"ref get {type}", () => new RefVm<T>(a), (v, n) => { var m = (RefVm<T>)v; for (int i = 0; i < n; i++) _ = m.Value; }, false, true);
            Report($"ref set same {type}", () => new RefVm<T>(a), (v, n) => { var m = (RefVm<T>)v; for (int i = 0; i < n; i++) m.Value = a; }, false, true);
            Report($"ref set new {type}", () => new RefVm<T>(a), (v, n) => { var m = (RefVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, false, true);
            // Name-only Set(ref): the static event-args cache hashes the name once per delivered notification.
            Report($"ref set new {type} + subscriber", () => new RefVm<T>(a), (v, n) => { var m = (RefVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, true);
            Report($"ref set new {type} + [DependsOn]", () => new RefVmWithDependent<T>(a), (v, n) => { var m = (RefVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, false, true);
            Report($"ref+key set new {type} + subscriber", () => new RefKeyVm<T>(a), (v, n) => { var m = (RefKeyVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, true, true);
            Report($"ref+key set new {type} + [DependsOn]", () => new RefKeyVmWithDependent<T>(a), (v, n) => { var m = (RefKeyVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, false, true);
            Report($"obj get {type}", () => new ObjVm<T>(a), (v, n) => { var m = (ObjVm<T>)v; for (int i = 0; i < n; i++) _ = m.Value; }, false, true);
            Report($"obj set new {type} + subscriber", () => new ObjVm<T>(a), (v, n) => { var m = (ObjVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, true, true);
            Report($"obj set new {type} + [DependsOn]", () => new ObjVmWithDependent<T>(a), (v, n) => { var m = (ObjVm<T>)v; for (int i = 0; i < n; i++) m.Value = (i & 1) == 0 ? b : a; }, false, true);
        }

        Styles("int", 1, 2);
        Styles("string", Values.TextA, Values.TextB);
#endif
        return sb.ToString();
    }
}
