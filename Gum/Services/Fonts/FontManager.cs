using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Microsoft.VisualBasic.ApplicationServices;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToolsUtilities;

namespace Gum.Services.Fonts;

public class FontManager 
{
    System.Drawing.Point[] availableSizes = new System.Drawing.Point[]
    {
        new (32, 32),
        new (64, 64),
        new (128, 128),
        new (256, 256),
        new (512, 512),
        new (1024, 1024),
        new (2048, 1024),
        new (2048, 2048),
        new (4096, 2048),
        new (4096, 4096),
        new (8192, 4096),
        new (8192, 8192)
    };

    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly FileWatchManager _fileWatchManager;

    public string AbsoluteFontCacheFolder => _fileCommands.ProjectDirectory + "FontCache/";
    string BmFontExeNoPath => "Gum.Libraries.bmfont.exe";

    public FontManager(IGuiCommands guiCommands, 
        IFileCommands fileCommands,
        FileWatchManager fileWatchManager)
    {
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _fileWatchManager = fileWatchManager;
    }


    public void DeleteFontCacheFolder()
    {
        _fileCommands.DeleteDirectory(AbsoluteFontCacheFolder);
    }

    public async Task CreateAllMissingFontFiles(GumProjectSave project, bool forceRecreate = false)
    {
        var elements = project.AllElements;

        await GenerateMissingFontsFor(project, elements, forceRecreate);

    }

    /// <summary>
    /// Creates all fonts referenced by the argument element. Fonts are created if they do not exist, or if
    /// forceRecreate is true.
    /// </summary>
    /// <param name="project">The Gum project</param>
    /// <param name="elements">The elements that shoud have their fonts created.</param>
    /// <param name="forceRecreate">If true, fonts are created even if they are already on disk.</param>
    /// <returns>An awaitable task</returns>
    private async Task GenerateMissingFontsFor(GumProjectSave project, IEnumerable<ElementSave> elements, bool forceRecreate)
    {
        var fontRanges = project.FontRanges;
        var spacingHorizontal = project.FontSpacingHorizontal;
        var spacingVertical = project.FontSpacingVertical;

        Dictionary<string, BmfcSave> bitmapFonts = new Dictionary<string, BmfcSave>();

        foreach (var element in elements)
        {
            foreach (var state in element.AllStates)
            {
                BmfcSave? bmfcSave = TryGetBmfcSaveFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forcedValues: null);
                if (bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }
                foreach (var instance in element.Instances)
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
            _guiCommands.PrintOutput("No fonts to create");
        }
        else
        {
            _guiCommands.PrintOutput($"Checking {bitmapFonts.Count} font files...");
        }
        int countCreated = 0;
        var start = DateTime.Now;

        var window = _guiCommands.ShowSpinner();

        var parallelOptions = new ParallelOptions();
        parallelOptions.MaxDegreeOfParallelism = 16;

        List<Task> tasks = new List<Task>();

        if (bitmapFonts.Count > 0)
        {
            var assembly = GetType().Assembly;

            TrySaveBmFontExe(assembly);
        }


        foreach (var item in bitmapFonts)
        {
            System.Diagnostics.Debug.WriteLine($"Starting {item.Key}");

            tasks.Add(TryCreateFontFor(item.Value, forceRecreate, showSpinner: false, createTask: true, project.AutoSizeFontOutputs));
        }


        await Task.WhenAll(tasks);

        window.Hide();

        var end = DateTime.Now;
        var time = end - start;
        if (bitmapFonts.Count > 0)
        {
            _guiCommands.PrintOutput($"Created {countCreated} font files(s) in {time.TotalSeconds} seconds");
        }
    }

    internal void ReactToFontValueSet(InstanceSave instance, GumProjectSave gumProject, StateSave stateSave, StateSave forcedValues)
    {
        if (stateSave == null)
        {
            throw new InvalidOperationException($"{nameof(stateSave)} is null");
        }

        BmfcSave? bmfcSave = TryGetBmfcSaveFor(instance, stateSave, 
            gumProject.FontRanges, gumProject.FontSpacingHorizontal, gumProject.FontSpacingVertical, forcedValues);

        if (bmfcSave != null)
        {
            var assembly = GetType().Assembly;
            TrySaveBmFontExe(assembly);

            // We throw away the task, but internally no task is created so this runs synchronously.
            _=TryCreateFontFor(bmfcSave, force:false, 
                // This is usually pretty fast, so no need to show a spinner
                showSpinner:false, 
                // don't create a task, so that the function does not continue
                // until the font is recreated.
                createTask:false,
                gumProject.AutoSizeFontOutputs);
        }

        var container = stateSave.ParentContainer;

        if(container != null)
        {
            var references = ObjectFinder.Self.GetElementsReferencingRecursively(container);

            _=GenerateMissingFontsFor(gumProject, references, false);
        }
    }

    public BmfcSave? TryGetBmfcSaveFor(InstanceSave instance, StateSave stateSave, string fontRanges, int spacingHorizontal, int spacingVertical, StateSave forcedValues)
    {
        string prefix = "";
        if (instance != null)
        {
            prefix = instance.Name + ".";
        }

        int? fontSize = forcedValues?.GetValue("FontSize") as int? ?? stateSave.GetValueRecursive(prefix + "FontSize") as int?;
        var fontValue = forcedValues?.GetValue("Font") as string ?? stateSave.GetValueRecursive(prefix + "Font") as string;
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

    private async Task<GeneralResponse> TryCreateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner, bool createTask,
        bool iterativelyDetermineSize)
    {

        if(force || GetFilePath(bmfcSave, null).Exists() == false)
        {
           await AssignEstimatedNeededSizeOn(bmfcSave, iterativelyDetermineSize, _guiCommands.PrintOutput);
        }

        var response = await CreateBitmapFontFilesIfNecessaryAsync(bmfcSave, force, false, showSpinner, createTask, null);

        if(response.Succeeded == false)
        {
            var prefix = "Error creating font " + bmfcSave.FontName + " size " + bmfcSave.FontSize + ". ";

            if(!string.IsNullOrEmpty(response.Message))
            {
                _guiCommands.PrintOutput($"{prefix}" + response.Message);
            }
            else
            {
                _guiCommands.PrintOutput($"{prefix}Unknown error.");
            }
        }

        return response;
    }


    public async Task<GeneralResponse<System.Drawing.Point>> GetOptimizedSizeFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, Action<string>? callback)
    {
        // todo finish here

        int index = availableSizes.Length / 2;
        int minIndex = 0;
        int maxIndex = availableSizes.Length - 1;

        var guess = availableSizes[index];
        int bestIndexAt1Page = maxIndex;

        while(minIndex <= maxIndex)
        {
            bmfcSave.OutputWidth = guess.X;
            bmfcSave.OutputHeight = guess.Y;

            callback?.Invoke($"Testing {bmfcSave}, guessing {guess.X}x{guess.Y}...");

            var pageCount = await GetPageCountFor(bmfcSave, forceMonoSpacedNumber, showSpinner:false, createTask:true);



            if (pageCount.Succeeded == false)
            {
                return GeneralResponse<System.Drawing.Point>.UnsuccessfulWith(pageCount.Message);
            }
            else
            {
                callback?.Invoke($"{guess.X}x{guess.Y} requires {pageCount.Data} page(s)");
                if(pageCount.Data == 1)
                {
                    bestIndexAt1Page = Math.Min(bestIndexAt1Page, index);
                    maxIndex = index - 1;
                }
                else
                {
                    minIndex = index + 1;
                }

                // use geometric mean to get the new value:

                index = (minIndex + maxIndex) / 2;
                guess = availableSizes[index];

                if(minIndex <= maxIndex)
                {
                    callback?.Invoke($"Trying again with new guess {guess.X}x{guess.Y}...");
                }
            }
        }

        var toReturn = new GeneralResponse<System.Drawing.Point>();
        toReturn.Succeeded = true;
        toReturn.Data = availableSizes[bestIndexAt1Page];
        return toReturn;

    }

    public async Task<GeneralResponse<int>> GetPageCountFor(BmfcSave bmfcSave,
        bool forceMonoSpacedNumber, bool showSpinner, bool createTask)
    {
        string tempDirectory = System.IO.Path.GetTempPath();
        // or more specific:
        FilePath appTempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Gum") + 
            System.IO.Path.DirectorySeparatorChar;

        var createResponse = await CreateBitmapFontFilesIfNecessaryAsync(bmfcSave, 
            force:true, 
            forceMonoSpacedNumber, 
            showSpinner, createTask, appTempDirectory);


        if(createResponse.Succeeded == false)
        {
            return GeneralResponse<int>.UnsuccessfulWith(createResponse.Message);
        }
        else
        {
            // parse the fnt file, count the pages
            var fntFileName = bmfcSave.FontCacheFileName;
            FilePath desiredFntFile = System.IO.Path.Combine(appTempDirectory + fntFileName);

            try
            {
                using (var reader = new System.IO.StreamReader(desiredFntFile.FullPath))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Contains("pages="))
                        {
                            var pagesIndex = line.IndexOf("pages=");
                            var valueStart = pagesIndex + "pages=".Length;
                            var valueEnd = line.IndexOf(' ', valueStart);
                            if (valueEnd == -1)
                            {
                                valueEnd = line.Length;
                            }
                            
                            var pagesValue = line.Substring(valueStart, valueEnd - valueStart);
                            if (int.TryParse(pagesValue, out int pageCount))
                            {
                                var toReturn = GeneralResponse<int>.SuccessfulResponse;
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
            }
            catch (Exception ex)
            {
                return GeneralResponse<int>.UnsuccessfulWith($"Error reading font file: {ex.Message}");
            }
        }
    }

    FilePath GetFilePath(BmfcSave bmfcSave, FilePath? destinationDirectory)
    {
        var fntFileName = bmfcSave.FontCacheFileName;

        FilePath desiredFntFile = destinationDirectory != null
            ? destinationDirectory + fntFileName
            : _fileCommands.ProjectDirectory + fntFileName;

        return desiredFntFile;
    }

    async Task<OptionallyAttemptedGeneralResponse> CreateBitmapFontFilesIfNecessaryAsync(BmfcSave bmfcSave, 
        bool force, bool forceMonoSpacedNumber, bool showSpinner, bool createTask, FilePath? destinationDirectory)
    {
        FilePath desiredFntFile = GetFilePath(bmfcSave, destinationDirectory);

        var toReturn = OptionallyAttemptedGeneralResponse.SuccessfulWithoutAttempt;

        if(_fileCommands.ProjectDirectory == null)
        {
            return OptionallyAttemptedGeneralResponse.UnsuccessfulWith("Project directory is null, has the project been saved?");
        }

        Window? spinner = null;

        try
        {
            if (!desiredFntFile.Exists() || force)
            {
                if (showSpinner)
                {
                    spinner = _guiCommands.ShowSpinner();
                }


                FilePath filePathTemporary = desiredFntFile;
                string bmfcFileToSave = filePathTemporary.RemoveExtension() + ".bmfc";
                System.Console.WriteLine("Saving: " + bmfcFileToSave);

                // arbitrary wait time
                _fileWatchManager.IgnoreNextChangeUntil(bmfcFileToSave);
                _fileWatchManager.IgnoreNextChangeUntil(desiredFntFile);

                var pngFileNameBase = desiredFntFile.RemoveExtension();

                // we don't know how many files will be produced, so we just have to guess.
                const int pagesToIgnore = 99;
                for (int i = 0; i < pagesToIgnore; i++)
                {
                    var pngWithNumber = $"{pngFileNameBase}_{i}.png";
                    _fileWatchManager.IgnoreNextChangeUntil(pngWithNumber);
                    
                    // numbers can be 00 or 0
                    pngWithNumber = $"{pngFileNameBase}_{i:00}.png";
                    _fileWatchManager.IgnoreNextChangeUntil(pngWithNumber);
                }

                bmfcSave.Save(bmfcFileToSave);



                // Now call the executable
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = BmFontExeLocation;




                info.Arguments = "-c \"" + bmfcFileToSave + "\"" +
                    " -o \"" + desiredFntFile + "\"";

                info.UseShellExecute = true;

                //info.UseShellExecute = false;
                //info.RedirectStandardError = true;
                //info.RedirectStandardInput = true;
                //info.RedirectStandardOutput = true;
                //info.CreateNoWindow = true;

                var filenameAndArgs = $"{info.FileName} {info.Arguments}";
                System.Diagnostics.Debug.WriteLine($"Running: {filenameAndArgs}");
                _guiCommands.PrintOutput(filenameAndArgs);

                // This is okay on .NET 8 because it doesn't use the shell - it's a direct exe call
                Process? process = Process.Start(info);

                if(process != null)
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

                if(process == null)
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
            spinner?.Hide();
        }

        return toReturn;
    }


    string BmFontExeLocation
    {
        get
        {
            string resourceName = BmFontExeNoPath;

            FilePath assemblyLocation = Assembly.GetExecutingAssembly().Location;

            string mainExecutablePath = assemblyLocation.GetDirectoryContainingThis().FullPath;


            string bmFontExeLocation = System.IO.Path.Combine(mainExecutablePath, "Libraries\\bmfont.exe");

            return bmFontExeLocation;
        }
    }

    private void TrySaveBmFontExe(Assembly assemblyContainingBitmapFontGenerator)
    {
        FilePath bmFontExeLocation = BmFontExeLocation;
        if (!bmFontExeLocation.Exists())
        {
            _fileCommands.SaveEmbeddedResource(
                assemblyContainingBitmapFontGenerator,
                BmFontExeNoPath,
                bmFontExeLocation.FullPath);

        }
    }

    async Task<int> WaitForExitAsync(Process process, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
            // This is expected when trying to enable events after the process has already exited.
            // Simply ignore this case.
            // Allow the exception to bubble in all other cases.
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



    private async Task AssignEstimatedNeededSizeOn(BmfcSave bmfcSave, bool iterativelyDetermineSize,
         Action<string> updateCallback)
    {
        bool handledIteratively = false;

        if(iterativelyDetermineSize)
        {
            var optimizedSize = await GetOptimizedSizeFor(bmfcSave, forceMonoSpacedNumber:false, updateCallback);

            if(optimizedSize.Succeeded)
            {
                bmfcSave.OutputWidth = optimizedSize.Data.X;
                bmfcSave.OutputHeight = optimizedSize.Data.Y;

                handledIteratively = true;
            }
        }

        if(!handledIteratively)
        {
            int spacingHorizontal = bmfcSave.SpacingHorizontal;
            int spacingVertical = bmfcSave.SpacingVertical;
            int fontSize = bmfcSave.FontSize;
            bool isBold = bmfcSave.IsBold;

            int numberWide, numberTall;

            var effectiveFontSize = fontSize + System.Math.Max(spacingHorizontal, spacingVertical) +
                bmfcSave.OutlineThickness*2;
            if (isBold)
            {
                effectiveFontSize += (int)(fontSize * 0.08);
            }
        

            EstimateBlocksNeeded(out numberWide, out numberTall, effectiveFontSize);

            bmfcSave.OutputWidth = numberWide * 256;
            bmfcSave.OutputHeight = numberTall * 256;
        }
    }

    private void EstimateBlocksNeeded(out int numberWide, out int numberTall, int effectiveFontSize)
    {
        // todo - eventually this should look at the output and adjust in response. For now, we'll just estimate
        // based on the font size
        int numberOf256Blocks = 2;

        if(effectiveFontSize < 20)
        {
            numberOf256Blocks = 1;
        }
        else if(effectiveFontSize < 35)
        {
            numberOf256Blocks = 2;
        }
        else if(effectiveFontSize < 47)
        {
            numberOf256Blocks = 3;
        }
        else if(effectiveFontSize < 56)
        {
            numberOf256Blocks = 4;
        }
        // Comic with an outline at 61 is too big for 5 blocks
        //else if (effectiveFontSize < 63)
        else if (effectiveFontSize < 61)

        {
            numberOf256Blocks = 5;
        }
        // futura 65 with 3 spacing barely spills over
        //else if (effectiveFontSize < 70)
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
        else if(effectiveFontSize < 131)
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
        else if ((numberOf256Blocks %3) == 0 && numberOf256Blocks / 3 < 8)
        {
            numberWide = 3;
            numberTall = numberOf256Blocks / 3;
        }
        else if((numberOf256Blocks % 2) == 0 && numberOf256Blocks / 2 < 8)
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
}
