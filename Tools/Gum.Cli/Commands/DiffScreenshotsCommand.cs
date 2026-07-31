using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Raylib;
using Gum.ProjectServices.Screenshot;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli diff-screenshots</c> command, which renders every Screen and Component in
/// a project via both MonoGame and raylib and reports any pixel-level mismatch (#4174) — the check
/// that would have caught #4172 (raylib blend modes not matching the tool) automatically.
/// </summary>
public static class DiffScreenshotsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates the <c>diff-screenshots</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        Option<string> outputOption = new Option<string>(
            "--output",
            "Directory the rendered PNGs are written to, under 'A/' (MonoGame) and 'B/' (raylib) subfolders. Defaults to a temp directory.");

        Option<byte> toleranceOption = new Option<byte>(
            "--tolerance",
            () => 2,
            "Maximum per-channel pixel difference (0-255) still considered a match.");

        Option<int> proximityOption = new Option<int>(
            "--proximity",
            () => 1,
            "How many pixels away to search for a matching color before counting a pixel as a real mismatch. Absorbs positional jitter between renderers (e.g. a 1px antialiasing shift) without masking real differences.");

        Option<bool> jsonOption = new Option<bool>(
            "--json",
            "Output the diff as a JSON document.");

        Command command = new Command(
            "diff-screenshots",
            "Render every Screen and Component via MonoGame and raylib, and report any pixel-level mismatch.")
        {
            projectArgument,
            outputOption,
            toleranceOption,
            proximityOption,
            jsonOption,
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string? output = context.ParseResult.GetValueForOption(outputOption);
            byte tolerance = context.ParseResult.GetValueForOption(toleranceOption);
            int proximity = context.ParseResult.GetValueForOption(proximityOption);
            bool json = context.ParseResult.GetValueForOption(jsonOption);

            context.ExitCode = Execute(projectPath, output, tolerance, proximity, json);
        });

        return command;
    }

    private static int Execute(string projectPath, string? output, byte tolerance, int proximity, bool json)
    {
        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullProjectPath}");
            return 2;
        }

        IScreenshotDiffService diffService = new ScreenshotDiffService();
        ScreenshotDiffResult result;
        try
        {
            result = diffService.Diff(new ScreenshotDiffRequest
            {
                ProjectPath = fullProjectPath,
                BackendA = new MonoGameScreenshotService(),
                BackendB = new RaylibScreenshotService(),
                OutputDirectory = output != null ? Path.GetFullPath(output) : null,
                Tolerance = tolerance,
                ProximityRadius = proximity,
            });
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }

        IScreenshotDiffHtmlReportWriter reportWriter = new ScreenshotDiffHtmlReportWriter();
        string reportPath = reportWriter.Write(result, Path.Combine(result.OutputDirectory, "report.html"));

        if (json)
        {
            WriteJson(result, reportPath);
        }
        else
        {
            WriteHumanReadable(result, reportPath);
        }

        return result.HasMismatch ? 1 : 0;
    }

    private static void WriteJson(ScreenshotDiffResult result, string reportPath)
    {
        var payload = new
        {
            hasMismatch = result.HasMismatch,
            outputDirectory = result.OutputDirectory,
            reportPath,
            elements = result.ElementDiffs.Select(d => new
            {
                element = d.ElementName,
                matches = d.Matches,
                errorMessage = d.ErrorMessage,
                monoGamePath = d.BackendAPath,
                raylibPath = d.BackendBPath,
                mismatchedPixelCount = d.MismatchedPixelCount,
                totalPixelCount = d.TotalPixelCount,
                mismatchPercentage = d.MismatchPercentage,
                boundingBox = d.Matches ? null : new
                {
                    minX = d.BoundingBoxMinX,
                    minY = d.BoundingBoxMinY,
                    maxX = d.BoundingBoxMaxX,
                    maxY = d.BoundingBoxMaxY,
                },
                dimensionMismatch = d.DimensionMismatchDescription,
            }),
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static void WriteHumanReadable(ScreenshotDiffResult result, string reportPath)
    {
        foreach (ElementScreenshotDiff diff in result.ElementDiffs)
        {
            if (diff.Matches)
            {
                Console.WriteLine($"MATCH  {diff.ElementName}");
                continue;
            }

            string reason = diff.ErrorMessage
                ?? diff.DimensionMismatchDescription
                ?? $"{diff.MismatchedPixelCount:N0} px mismatched ({diff.MismatchPercentage:0.###}%), " +
                   $"region ({diff.BoundingBoxMinX}, {diff.BoundingBoxMinY})–({diff.BoundingBoxMaxX}, {diff.BoundingBoxMaxY})";

            Console.WriteLine($"DIFF   {diff.ElementName}: {reason}");
        }

        Console.WriteLine();
        Console.WriteLine($"Rendered PNGs: {result.OutputDirectory}");
        Console.WriteLine($"HTML report:   {reportPath}");
        int mismatchCount = result.ElementDiffs.Count(d => !d.Matches);
        Console.WriteLine(mismatchCount == 0
            ? $"All {result.ElementDiffs.Count} element(s) matched."
            : $"{mismatchCount} of {result.ElementDiffs.Count} element(s) mismatched.");
    }
}
