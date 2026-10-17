using System.Diagnostics;
using System.Reflection;
using System.Text;
using Gum.Forms.Controls;
using Gum.Managers;
using RenderingLibrary;
using ToolsUtilities;
using Xunit.Abstractions;

namespace Gum.Layout.Benchmarks;

/// <summary>
/// Cost of the ambient reads that resolve the current service (#6066): <c>ObjectFinder.Self</c>,
/// <c>FileManager.RelativeDirectory</c>, <c>FrameworkElement.MainCursor</c>, <c>PopupRoot</c> and
/// <c>DefaultFormsTemplates</c>. Everything is public API that also exists before the facades, so the same
/// file compiles and runs on either side of #6077 for a before/after comparison. Prints nanoseconds per
/// read (best of several batches) and writes <c>facade-benchmark.txt</c> next to the test binary.
/// </summary>
/// <remarks>
/// Sections: with two services but no per-service state (the cost every host pays), then with two live
/// services where <c>IGumService.Default</c> is the first (it adopts the process statics) and where it is the
/// second (its own state). The services come from <c>InitializeForTesting</c>, which does not create a
/// <c>GumContext</c>, so the contexts are attached by reflection; on code without <c>GumContext</c> only the
/// first section runs.
/// </remarks>
public class StaticFacadeBenchmarks
{
    private const int Reads = 20_000_000;
    private const int Batches = 7;

    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly ITestOutputHelper _output;

    public StaticFacadeBenchmarks(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReadCost()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("ambient read cost, ns per read (best of " + Batches + " batches of " + Reads + ")");
        report.AppendLine("baseline (empty lambda)  " + Measure(() => (object?)null).ToString("F2"));

        IGumService? originalDefault = IGumService.Default;
        string originalRelativeDirectory = FileManager.RelativeDirectory;
        Gum.GumService first = new Gum.GumService();
        Gum.GumService second = new Gum.GumService();
        List<object> contexts = new List<object>();
        try
        {
            first.InitializeForTesting();
            second.InitializeForTesting();
            IGumService.Default = first;

            report.AppendLine("-- two services, no per-service state");
            AppendReads(report);

            // The first context to go live adopts the process statics, so attach in this order.
            object? firstContext = AttachContext(first);
            object? secondContext = AttachContext(second);
            if (firstContext == null || secondContext == null)
            {
                report.AppendLine("(no GumContext in this build)");
            }
            else
            {
                contexts.Add(firstContext);
                contexts.Add(secondContext);

                IGumService.Default = first;
                report.AppendLine("-- default is the first service (adopts the process statics)");
                AppendReads(report);

                IGumService.Default = second;
                FileManager.RelativeDirectory = "Content/";
                report.AppendLine("-- default is the second service (own state)");
                AppendReads(report);
            }
        }
        finally
        {
            IGumService.Default = originalDefault;
            foreach (object context in contexts)
            {
                context.GetType().GetMethod("Release", Any)!.Invoke(context, null);
            }
            DetachContext(first);
            DetachContext(second);
            FileManager.RelativeDirectory = originalRelativeDirectory;
        }

        _output.WriteLine(report.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "facade-benchmark.txt"), report.ToString());
    }

    private static double Measure(Func<object?> read)
    {
        object? sink = null;
        for (int i = 0; i < 1_000_000; i++)
        {
            sink = read();
        }

        double best = double.MaxValue;
        for (int batch = 0; batch < Batches; batch++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < Reads; i++)
            {
                sink = read();
            }
            stopwatch.Stop();
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / Reads);
        }
        GC.KeepAlive(sink);
        return best;
    }

    // Reflection keeps this file compiling against the code before GumContext existed.
    private static object? AttachContext(Gum.GumService service)
    {
        Type? contextType = typeof(ObjectFinder).Assembly.GetType("Gum.GumContext");
        FieldInfo? contextField = typeof(Gum.GumService).GetField("_context", Any);
        if (contextType == null || contextField == null)
        {
            return null;
        }

        object context = Activator.CreateInstance(contextType, nonPublic: true)!;
        contextType.GetProperty("ServiceTypeName", Any)!.SetValue(context, "BenchmarkService");
        contextType.GetProperty("Service", Any)!.SetValue(context, service);
        contextType.GetMethod("Register", Any)!.Invoke(context, null);
        contextField.SetValue(service, context);
        return context;
    }

    private static void DetachContext(Gum.GumService service)
    {
        typeof(Gum.GumService).GetField("_context", Any)?.SetValue(service, null);
    }

    private static void AppendReads(StringBuilder report)
    {
        report.AppendLine("ObjectFinder.Self        " + Measure(() => ObjectFinder.Self).ToString("F2"));
        report.AppendLine("FileManager.RelativeDir  " + Measure(() => FileManager.RelativeDirectory).ToString("F2"));
        report.AppendLine("FrameworkElement.MainCursor " + Measure(() => FrameworkElement.MainCursor).ToString("F2"));
        report.AppendLine("FrameworkElement.PopupRoot  " + Measure(() => FrameworkElement.PopupRoot).ToString("F2"));
        report.AppendLine("DefaultFormsTemplates       " + Measure(() => FrameworkElement.DefaultFormsTemplates).ToString("F2"));
    }
}
