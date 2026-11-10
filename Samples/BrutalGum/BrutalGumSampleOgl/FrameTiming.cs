// Per-frame CPU timing for the frame loop, written to a file when the run passes --timing <path>.
//   --vsync 0|1    swap interval (default 1, as shipped)
//   --gl-finish    calls glFinish after Draw and times it separately, so Swap no longer includes the GPU work Draw queued
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal sealed class FrameTiming
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void FinishFn();

    private const int WarmupFrames = 60;

    private readonly string? _path;
    private readonly FinishFn? _finish;
    private readonly List<double[]> _frames = new();
    private readonly Stopwatch _watch = new();
    private readonly double[] _current = new double[6];
    private long _frameStart;

    public static readonly string[] Columns = { "Update", "Draw", "Finish", "Swap", "Frame", "Clear" };

    public int SwapInterval { get; }

    public FrameTiming(string[] args, Func<string, IntPtr> getProc)
    {
        _path = Value(args, "--timing");
        SwapInterval = int.Parse(Value(args, "--vsync") ?? "1");
        if (args.Contains("--gl-finish"))
        {
            _finish = Marshal.GetDelegateForFunctionPointer<FinishFn>(getProc("glFinish"));
        }
        _renderer = Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<GetStringFn>(getProc("glGetString"))(0x1F01 /* GL_RENDERER */)) ?? "?";
        _watch.Start();
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate IntPtr GetStringFn(int name);
    private readonly string _renderer;

    public bool Enabled => _path != null;

    public void BeginFrame()
    {
        _frameStart = _watch.ElapsedTicks;
        Array.Clear(_current);
    }

    /// <summary>Runs <paramref name="action"/> and records its duration in column <paramref name="column"/>.</summary>
    public void Time(int column, Action action)
    {
        long start = _watch.ElapsedTicks;
        action();
        _current[column] = Ms(_watch.ElapsedTicks - start);
    }

    public void Finish()
    {
        if (_finish != null)
        {
            FinishFn finish = _finish;
            Time(2, () => finish());
        }
    }

    public void EndFrame()
    {
        _current[4] = Ms(_watch.ElapsedTicks - _frameStart);
        _frames.Add((double[])_current.Clone());
    }

    public void Write(string label)
    {
        if (_path == null) return;
        List<double[]> frames = _frames.Skip(WarmupFrames).ToList();
        StringBuilder text = new();
        text.AppendLine($"{label} vsync={SwapInterval} glFinish={_finish != null} frames={frames.Count} (after {WarmupFrames} warmup) renderer={_renderer}");
        for (int c = 0; c < Columns.Length; c++)
        {
            double[] values = frames.Select(f => f[c]).OrderBy(v => v).ToArray();
            if (values.Length == 0) continue;
            text.AppendLine($"  {Columns[c],-7} median {Percentile(values, 0.5),7:F3} ms  p95 {Percentile(values, 0.95),7:F3} ms  max {values[^1],7:F3} ms");
        }
        double medianFrame = Percentile(frames.Select(f => f[4]).OrderBy(v => v).ToArray(), 0.5);
        text.AppendLine($"  FPS (1000/median frame) {1000 / medianFrame:F1}");
        File.AppendAllText(_path, text.ToString());
    }

    private static double Percentile(double[] sorted, double p) =>
        sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)(p * sorted.Length))];

    private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private static string? Value(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
