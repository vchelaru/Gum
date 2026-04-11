using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    public async Task CreateAllMissingFontFiles(GumProjectSave project, string projectDirectory, bool forceRecreate = false)
    {
        await GenerateMissingFontsFor(project, project.AllElements, projectDirectory, forceRecreate);
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
    public GeneralResponse CreateFontIfNecessary(BmfcSave bmfcSave, string projectDirectory, bool autoSizeFontOutputs)
    {
        // Run synchronously (createTask: false) — used by property-setting code paths.
        Task<GeneralResponse> task = TryCreateFontFor(bmfcSave, force: false, showSpinner: false,
            createTask: false, projectDirectory, autoSizeFontOutputs);

        // TryCreateFontFor with createTask: false completes synchronously,
        // so .Result is safe here and will not deadlock.
        return task.Result;
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
            bmfcSave.OutlineThickness = outlineValue;
            bmfcSave.UseSmoothing = fontSmoothing;
            bmfcSave.IsItalic = isItalic;
            bmfcSave.IsBold = isBold;
            bmfcSave.Ranges = fontRanges;
            bmfcSave.SpacingHorizontal = spacingHorizontal;
            bmfcSave.SpacingVertical = spacingVertical;

            if (BmfcSave.IsFontFilePath(fontValue))
            {
                bmfcSave.FontFile = fontValue;
                bmfcSave.FontName = Path.GetFileNameWithoutExtension(fontValue);
            }
            else
            {
                bmfcSave.FontName = fontValue;
            }
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

    /// <summary>
    /// Collects all unique fonts required by the given elements without performing any I/O.
    /// Keyed by <see cref="BmfcSave.FontCacheFileName"/> so duplicate font+size+style combinations
    /// are automatically deduplicated.
    /// </summary>
    internal Dictionary<string, BmfcSave> CollectRequiredFonts(GumProjectSave project, IEnumerable<ElementSave> elements)
    {
        string fontRanges = project.FontRanges;
        int spacingHorizontal = project.FontSpacingHorizontal;
        int spacingVertical = project.FontSpacingVertical;

        Dictionary<string, BmfcSave> bitmapFonts = new Dictionary<string, BmfcSave>();

        foreach (ElementSave element in elements)
        {
            foreach (StateSave state in element.AllStates)
            {
                // Resolve variable references so that font properties set via references
                // (e.g. "FontSize = HeaderText.FontSize") are baked into the state before
                // we read them. In the tool this happens on every edit, but in the headless/CLI
                // path the references may not have been applied yet.
                element.ApplyVariableReferences(state);

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

                    // TryGetBmfcSaveFor only finds font properties set directly on this instance
                    // (e.g., "MyComponentInstance.Font"). For component instances, font properties
                    // live on inner Text instances and may be partially exposed. Use
                    // RecursiveVariableFinder to resolve through the component hierarchy.
                    CollectFontsFromNestedTextInstances(element, state, instance,
                        bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
                }
            }
        }

        return bitmapFonts;
    }

    /// <summary>
    /// For a component instance, descends into the component to find Text instances and resolves
    /// their font properties using <see cref="RecursiveVariableFinder"/>. This handles the case
    /// where a screen has a component instance with an overridden FontSize, but the Font (family)
    /// comes from the component's inner Text definition.
    /// </summary>
    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave componentInstance, Dictionary<string, BmfcSave> bitmapFonts,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        ElementSave? componentElement = ObjectFinder.Self.GetElementSave(componentInstance);
        if (componentElement == null)
        {
            return;
        }

        foreach (InstanceSave innerInstance in componentElement.Instances)
        {
            ElementSave? innerElement = ObjectFinder.Self.GetElementSave(innerInstance);
            if (innerElement == null)
            {
                continue;
            }

            if (innerElement is StandardElementSave standard && standard.Name == "Text")
            {
                // Build element stack: outer element → component → Text standard element
                // RecursiveVariableFinder.GetValueByBottomName starts from the bottom (Text)
                // and climbs up through exposed variables to find overrides.
                List<ElementWithState> elementStack = new List<ElementWithState>
                {
                    new ElementWithState(outerElement) { StateName = outerState.Name, InstanceName = componentInstance.Name },
                    new ElementWithState(componentElement) { InstanceName = innerInstance.Name },
                    new ElementWithState(innerElement)
                };

                BmfcSave? bmfcSave = TryGetBmfcSaveFromStack(elementStack,
                    fontRanges, spacingHorizontal, spacingVertical);
                if (bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }
            }
            else
            {
                // Recurse into nested components (component containing component containing Text)
                CollectFontsFromNestedTextInstances(outerElement, outerState, componentInstance,
                    componentElement, innerInstance, bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
            }
        }
    }

    /// <summary>
    /// Recursive overload for deeper nesting (component → component → ... → Text).
    /// Builds the element stack progressively as it descends.
    /// </summary>
    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave outerInstance, ElementSave parentComponent, InstanceSave innerInstance,
        Dictionary<string, BmfcSave> bitmapFonts,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        ElementSave? innerElement = ObjectFinder.Self.GetElementSave(innerInstance);
        if (innerElement == null)
        {
            return;
        }

        if (innerElement is StandardElementSave standard && standard.Name == "Text")
        {
            List<ElementWithState> elementStack = new List<ElementWithState>
            {
                new ElementWithState(outerElement) { StateName = outerState.Name, InstanceName = outerInstance.Name },
                new ElementWithState(parentComponent) { InstanceName = innerInstance.Name },
                new ElementWithState(innerElement)
            };

            BmfcSave? bmfcSave = TryGetBmfcSaveFromStack(elementStack,
                fontRanges, spacingHorizontal, spacingVertical);
            if (bmfcSave != null)
            {
                bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
            }
        }
        else
        {
            // Continue descending
            foreach (InstanceSave deeperInstance in innerElement.Instances)
            {
                CollectFontsFromNestedTextInstances(outerElement, outerState, outerInstance,
                    innerElement, deeperInstance, bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
            }
        }
    }

    /// <summary>
    /// Resolves font properties through a component hierarchy using <see cref="RecursiveVariableFinder"/>
    /// and returns a <see cref="BmfcSave"/> if both Font and FontSize are found.
    /// </summary>
    private static BmfcSave? TryGetBmfcSaveFromStack(List<ElementWithState> elementStack,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        RecursiveVariableFinder rfv = new RecursiveVariableFinder(elementStack);

        string? fontValue = rfv.GetValueByBottomName("Font") as string;
        int? fontSize = rfv.GetValueByBottomName("FontSize") as int?;

        if (fontValue == null || fontSize == null)
        {
            return null;
        }

        int outlineValue = rfv.GetValueByBottomName("OutlineThickness") as int? ?? 0;
        bool fontSmoothing = rfv.GetValueByBottomName("UseFontSmoothing") as bool? ?? true;
        bool isItalic = rfv.GetValueByBottomName("IsItalic") as bool? ?? false;
        bool isBold = rfv.GetValueByBottomName("IsBold") as bool? ?? false;

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize.Value;
        bmfcSave.FontName = fontValue;
        bmfcSave.OutlineThickness = outlineValue;
        bmfcSave.UseSmoothing = fontSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.Ranges = fontRanges;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;

        return bmfcSave;
    }

    private async Task GenerateMissingFontsFor(GumProjectSave project, IEnumerable<ElementSave> elements,
        string projectDirectory, bool forceRecreate)
    {
        Dictionary<string, BmfcSave> bitmapFonts = CollectRequiredFonts(project, elements);

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

        int completed = 0;
        _callbacks.OnFontProgress(0, bitmapFonts.Count);

        foreach (KeyValuePair<string, BmfcSave> item in bitmapFonts)
        {
            System.Diagnostics.Debug.WriteLine($"Starting {item.Key}");
            Task task = TryCreateFontFor(item.Value, forceRecreate, showSpinner: false, createTask: true,
                projectDirectory, project.AutoSizeFontOutputs)
                .ContinueWith(_ =>
                {
                    int current = Interlocked.Increment(ref completed);
                    _callbacks.OnFontProgress(current, bitmapFonts.Count);
                });
            tasks.Add(task);
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

                toReturn.Succeeded = generateResponse.Succeeded;
                toReturn.Message = generateResponse.Message;
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

    /// <summary>
    /// Default no-op implementation used when no callbacks are supplied.
    /// </summary>
    private sealed class NoOpFontGenerationCallbacks : IFontGenerationCallbacks { }
}
