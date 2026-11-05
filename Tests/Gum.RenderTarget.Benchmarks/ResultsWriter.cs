using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>Writes results.csv and results.md.</summary>
public static class ResultsWriter
{
    private static string F(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);

    public static void Write(string folder, IReadOnlyList<ResultRow> rows, string header)
    {
        Directory.CreateDirectory(folder);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("scenario,sprites,size,frames,draw_cpu_mean_ms,draw_cpu_median_ms,draw_cpu_p95_ms," +
            "frame_mean_ms,frame_median_ms,frame_p95_ms,drained_wall_ms_per_frame," +
            "gl_draw_calls,gl_clears,gl_target_switches,gum_draw_call_count,gum_spritebatch_begins,gum_shapebatch_begins," +
            "alloc_kb_per_frame,gen0_gcs,gen2_gcs");
        foreach (ResultRow r in rows)
        {
            csv.AppendLine(string.Join(",",
                r.Scenario, r.Config.SpriteCount, r.Config.SpriteSize, r.MeasuredFrames,
                F(r.DrawCpuMs.Mean), F(r.DrawCpuMs.Median), F(r.DrawCpuMs.P95),
                F(r.FrameMs.Mean), F(r.FrameMs.Median), F(r.FrameMs.P95), F(r.DrainedWallMsPerFrame),
                r.GlDrawCalls, r.GlClears, r.GlTargetSwitches,
                r.GumDrawCallCount, r.GumSpriteBatchBegins, r.GumShapeBatchBegins,
                F(r.AllocKbPerFrame), r.Gen0Collections, r.Gen2Collections));
        }

        File.WriteAllText(Path.Combine(folder, "results.csv"), csv.ToString());

        StringBuilder md = new StringBuilder();
        md.AppendLine(header);
        md.AppendLine();
        md.AppendLine("| Scn | Sprites | Size | Draw CPU mean / median / p95 (ms) | Frame mean / median / p95 (ms) | Drained wall (ms/frame) | GL draws | GL clears | RT switches | Gum SpriteBatch begins | Alloc KB/frame |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (ResultRow r in rows)
        {
            md.AppendLine($"| {r.Scenario} | {r.Config.SpriteCount} | {r.Config.SpriteSize} | " +
                $"{F(r.DrawCpuMs.Mean)} / {F(r.DrawCpuMs.Median)} / {F(r.DrawCpuMs.P95)} | " +
                $"{F(r.FrameMs.Mean)} / {F(r.FrameMs.Median)} / {F(r.FrameMs.P95)} | {F(r.DrainedWallMsPerFrame)} | " +
                $"{r.GlDrawCalls} | {r.GlClears} | {r.GlTargetSwitches} | {r.GumSpriteBatchBegins} | {F(r.AllocKbPerFrame)} |");
        }

        File.WriteAllText(Path.Combine(folder, "results.md"), md.ToString());
    }
}
