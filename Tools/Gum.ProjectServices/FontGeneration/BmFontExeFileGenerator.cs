using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Generates bitmap font files by invoking the embedded bmfont.exe tool.
/// Windows-only: throws <see cref="PlatformNotSupportedException"/> on non-Windows platforms.
/// </summary>
public class BmFontExeFileGenerator : IFontFileGenerator
{
    private readonly IFontGenerationCallbacks _callbacks;

    /// <summary>
    /// Initializes a new instance of <see cref="BmFontExeFileGenerator"/>.
    /// </summary>
    /// <param name="callbacks">
    /// Optional callbacks for output logging. When <c>null</c>, all feedback is suppressed.
    /// </param>
    public BmFontExeFileGenerator(IFontGenerationCallbacks? callbacks = null)
    {
        _callbacks = callbacks ?? new NoOpFontGenerationCallbacks();
    }

    private string BmFontExeLocation => Path.Combine(AppContext.BaseDirectory, "Libraries", "bmfont.exe");

    /// <inheritdoc/>
    public async Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        ThrowIfNotWindows();
        EnsureToolsExtracted();

        string bmfcFileToSave = Path.ChangeExtension(outputFntPath, ".bmfc");
        System.Console.WriteLine("Saving: " + bmfcFileToSave);

        bmfcSave.Save(bmfcFileToSave);

        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = BmFontExeLocation;
        info.Arguments = "-c \"" + bmfcFileToSave + "\"" + " -o \"" + outputFntPath + "\"";
        info.UseShellExecute = true;

        var stopwatch = Stopwatch.StartNew();

        Process? process = Process.Start(info);

        if (process != null)
        {
            if (createTask)
            {
                await WaitForExitAsync(process);
            }
            else
            {
                process.WaitForExit();
            }
        }

        stopwatch.Stop();

        GeneralResponse toReturn = new GeneralResponse();

        if (process == null)
        {
            toReturn.Succeeded = false;
            toReturn.Message = "Could not start bmfont.exe process.";
        }
        else if (File.Exists(outputFntPath))
        {
            toReturn.Succeeded = true;
            toReturn.Message = string.Empty;
            _callbacks.OnOutput($"bmfont ({stopwatch.ElapsedMilliseconds}ms) : generated \"{bmfcSave.FontName}\" size {bmfcSave.FontSize} -> {outputFntPath} ");
        }
        else
        {
            toReturn.Succeeded = false;
            toReturn.Message = "Waited for font to be created, but expected file was not created by bmfont.exe";
        }

        return toReturn;
    }

    /// <summary>
    /// Extracts embedded bmfont.exe and BmfcTemplate.bmfc resources if they are not already present on disk.
    /// </summary>
    internal void EnsureToolsExtracted()
    {
        Assembly assembly = typeof(BmFontExeFileGenerator).Assembly;
        string baseDir = AppContext.BaseDirectory;

        ExtractResourceIfMissing(assembly,
            "Gum.ProjectServices.Libraries.bmfont.exe",
            Path.Combine(baseDir, "Libraries", "bmfont.exe"));

        ExtractResourceIfMissing(assembly,
            "Gum.ProjectServices.Content.BmfcTemplate.bmfc",
            Path.Combine(baseDir, "Content", "BmfcTemplate.bmfc"));
    }

    private static void ExtractResourceIfMissing(Assembly assembly, string resourceName, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(destinationPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {assembly.FullName}.");
        }

        using FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        resourceStream.CopyTo(fileStream);
    }

    private static async Task<int> WaitForExitAsync(Process process, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Process_Exited(object? sender, EventArgs e)
        {
            tcs.TrySetResult(process.ExitCode);
        }

        try
        {
            process.EnableRaisingEvents = true;
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            // Expected when enabling events after the process already exited.
        }

        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            process.Exited += Process_Exited;

            try
            {
                if (process.HasExited)
                {
                    tcs.TrySetResult(process.ExitCode);
                }

                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                process.Exited -= Process_Exited;
            }
        }
    }

    private static void ThrowIfNotWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Font generation requires Windows (bmfont.exe is a Windows-only application).");
        }
    }

    /// <summary>
    /// Default no-op implementation used when no callbacks are supplied.
    /// </summary>
    private sealed class NoOpFontGenerationCallbacks : IFontGenerationCallbacks { }
}
