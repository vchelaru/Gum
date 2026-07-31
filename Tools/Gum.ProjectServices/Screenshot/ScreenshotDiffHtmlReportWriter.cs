using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Writes a <see cref="ScreenshotDiffResult"/> as a static HTML page with one row per element,
/// MonoGame's render on the left and raylib's on the right, so a mismatch can be spotted by eye
/// without opening each PNG individually (#4174).
/// </summary>
/// <remarks>
/// Image <c>src</c> attributes are relative paths computed against the report file's own
/// directory, so the report only works when opened from beside the rendered PNGs (i.e. written
/// into <see cref="ScreenshotDiffResult.OutputDirectory"/>, which is where
/// <c>gumcli diff-screenshots</c> always places it) — no images are embedded.
/// </remarks>
public class ScreenshotDiffHtmlReportWriter : IScreenshotDiffHtmlReportWriter
{
    /// <inheritdoc/>
    public string Write(ScreenshotDiffResult result, string outputPath)
    {
        string reportDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))
            ?? throw new System.ArgumentException("Output path must have a directory.", nameof(outputPath));

        IEnumerable<ElementScreenshotDiff> ordered = result.ElementDiffs
            .OrderBy(d => d.Matches)
            .ThenBy(d => d.ElementName);

        StringBuilder html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Gum screenshot diff</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: sans-serif; background: #1e1e1e; color: #eee; }");
        html.AppendLine("h1 { font-size: 1.2rem; }");
        html.AppendLine(".row { display: flex; align-items: flex-start; gap: 1rem; padding: 1rem; margin-bottom: 1rem; border-radius: 6px; }");
        html.AppendLine(".row.match { background: #1f3320; }");
        html.AppendLine(".row.mismatch { background: #3a2020; }");
        html.AppendLine(".row .info { flex: 0 0 260px; }");
        html.AppendLine(".row .pane { flex: 1; text-align: center; }");
        html.AppendLine(".row .pane img { max-width: 100%; background: repeating-conic-gradient(#444 0% 25%, #333 0% 50%) 0 / 16px 16px; }");
        html.AppendLine(".badge { display: inline-block; padding: 0.1rem 0.5rem; border-radius: 4px; font-weight: bold; font-size: 0.85rem; }");
        html.AppendLine(".badge.match { background: #2e7d32; }");
        html.AppendLine(".badge.mismatch { background: #c62828; }");
        html.AppendLine("</style></head><body>");

        int mismatchCount = result.ElementDiffs.Count(d => !d.Matches);
        html.AppendLine($"<h1>{result.ElementDiffs.Count} element(s), {mismatchCount} mismatched — MonoGame (left) vs raylib (right)</h1>");

        foreach (ElementScreenshotDiff diff in ordered)
        {
            AppendRow(html, diff, reportDirectory);
        }

        html.AppendLine("</body></html>");

        File.WriteAllText(outputPath, html.ToString());
        return outputPath;
    }

    private static void AppendRow(StringBuilder html, ElementScreenshotDiff diff, string reportDirectory)
    {
        string rowClass = diff.Matches ? "match" : "mismatch";
        string badgeText = diff.Matches ? "MATCH" : "DIFF";

        html.AppendLine($"<div class=\"row {rowClass}\">");
        html.AppendLine("<div class=\"info\">");
        html.AppendLine($"<span class=\"badge {rowClass}\">{badgeText}</span><br>");
        html.AppendLine($"<strong>{WebUtility.HtmlEncode(diff.ElementName)}</strong>");

        if (diff.ErrorMessage != null)
        {
            html.AppendLine($"<p>{WebUtility.HtmlEncode(diff.ErrorMessage)}</p>");
        }
        else if (diff.DimensionMismatchDescription != null)
        {
            html.AppendLine($"<p>{WebUtility.HtmlEncode(diff.DimensionMismatchDescription)}</p>");
        }
        else if (!diff.Matches)
        {
            html.AppendLine($"<p>pixel ({diff.DiffX}, {diff.DiffY}) differs by {diff.MaxChannelDifference}</p>");
        }

        html.AppendLine("</div>");
        html.AppendLine(RenderPane(diff.BackendAPath, reportDirectory));
        html.AppendLine(RenderPane(diff.BackendBPath, reportDirectory));
        html.AppendLine("</div>");
    }

    private static string RenderPane(string? imagePath, string reportDirectory)
    {
        if (imagePath == null)
        {
            return "<div class=\"pane\">(not rendered)</div>";
        }

        string relativePath = Path.GetRelativePath(reportDirectory, imagePath).Replace('\\', '/');
        return $"<div class=\"pane\"><img src=\"{WebUtility.HtmlEncode(relativePath)}\"></div>";
    }
}
