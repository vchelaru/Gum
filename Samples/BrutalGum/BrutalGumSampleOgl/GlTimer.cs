using System.Runtime.InteropServices;
using GumSamples.Benchmark;

/// <summary>GPU time of the work between Begin and End, from an OpenGL GL_TIME_ELAPSED query (core in OpenGL 3.3).</summary>
sealed unsafe class GlTimer : IGpuTimer
{
    const int GL_TIME_ELAPSED = 0x88BF;
    const int GL_QUERY_RESULT = 0x8866;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void GenQueries(int n, uint* ids);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void BeginQuery(int target, uint id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void EndQuery(int target);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void GetQueryObjectUi64(uint id, int pname, ulong* result);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void Finish();

    private readonly BeginQuery _begin;
    private readonly EndQuery _end;
    private readonly GetQueryObjectUi64 _getResult;
    private readonly Finish _finish;
    private readonly uint _query;

    public GlTimer(Func<string, IntPtr> getProc)
    {
        T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(getProc(name));
        _begin = Load<BeginQuery>("glBeginQuery");
        _end = Load<EndQuery>("glEndQuery");
        _getResult = Load<GetQueryObjectUi64>("glGetQueryObjectui64v");
        _finish = Load<Finish>("glFinish");
        uint id;
        Load<GenQueries>("glGenQueries")(1, &id);
        _query = id;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr GetString(int name);

    /// <summary>GL_RENDERER, so a run records which GPU (or software rasterizer) produced the numbers.</summary>
    public static string Renderer(Func<string, IntPtr> getProc) =>
        Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<GetString>(getProc("glGetString"))(0x1F01)) ?? "unknown";

    public void Begin() => _begin(GL_TIME_ELAPSED, _query);

    public void End() => _end(GL_TIME_ELAPSED);

    /// <summary>glFinish, the host's Sync step.</summary>
    public void Sync() => _finish();

    public double ReadMilliseconds()
    {
        ulong nanoseconds;
        _getResult(_query, GL_QUERY_RESULT, &nanoseconds); // blocks until the GPU finishes
        return nanoseconds / 1_000_000.0;
    }
}
