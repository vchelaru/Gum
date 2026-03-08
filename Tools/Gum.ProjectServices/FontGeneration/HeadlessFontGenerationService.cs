using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Headless implementation of font generation using bmfont.exe.
/// Windows-only: throws <see cref="PlatformNotSupportedException"/> on non-Windows platforms.
/// Progress and UI feedback are delivered through an optional <see cref="IFontGenerationCallbacks"/> instance.
/// </summary>
public class HeadlessFontGenerationService : IHeadlessFontGenerationService
{
    private static readonly Point[] AvailableSizes = new Point[]
    {
        new(32, 32),
        new(64, 64),
        new(128, 128),
        new(256, 256),
        new(512, 512),
        new(1024, 1024),
        new(2048, 1024),
        new(2048, 2048),
        new(4096, 2048),
        new(4096, 4096),
        new(8192, 4096),
        new(8192, 8192)
    };

    private readonly IFontGenerationCallbacks _callbacks;

    /// <summary>
    /// Initializes a new instance of <see cref="HeadlessFontGenerationService"/>.
    /// </summary>
    /// <param name="callbacks">
    /// Optional callbacks for output and spinner display. When <c>null</c>, all feedback is suppressed.
    /// </param>
    public HeadlessFontGenerationService(IFontGenerationCallbacks? callbacks = null)
    {
        _callbacks = callbacks ?? new NoOpFontGenerationCallbacks();
    }

    private string BmFontExeLocation => Path.Combine(AppContext.BaseDirectory, "Libraries", "bmfont.exe");

    /// <inheritdoc/>
    public async Task CreateAllMissingFontFiles(GumProjectSave project, string projectDirectory, bool forceRecreate = false)
    {
        ThrowIfNotWindows();
        await GenerateMissingFontsFor(project, project.AllElements, projectDirectory, forceRecreate);
    }

    /// <inheritdoc/>
    public void ReactToFontValueSet(InstanceSave instance, GumProjectSave gumProject, StateSave stateSave,
        StateSave forcedValues, string projectDirectory)
    {
        ThrowIfNotWindows();

        BmfcSave? bmfcSave = TryGetBmfcSaveFor(instance, stateSave,
            gumProject.FontRanges, gumProject.FontSpacingHorizontal, gumProject.FontSpacingVertical, forcedValues);

        if (bmfcSave != null)
        {
            EnsureToolsExtracted();

            // Run synchronously (createTask: false) — this is usually fast enough for real-time feedback.
            _ = TryCreateFontFor(bmfcSave, force: false, showSpinner: false, createTask: false,
                projectDirectory, gumProject.AutoSizeFontOutputs);
        }

        var container = stateSave.ParentContainer;

        if (container != null)
        {
            var references = ObjectFinder.Self.GetElementsReferencingRecursively(container);
            _ = GenerateMissingFontsFor(gumProject, references, projectDirectory, forceRecreate: false);
        }
    }

    /// <inheritdoc/>
    public BmfcSave? TryGetBmfcSaveFor(InstanceSave? instance, StateSave stateSave, string fontRanges,
        int spacingHorizontal, int spacingVertical, StateSave? forcedValues)
    {
        string prefix = "";
        if (instance != null)
        {
            prefix = instance.Name + ".";
        }

        int? fontSize = forcedValues?.GetValue("FontSize") as int? ?? stateSave.GetValueRecursive(prefix + "FontSize") as int?;
        string? fontValue = forcedValues?.GetValue("Font") as string ?? stateSave.GetValueRecursive(prefix + "Font") as string;
        int outlineValue = forcedValues?.GetValue("OutlineThickness") as int? ?? stateSave.GetValueRecursive(prefix + "OutlineThickness") as int? ?? 0;

        // default to true to match how old behavior worked
        bool fontSmoothing = forcedValues?.GetValue("UseFontSmoothing") as bool? ?? stateSave.GetValueRecursive(prefix + "UseFontSmoothing") as bool? ?? true;
        bool isItalic = forcedValues?.GetValue("IsItalic") as bool? ?? stateSave.GetValueRecursive(prefix + "IsItalic") as bool? ?? false;
        bool isBold = forcedValues?.GetValue("IsBold") as bool? ?? stateSave.GetValueRecursive(prefix + "IsBold") as bool? ?? false;

        BmfcSave? bmfcSave = null;
        if (fontValue != null && fontSize != null)
        {
            bmfcSave = new BmfcSave();
            bmfcSave.FontSize = fontSize.Value;
            bmfcSave.FontName = fontValue;
            bmfcSave.OutlineThickness = outlineValue;
            bmfcSave.UseSmoothing = fontSmoothing;
            bmfcSave.IsItalic = isItalic;
            bmfcSave.IsBold = isBold;
            bmfcSave.Ranges = fontRanges;
            bmfcSave.SpacingHorizontal = spacingHorizontal;
            bmfcSave.SpacingVertical = spacingVertical;
        }

        return bmfcSave;
    }

    /// <inheritdoc/>
    public async Task<GeneralResponse<Point>> GetOptimizedSizeFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, Action<string>? callback)
    {
        int index = AvailableSizes.Length / 2;
        int minIndex = 0;
        int maxIndex = AvailableSizes.Length - 1;

        Point guess = AvailableSizes[index];
        int bestIndexAt1Page = maxIndex;

        while (minIndex <= maxIndex)
        {
            bmfcSave.OutputWidth = guess.X;
            bmfcSave.OutputHeight = guess.Y;

            callback?.Invoke($"Testing {bmfcSave}, guessing {guess.X}x{guess.Y}...");

            GeneralResponse<int> pageCount = await GetPageCountFor(bmfcSave, forceMonoSpacedNumber);

            if (pageCount.Succeeded == false)
            {
                return GeneralResponse<Point>.UnsuccessfulWith(pageCount.Message);
            }
            else
            {
                callback?.Invoke($"{guess.X}x{guess.Y} requires {pageCount.Data} page(s)");
                if (pageCount.Data == 1)
                {
                    bestIndexAt1Page = Math.Min(bestIndexAt1Page, index);
                    maxIndex = index - 1;
                }
                else
                {
                    minIndex = index + 1;
                }

                index = (minIndex + maxIndex) / 2;
                guess = AvailableSizes[index];

                if (minIndex <= maxIndex)
                {
                    callback?.Invoke($"Trying again with new guess {guess.X}x{guess.Y}...");
                }
            }
        }

        GeneralResponse<Point> toReturn = new GeneralResponse<Point>();
        toReturn.Succeeded = true;
        toReturn.Data = AvailableSizes[bestIndexAt1Page];
        return toReturn;
    }

    /// <inheritdoc/>
    public async Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave, bool forceMonoSpacedNumber)
    {
        FilePath appTempDirectory = Path.Combine(Path.GetTempPath(), "Gum") +
            Path.DirectorySeparatorChar;

        OptionallyAttemptedGeneralResponse createResponse = await CreateBitmapFontFilesIfNecessaryAsync(
            bmfcSave,
            force: true,
            forceMonoSpacedNumber,
            showSpinner: false,
            createTask: true,
            appTempDirectory,
            projectDirectory: null);

        if (createResponse.Succeeded == false)
        {
            return GeneralResponse<int>.UnsuccessfulWith(createResponse.Message);
        }
        else
        {
            string fntFileName = bmfcSave.FontCacheFileName;
            FilePath desiredFntFile = Path.Combine(appTempDirectory + fntFileName);

            try
            {
                using StreamReader reader = new StreamReader(desiredFntFile.FullPath);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Contains("pages="))
                    {
                        int pagesIndex = line.IndexOf("pages=");
                        int valueStart = pagesIndex + "pages=".Length;
                        int valueEnd = line.IndexOf(' ', valueStart);
                        if (valueEnd == -1)
                        {
                            valueEnd = line.Length;
                        }

                        string pagesValue = line.Substring(valueStart, valueEnd - valueStart);
                        if (int.TryParse(pagesValue, out int pageCount))
                        {
                            GeneralResponse<int> toReturn = GeneralResponse<int>.SuccessfulResponse;
                            toReturn.Data = pageCount;
                            return toReturn;
                        }
                        else
                        {
                            return GeneralResponse<int>.UnsuccessfulWith($"Could not parse page count from value: {pagesValue}");
                        }
                    }
                }
                return GeneralResponse<int>.UnsuccessfulWith("Could not find 'pages=' in font file");
            }
            catch (Exception ex)
            {
                return GeneralResponse<int>.UnsuccessfulWith($"Error reading font file: {ex.Message}");
            }
        }
    }

    private async Task GenerateMissingFontsFor(GumProjectSave project, IEnumerable<ElementSave> elements,
        string projectDirectory, bool forceRecreate)
    {
        string fontRanges = project.FontRanges;
        int spacingHorizontal = project.FontSpacingHorizontal;
        int spacingVertical = project.FontSpacingVertical;

        Dictionary<string, BmfcSave> bitmapFonts = new Dictionary<string, BmfcSave>();

        foreach (ElementSave element in elements)
        {
            foreach (StateSave state in element.AllStates)
            {
                BmfcSave? bmfcSave = TryGetBmfcSaveFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forcedValues: null);
                if (bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }
                foreach (InstanceSave instance in element.Instances)
                {
                    BmfcSave? bmfcSaveInner = TryGetBmfcSaveFor(instance, state, fontRanges, spacingHorizontal, spacingVertical, forcedValues: null);
                    if (bmfcSaveInner != null)
                    {
                        bitmapFonts[bmfcSaveInner.FontCacheFileName] = bmfcSaveInner;
                    }
                }
            }
        }

        if (bitmapFonts.Count == 0)
        {
            _callbacks.OnOutput("No fonts to create");
        }
        else
        {
            _callbacks.OnOutput($"Checking {bitmapFonts.Count} font files...");
        }

        DateTime start = DateTime.Now;

        IDisposable? spinner = null;

        if (bitmapFonts.Count > 0)
        {
            EnsureToolsExtracted();
            spinner = _callbacks.ShowSpinner();
        }

        List<Task> tasks = new List<Task>();

        foreach (KeyValuePair<string, BmfcSave> item in bitmapFonts)
        {
            System.Diagnostics.Debug.WriteLine($"Starting {item.Key}");
            tasks.Add(TryCreateFontFor(item.Value, forceRecreate, showSpinner: false, createTask: true,
                projectDirectory, project.AutoSizeFontOutputs));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            spinner?.Dispose();
        }

        DateTime end = DateTime.Now;
        TimeSpan time = end - start;
        if (bitmapFonts.Count > 0)
        {
            _callbacks.OnOutput($"Created font files in {time.TotalSeconds:F1} seconds");
        }
    }

    private async Task<GeneralResponse> TryCreateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner,
        bool createTask, string projectDirectory, bool iterativelyDetermineSize)
    {
        if (force || GetFilePath(bmfcSave, destinationDirectory: null, projectDirectory).Exists() == false)
        {
            await AssignEstimatedNeededSizeOn(bmfcSave, iterativelyDetermineSize, _callbacks.OnOutput);
        }

        OptionallyAttemptedGeneralResponse response = await CreateBitmapFontFilesIfNecessaryAsync(
            bmfcSave, force, forceMonoSpacedNumber: false, showSpinner, createTask,
            destinationDirectory: null, projectDirectory);

        if (response.Succeeded == false)
        {
            string prefix = "Error creating font " + bmfcSave.FontName + " size " + bmfcSave.FontSize + ". ";

            if (!string.IsNullOrEmpty(response.Message))
            {
                _callbacks.OnOutput($"{prefix}" + response.Message);
            }
            else
            {
                _callbacks.OnOutput($"{prefix}Unknown error.");
            }
        }

        return response;
    }

    private async Task<OptionallyAttemptedGeneralResponse> CreateBitmapFontFilesIfNecessaryAsync(BmfcSave bmfcSave,
        bool force, bool forceMonoSpacedNumber, bool showSpinner, bool createTask,
        FilePath? destinationDirectory, string? projectDirectory)
    {
        FilePath desiredFntFile = GetFilePath(bmfcSave, destinationDirectory, projectDirectory);

        OptionallyAttemptedGeneralResponse toReturn = OptionallyAttemptedGeneralResponse.SuccessfulWithoutAttempt;

        IDisposable? spinner = null;

        try
        {
            if (!desiredFntFile.Exists() || force)
            {
                if (showSpinner)
                {
                    spinner = _callbacks.ShowSpinner();
                }

                FilePath filePathTemporary = desiredFntFile;
                string bmfcFileToSave = filePathTemporary.RemoveExtension() + ".bmfc";
                System.Console.WriteLine("Saving: " + bmfcFileToSave);

                _callbacks.OnIgnoreFileChange(bmfcFileToSave);
                _callbacks.OnIgnoreFileChange(desiredFntFile);

                FilePath pngFileNameBase = desiredFntFile.RemoveExtension();

                const int pagesToIgnore = 99;
                for (int i = 0; i < pagesToIgnore; i++)
                {
                    _callbacks.OnIgnoreFileChange($"{pngFileNameBase}_{i}.png");
                    _callbacks.OnIgnoreFileChange($"{pngFileNameBase}_{i:00}.png");
                }

                bmfcSave.Save(bmfcFileToSave);

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = BmFontExeLocation;
                info.Arguments = "-c \"" + bmfcFileToSave + "\"" + " -o \"" + desiredFntFile + "\"";
                info.UseShellExecute = true;

                string filenameAndArgs = $"{info.FileName} {info.Arguments}";
                System.Diagnostics.Debug.WriteLine($"Running: {filenameAndArgs}");
                _callbacks.OnOutput(filenameAndArgs);

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

                if (process == null)
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = "Could not start bmfont.exe process.";
                }
                else if (desiredFntFile.Exists())
                {
                    toReturn.Succeeded = true;
                    toReturn.Message = string.Empty;
                }
                else
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = "Waited for font to be created, but expected file was not created by bmfont.exe";
                }
            }
        }
        finally
        {
            spinner?.Dispose();
        }

        return toReturn;
    }

    private FilePath GetFilePath(BmfcSave bmfcSave, FilePath? destinationDirectory, string? projectDirectory)
    {
        string fntFileName = bmfcSave.FontCacheFileName;

        if (destinationDirectory != null)
        {
            return destinationDirectory + fntFileName;
        }

        if (projectDirectory != null)
        {
            return projectDirectory + fntFileName;
        }

        return FileManager.RelativeDirectory + fntFileName;
    }

    private void EnsureToolsExtracted()
    {
        Assembly assembly = typeof(HeadlessFontGenerationService).Assembly;
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

    private async Task AssignEstimatedNeededSizeOn(BmfcSave bmfcSave, bool iterativelyDetermineSize,
        Action<string> updateCallback)
    {
        bool handledIteratively = false;

        if (iterativelyDetermineSize)
        {
            GeneralResponse<Point> optimizedSize = await GetOptimizedSizeFor(bmfcSave, forceMonoSpacedNumber: false, updateCallback);

            if (optimizedSize.Succeeded)
            {
                bmfcSave.OutputWidth = optimizedSize.Data.X;
                bmfcSave.OutputHeight = optimizedSize.Data.Y;

                handledIteratively = true;
            }
        }

        if (!handledIteratively)
        {
            int spacingHorizontal = bmfcSave.SpacingHorizontal;
            int spacingVertical = bmfcSave.SpacingVertical;
            int fontSize = bmfcSave.FontSize;
            bool isBold = bmfcSave.IsBold;

            int numberWide, numberTall;

            int effectiveFontSize = fontSize + Math.Max(spacingHorizontal, spacingVertical) +
                bmfcSave.OutlineThickness * 2;
            if (isBold)
            {
                effectiveFontSize += (int)(fontSize * 0.08);
            }

            EstimateBlocksNeeded(out numberWide, out numberTall, effectiveFontSize);

            bmfcSave.OutputWidth = numberWide * 256;
            bmfcSave.OutputHeight = numberTall * 256;
        }
    }

    private static void EstimateBlocksNeeded(out int numberWide, out int numberTall, int effectiveFontSize)
    {
        int numberOf256Blocks;

        if (effectiveFontSize < 20)
        {
            numberOf256Blocks = 1;
        }
        else if (effectiveFontSize < 35)
        {
            numberOf256Blocks = 2;
        }
        else if (effectiveFontSize < 47)
        {
            numberOf256Blocks = 3;
        }
        else if (effectiveFontSize < 56)
        {
            numberOf256Blocks = 4;
        }
        else if (effectiveFontSize < 61)
        {
            numberOf256Blocks = 5;
        }
        else if (effectiveFontSize < 68)
        {
            numberOf256Blocks = 6;
        }
        else if (effectiveFontSize < 82)
        {
            numberOf256Blocks = 8;
        }
        else if (effectiveFontSize < 95)
        {
            numberOf256Blocks = 10;
        }
        else if (effectiveFontSize < 103)
        {
            numberOf256Blocks = 12;
        }
        else if (effectiveFontSize < 113)
        {
            numberOf256Blocks = 14;
        }
        else if (effectiveFontSize < 120)
        {
            numberOf256Blocks = 16;
        }
        else if (effectiveFontSize < 131)
        {
            numberOf256Blocks = 18;
        }
        else
        {
            numberOf256Blocks = 20;
        }

        if ((numberOf256Blocks % 5) == 0 && numberOf256Blocks / 5 < 8)
        {
            numberWide = 5;
            numberTall = numberOf256Blocks / 5;
        }
        else if ((numberOf256Blocks % 4) == 0 && numberOf256Blocks / 4 < 8)
        {
            numberWide = 4;
            numberTall = numberOf256Blocks / 4;
        }
        else if ((numberOf256Blocks % 3) == 0 && numberOf256Blocks / 3 < 8)
        {
            numberWide = 3;
            numberTall = numberOf256Blocks / 3;
        }
        else if ((numberOf256Blocks % 2) == 0 && numberOf256Blocks / 2 < 8)
        {
            numberWide = 2;
            numberTall = numberOf256Blocks / 2;
        }
        else
        {
            numberWide = 1;
            numberTall = numberOf256Blocks;
        }
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
