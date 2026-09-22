using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Headless implementation of font generation that delegates actual file creation to an <see cref="IFontFileGenerator"/>.
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

    private readonly IFontFileGenerator _fontFileGenerator;
    private readonly IFontGenerationCallbacks _callbacks;

    /// <summary>
    /// How long a failed generation attempt is remembered before the next request for the same
    /// font (by <see cref="BmfcSave.FontCacheFileName"/>) is allowed to retry for real. Without
    /// this, a single bad font reference (missing file, unresolvable system font) turns into a
    /// full generation attempt — and exception/log line — per state/instance that references it
    /// (#4254). A cooldown rather than a session-sticky cache lets an external fix (e.g. installing
    /// a missing font) self-heal without requiring a project reload.
    /// </summary>
    internal static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, DateTime> _recentFailures = new();

    /// <summary>
    /// Generations currently running, keyed by the target .fnt path. A second request for a file
    /// that is already being written joins the running one instead of racing it (#4799).
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<OptionallyAttemptedGeneralResponse>>> _inFlight = new();

    /// <summary>
    /// Test seam — overridden by a test subclass to advance the clock without a real wait.
    /// </summary>
    internal virtual DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of <see cref="HeadlessFontGenerationService"/>.
    /// </summary>
    /// <param name="fontFileGenerator">Strategy for generating individual font files.</param>
    /// <param name="callbacks">
    /// Optional callbacks for output and spinner display. When <c>null</c>, all feedback is suppressed.
    /// </param>
    public HeadlessFontGenerationService(IFontFileGenerator fontFileGenerator, IFontGenerationCallbacks? callbacks = null)
    {
        _fontFileGenerator = fontFileGenerator;
        _callbacks = callbacks ?? new NoOpFontGenerationCallbacks();
    }

    /// <inheritdoc/>
    public async Task<int> CreateAllMissingFontFiles(GumProjectSave project, string projectDirectory, bool forceRecreate = false)
    {
        return await GenerateMissingFontsFor(project, project.AllElements, projectDirectory, forceRecreate);
    }

    /// <summary>
    /// When a font property changes on an element, all elements that reference it may need
    /// new font files generated for their overridden font combinations. This ensures those
    /// files exist on disk even if the user never views those elements before closing the tool.
    ///
    /// Single-font on-demand creation is handled by the shared code in
    /// CustomSetPropertyOnRenderable.UpdateToFontValues via IFontManager.
    /// </summary>
    public void GenerateMissingFontsForReferencingElements(GumProjectSave gumProject,
        StateSave stateSave, string projectDirectory)
    {
        var container = stateSave.ParentContainer;

        if (container != null)
        {
            var references = ObjectFinder.Self.GetElementsReferencingRecursively(container);
            _ = GenerateMissingFontsFor(gumProject, references, projectDirectory, forceRecreate: false);
        }
    }

    /// <inheritdoc/>
    public FontFileStatus CreateFontIfNecessary(BmfcSave bmfcSave, string projectDirectory, bool autoSizeFontOutputs)
    {
        // Run synchronously (createTask: false) — used by property-setting code paths.
        Task<OptionallyAttemptedGeneralResponse> task = TryCreateFontFor(bmfcSave, force: false, showSpinner: false,
            createTask: false, projectDirectory, autoSizeFontOutputs);

        // TryCreateFontFor with createTask: false completes synchronously,
        // so .Result is safe here and will not deadlock.
        OptionallyAttemptedGeneralResponse response = task.Result;

        if (response.IsInProgress)
        {
            return FontFileStatus.Generating;
        }

        return response.Succeeded ? FontFileStatus.Ready : FontFileStatus.Failed;
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

    /// <summary>
    /// Collects all unique fonts required by the given elements without performing any I/O.
    /// Delegates to <see cref="FontReferenceCollector"/> in GumCommon so this code path is
    /// shared with <see cref="GumProjectDependencyWalker"/> (used by <c>gumcli pack</c>).
    /// </summary>
    internal Dictionary<string, BmfcSave> CollectRequiredFonts(GumProjectSave project, IEnumerable<ElementSave> elements)
    {
        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));

        // The collector resolves every instance's BaseType through the injected resolver above,
        // which falls back to an O(n) linear scan of Screens/Components/StandardElements per call
        // without the cache. EnableCache/DisableCache is reference-counted, so this composes safely
        // if a caller already has the cache active.
        ObjectFinder.Self.EnableCache();
        try
        {
            return collector.Collect(project, elements);
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
    }

    /// <returns>How many fonts were actually generated (as opposed to already on disk).</returns>
    private async Task<int> GenerateMissingFontsFor(GumProjectSave project, IEnumerable<ElementSave> elements,
        string projectDirectory, bool forceRecreate)
    {
        using var totalScope = Gum.Diagnostics.StartupTiming.Time("HeadlessFontGenerationService.GenerateMissingFontsFor (total)");

        Dictionary<string, BmfcSave> bitmapFonts;
        using (Gum.Diagnostics.StartupTiming.Time("  CollectRequiredFonts"))
        {
            bitmapFonts = CollectRequiredFonts(project, elements);
        }
        Gum.Diagnostics.StartupTiming.Log($"  CollectRequiredFonts found {bitmapFonts.Count} fonts");

        // Resolve relative FontFile paths to absolute so font generators can find them.
        // FontFile is stored relative to the project directory, but generators resolve
        // paths relative to their own working directory or the .bmfc file location.
        foreach (BmfcSave bmfc in bitmapFonts.Values)
        {
            if (!string.IsNullOrEmpty(bmfc.FontFile) && !Path.IsPathRooted(bmfc.FontFile))
            {
                bmfc.FontFile = Path.GetFullPath(Path.Combine(projectDirectory, bmfc.FontFile));
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
            spinner = _callbacks.ShowSpinner();
        }

        List<Task> tasks = new List<Task>();
        ConcurrentBag<bool> didAttemptFlags = new();

        int completed = 0;
        _callbacks.OnFontProgress(0, bitmapFonts.Count);

        foreach (KeyValuePair<string, BmfcSave> item in bitmapFonts)
        {
            System.Diagnostics.Debug.WriteLine($"Starting {item.Key}");
            Task task = TryCreateFontFor(item.Value, forceRecreate, showSpinner: false, createTask: true,
                projectDirectory, project.AutoSizeFontOutputs)
                .ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion && t.Result is OptionallyAttemptedGeneralResponse response)
                    {
                        didAttemptFlags.Add(response.DidAttempt);
                    }

                    int current = Interlocked.Increment(ref completed);
                    _callbacks.OnFontProgress(current, bitmapFonts.Count);
                });
            tasks.Add(task);
        }

        try
        {
            using var _ = Gum.Diagnostics.StartupTiming.Time("  Task.WhenAll(per-font generation)");
            await Task.WhenAll(tasks);
        }
        finally
        {
            spinner?.Dispose();
        }

        DateTime end = DateTime.Now;
        TimeSpan time = end - start;
        int attemptedCount = didAttemptFlags.Count(didAttempt => didAttempt);
        if (bitmapFonts.Count > 0)
        {
            _callbacks.OnOutput(BuildFontGenerationSummaryMessage(bitmapFonts.Count, attemptedCount, time));
        }

        return attemptedCount;
    }

    /// <summary>
    /// Builds the summary message shown after a font-generation pass. <paramref name="attemptedCount"/>
    /// is how many of the <paramref name="totalFontCount"/> fonts actually needed (re)generation — the
    /// rest were already cached on disk. Reported separately so a cache-hit run (#4266) doesn't claim
    /// "Created" when nothing was actually created.
    /// </summary>
    internal static string BuildFontGenerationSummaryMessage(int totalFontCount, int attemptedCount, TimeSpan elapsedTime)
    {
        if (attemptedCount == 0)
        {
            return $"All {totalFontCount} font files already up to date";
        }

        if (attemptedCount == totalFontCount)
        {
            return $"Created {totalFontCount} font files in {elapsedTime.TotalSeconds:F1} seconds";
        }

        return $"Created {attemptedCount} font files ({totalFontCount - attemptedCount} already up to date) in {elapsedTime.TotalSeconds:F1} seconds";
    }

    private async Task<OptionallyAttemptedGeneralResponse> TryCreateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner,
        bool createTask, string projectDirectory, bool iterativelyDetermineSize)
    {
        string inFlightKey = GetFilePath(bmfcSave, destinationDirectory: null, projectDirectory).FullPath;

        Lazy<Task<OptionallyAttemptedGeneralResponse>> ownGeneration = new(() =>
            GenerateFontFor(bmfcSave, force, showSpinner, createTask, projectDirectory, iterativelyDetermineSize));
        Lazy<Task<OptionallyAttemptedGeneralResponse>> registered = _inFlight.GetOrAdd(inFlightKey, ownGeneration);

        if (!ReferenceEquals(registered, ownGeneration))
        {
            if (!createTask)
            {
                // A synchronous caller (font resolution on the UI thread) can't block on a task
                // whose continuations may need that same thread; report that the file is on its way.
                return new OptionallyAttemptedGeneralResponse
                {
                    Succeeded = false,
                    DidAttempt = false,
                    IsInProgress = true,
                    Message = $"Font {bmfcSave.FontName} size {bmfcSave.FontSize} is already being generated."
                };
            }

            return await registered.Value;
        }

        try
        {
            return await ownGeneration.Value;
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<OptionallyAttemptedGeneralResponse>>>(inFlightKey, ownGeneration));
        }
    }

    private async Task<OptionallyAttemptedGeneralResponse> GenerateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner,
        bool createTask, string projectDirectory, bool iterativelyDetermineSize)
    {
        string cacheKey = bmfcSave.FontCacheFileName;

        if (!force && _recentFailures.TryGetValue(cacheKey, out DateTime failedAt)
            && UtcNow - failedAt < FailureCooldown)
        {
            return new OptionallyAttemptedGeneralResponse
            {
                Succeeded = false,
                DidAttempt = false,
                Message = $"Skipping retry for font {bmfcSave.FontName} size {bmfcSave.FontSize} — a previous attempt failed recently."
            };
        }

        if ((force || GetFilePath(bmfcSave, destinationDirectory: null, projectDirectory).Exists() == false)
            && _fontFileGenerator.RequiresSizeEstimation)
        {
            await AssignEstimatedNeededSizeOn(bmfcSave, iterativelyDetermineSize, _callbacks.OnOutput);
        }

        OptionallyAttemptedGeneralResponse response = await CreateBitmapFontFilesIfNecessaryAsync(
            bmfcSave, force, forceMonoSpacedNumber: false, showSpinner, createTask,
            destinationDirectory: null, projectDirectory);

        if (response.Succeeded == false)
        {
            _recentFailures[cacheKey] = UtcNow;

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
        else
        {
            _recentFailures.TryRemove(cacheKey, out _);
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

        // Issue #4001: a dropshadow font is a primary .fnt plus a "-shadow.fnt" sibling (the shadow
        // silhouette, sharing the primary's PNG). A primary present without its sibling — e.g. an
        // atlas baked before the two-pass change — must regenerate, otherwise the runtime renders
        // with no shadow at all.
        bool shadowSiblingMissing = bmfcSave.HasDropshadow
            && !new FilePath(desiredFntFile.RemoveExtension() + "-shadow.fnt").Exists();

        try
        {
            if (!desiredFntFile.Exists() || shadowSiblingMissing || force)
            {
                using var _ = Gum.Diagnostics.StartupTiming.Time($"    generate {desiredFntFile.FileNameNoPath}");

                if (showSpinner)
                {
                    spinner = _callbacks.ShowSpinner();
                }

                FilePath bmfcFilePath = desiredFntFile.RemoveExtension() + ".bmfc";
                _callbacks.OnIgnoreFileChange(bmfcFilePath);
                _callbacks.OnIgnoreFileChange(desiredFntFile);

                FilePath pngFileNameBase = desiredFntFile.RemoveExtension();

                const int pagesToIgnore = 99;
                for (int i = 0; i < pagesToIgnore; i++)
                {
                    _callbacks.OnIgnoreFileChange($"{pngFileNameBase}_{i}.png");
                    _callbacks.OnIgnoreFileChange($"{pngFileNameBase}_{i:00}.png");
                }

                GeneralResponse generateResponse = await _fontFileGenerator.GenerateFont(
                    bmfcSave, desiredFntFile.FullPath, createTask);

                toReturn.DidAttempt = true;
                toReturn.Succeeded = generateResponse.Succeeded;
                toReturn.Message = generateResponse.Message;
            }
            else
            {
                Gum.Diagnostics.StartupTiming.Log($"    skip {desiredFntFile.FileNameNoPath} (already exists)");
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
            // This heuristic only runs for the bmfont.exe backend (RequiresSizeEstimation), which
            // itself rounds FontSize to the nearest whole pixel size (GetGdiRoundedFontSize) before
            // rasterizing, so estimate against that same rounded value.
            int fontSize = BmfcSave.GetGdiRoundedFontSize(bmfcSave.FontSize);
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

    /// <summary>
    /// Default no-op implementation used when no callbacks are supplied.
    /// </summary>
    private sealed class NoOpFontGenerationCallbacks : IFontGenerationCallbacks { }
}
