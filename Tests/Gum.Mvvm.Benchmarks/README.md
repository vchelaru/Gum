# Gum.Mvvm.Benchmarks

Baseline numbers for `Gum.Mvvm.ViewModel` property access (issue #6367, background in #6356). A console
app with its own fixed-iteration harness, not a test project, so `dotnet test` and the CI test slnf never
run it. It is built by `AllLibraries.sln` like `Gum.Layout.Benchmarks`.

## Run it

```
dotnet run -c Release --project Tests/Gum.Mvvm.Benchmarks -- --runs 15 --out results.md
```

Takes about 2 minutes. Options: `--quick` (a 30 second smoke run, noisy), `--runs <n>` (default 9),
`--type <name>` (only types whose name contains it, e.g. `float`), `--out <file>`. It refuses to run in
Debug or with a debugger attached. Close other work first; the numbers on a busy machine are noisy.

## What it measures

One `BenchViewModel` with 12 properties (string, `ObservableCollection<int>`, `int[]`, a class, int,
float, bool, double, an enum, an 8 B struct, a 32 B struct, and a 16 B struct without `IEquatable`) against a
`PlainModel` with the same auto-properties. Per type:

- Get, set to the same value, set to a new value (alternating between two values).
- Set to a new value with one `PropertyChanged` subscriber, and with a `[DependsOn]` dependent.
- `SetWithoutNotifying`.

Extras: `ChangeAndNotify` over a backing field as a second baseline for notifying sets, and a frame-style
read of 1,200 properties (100 models x 12).

Method: each case warms up, then takes the median of N timed runs of about 25 ms. Runs are interleaved
across cases (each case runs once per pass) so a burst of other activity does not ruin one case.
`TieredCompilation` is off so every method is fully optimized when first jitted. Plain baselines include
one store into a sink object per iteration so the JIT cannot delete the read, which is why a reference-type
plain get reads 2 ns and a value-type one 0.4 ns. Bytes come from `GC.GetAllocatedBytesForCurrentThread`.
There are no browser/WebAssembly numbers here; #6356 measured roughly 10 to 20 times these desktop costs.

## Baseline (before any optimization)

Date: 2026-10-21  Mode: full
Runtime: .NET 8.0.7, Release, X64
OS: Microsoft Windows 10.0.26200
CPU: Intel(R) Core(TM) i5-10210U CPU @ 1.60GHz, 8 logical cores
Each case: 100 ms warmup, median of 15 runs of about 25 ms, runs interleaved across cases.

ns/op (lower is better). Plain = auto-property. VM = Gum.Mvvm.ViewModel Get/Set.

| Type | Plain get | VM get | Plain set | VM set same | VM set new | VM set new + 1 subscriber | VM set new + [DependsOn] | VM SetWithoutNotifying |
|---|---|---|---|---|---|---|---|---|
| string | 2.4 | 29 | 3.4 | 50 | 111 | 153 | 173 | 95 |
| ObservableCollection<int> | 2.2 | 24 | 4.5 | 306 | 342 | 425 | 424 | 152 |
| int[] | 2.7 | 21 | 4.3 | 40 | 104 | 151 | 155 | 79 |
| class | 2.3 | 27 | 4.5 | 44 | 98 | 137 | 167 | 85 |
| int | 0.4 | 22 | 1.9 | 27 | 104 | 149 | 173 | 98 |
| float | 0.4 | 22 | 1.7 | 27 | 100 | 145 | 149 | 109 |
| bool | 0.4 | 19 | 1.8 | 22 | 71 | 116 | 132 | 69 |
| double | 0.4 | 22 | 1.8 | 26 | 107 | 161 | 161 | 99 |
| enum | 0.4 | 21 | 2.0 | 23 | 82 | 137 | 144 | 75 |
| struct 8 B | 0.4 | 20 | 1.0 | 28 | 84 | 126 | 140 | 75 |
| struct 32 B | 0.6 | 23 | 1.2 | 23 | 93 | 123 | 134 | 85 |
| struct 16 B, no IEquatable | 0.4 | 24 | 1.0 | 581 | 473 | 495 | 537 | 446 |

Bytes allocated per op, VM cases (every plain case allocates 0 B except where listed below).

| Type | VM get | VM set same | VM set new | VM set new + 1 subscriber | VM set new + [DependsOn] | VM SetWithoutNotifying |
|---|---|---|---|---|---|---|
| string | 0 | 0 | 0 | 0 | 0 | 0 |
| ObservableCollection<int> | 0 | 224 | 112 | 104 | 104 | 0 |
| int[] | 0 | 0 | 0 | 0 | 0 | 0 |
| class | 0 | 0 | 0 | 0 | 0 | 0 |
| int | 0 | 0 | 24 | 24 | 24 | 24 |
| float | 0 | 0 | 24 | 24 | 24 | 24 |
| bool | 0 | 0 | 24 | 24 | 24 | 24 |
| double | 0 | 0 | 24 | 24 | 24 | 24 |
| enum | 0 | 0 | 24 | 24 | 24 | 24 |
| struct 8 B | 0 | 0 | 24 | 24 | 24 | 24 |
| struct 32 B | 0 | 0 | 48 | 48 | 48 | 48 |
| struct 16 B, no IEquatable | 0 | 200 | 184 | 184 | 184 | 184 |

Plain cases allocated 0 B/op.

Baseline for notifying sets without the dictionary: ChangeAndNotify(ref field, value), the existing API, set new value.

| Type | No subscriber ns (B) | 1 subscriber ns (B) | VM no subscriber ns (B) | VM 1 subscriber ns (B) |
|---|---|---|---|---|
| float | 8.3 (0) | 37 (0) | 100 (24) | 145 (24) |
| string | 13 (0) | 42 (0) | 111 (0) | 153 (0) |

Frame-style read: 1200 property reads per frame (100 models x 12 properties, mixed types).

| | ns per read | microseconds per frame | B per frame |
|---|---|---|---|
| Plain | 0.46 | 0.6 | 0 |
| VM | 25.34 | 30.4 | 0 |

Noise: this run was on a busy laptop; the middle half of the runs typically spans 30-40% of the median, so read the ratios between rows, not the last digit.

## Observations

- A get is about 20 ns against 0.4 ns for a value-type auto-property (a dictionary lookup plus an unbox); it allocates nothing.
- A new-value set of a value type allocates the box (24 B, 48 B for the 32 B struct); reference types do not, except collections.
- A struct without `IEquatable<T>` allocates about 200 B even for a same-value set (`ValueType.Equals` boxing), and costs 500 ns or more.
- `ObservableCollection` sets allocate 100 to 220 B and cost 300 ns or more (the collection-changed unsubscribe and subscribe).
