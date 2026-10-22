using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gum.TieringRepro;

/// <summary>
/// Child process: times the body of MathFunctions.RoundToInt(float), old and new, with and without a
/// preceding block zero (initblk) that leaves the upper vector registers dirty. Prints one MICRO line per
/// case with the best of several rounds, in ns per call.
/// </summary>
internal static class Micro
{
#pragma warning disable CS0649 // Only ever zeroed; the fields give the struct its 64-byte size.
    private struct Buf64
    {
        public long A, B, C, D, E, F, G, H;
    }
#pragma warning restore CS0649

    private const int CallsPerBlock = 20_000;
    private const int BlocksPerRound = 50;
    private const int Rounds = 9;

    private static Buf64 s_buffer;
    private static int s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitBlk(ref Buf64 buffer) => buffer = default;

    // MathFunctions.RoundToInt(float) as it is today (RenderingLibrary/Math/MathFunctions.cs).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Old(float x)
    {
        if (float.IsNaN(x))
        {
            throw new ArgumentException("floatToRound cannot be NaN");
        }
        return (int)(Math.Round(x, MidpointRounding.AwayFromZero) + (Math.Sign(x) * .5f));
    }

    // The proposed body.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int New(float x)
    {
        if (float.IsNaN(x))
        {
            throw new ArgumentException("floatToRound cannot be NaN");
        }
        return (int)MathF.Round(x, MidpointRounding.AwayFromZero);
    }

    private static int BlockInitOnly(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            InitBlk(ref s_buffer);
            sum += i;
        }
        return sum;
    }

    private static int BlockOldClean(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += Old(i * 0.37f);
        }
        return sum;
    }

    private static int BlockOldDirty(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            InitBlk(ref s_buffer);
            sum += Old(i * 0.37f);
        }
        return sum;
    }

    private static int BlockNewClean(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += New(i * 0.37f);
        }
        return sum;
    }

    private static int BlockNewDirty(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            InitBlk(ref s_buffer);
            sum += New(i * 0.37f);
        }
        return sum;
    }

    private static double TimeNanosecondsPerCall(Func<int, int> block)
    {
        long start = Stopwatch.GetTimestamp();
        int sum = 0;
        for (int i = 0; i < BlocksPerRound; i++)
        {
            sum += block(CallsPerBlock);
        }
        long elapsed = Stopwatch.GetTimestamp() - start;
        s_sink += sum;
        return elapsed * 1e9 / Stopwatch.Frequency / ((double)CallsPerBlock * BlocksPerRound);
    }

    public static int Run()
    {
        (string Name, Func<int, int> Block)[] cases =
        {
            ("init", BlockInitOnly),
            ("old-clean", BlockOldClean),
            ("old-dirty", BlockOldDirty),
            ("new-clean", BlockNewClean),
            ("new-dirty", BlockNewDirty),
        };
        double[] best = new double[cases.Length];
        Array.Fill(best, double.MaxValue);

        // Give tiered compilation (when it is on) time to promote everything before timing.
        long warmupEnd = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 600 / 1000;
        while (Stopwatch.GetTimestamp() < warmupEnd)
        {
            foreach ((string _, Func<int, int> block) in cases)
            {
                TimeNanosecondsPerCall(block);
            }
        }

        for (int round = 0; round < Rounds; round++)
        {
            // Alternate the order every round so no case always runs first or last.
            for (int k = 0; k < cases.Length; k++)
            {
                int index = round % 2 == 0 ? k : cases.Length - 1 - k;
                best[index] = Math.Min(best[index], TimeNanosecondsPerCall(cases[index].Block));
            }
        }

        for (int k = 0; k < cases.Length; k++)
        {
            Console.WriteLine($"MICRO\t{cases[k].Name}\t{best[k]:F3}");
        }

        // The two bodies must agree on the inputs used here, or the timing compares different work.
        int oldSum = BlockOldClean(CallsPerBlock);
        int newSum = BlockNewClean(CallsPerBlock);
        Console.WriteLine($"SAME\t{(oldSum == newSum ? 1 : 0)}\t{s_sink & 1}");
        return 0;
    }
}
