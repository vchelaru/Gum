using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
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

namespace Gum.Managers;

public class FontManager 
{
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;

    public FontManager(IGuiCommands guiCommands, 
        IFileCommands fileCommands)
    {
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
    }

    public string AbsoluteFontCacheFolder
    {
        get
        {
            return _fileCommands.ProjectDirectory + "FontCache/";
        }
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

            tasks.Add(TryCreateFontFor(item.Value, forceRecreate, showSpinner: false, createTask: true));

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
                createTask:false);
        }

        var container = stateSave.ParentContainer;

        if(container != null)
        {
            var references = ObjectFinder.Self.GetElementsReferencingRecursively(container);

            _=GenerateMissingFontsFor(gumProject, references, false);
        }
    }

    private BmfcSave? TryGetBmfcSaveFor(InstanceSave instance, StateSave stateSave, string fontRanges, int spacingHorizontal, int spacingVertical, StateSave forcedValues)
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

    private async Task<GeneralResponse> TryCreateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner, bool createTask)
    {
        AssignEstimatedNeededSizeOn(bmfcSave);

        var response = await CreateBitmapFontFilesIfNecessaryAsync(bmfcSave, force, false, showSpinner, createTask);

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

    async Task<OptionallyAttemptedGeneralResponse> CreateBitmapFontFilesIfNecessaryAsync(BmfcSave bmfcSave, bool force, bool forceMonoSpacedNumber, bool showSpinner, bool createTask)
    {

        var fntFileName = bmfcSave.FontCacheFileName;
        FilePath desiredFntFile = _fileCommands.ProjectDirectory + fntFileName;

        var toReturn = OptionallyAttemptedGeneralResponse.SuccessfulWithoutAttempt;

        if(_fileCommands.ProjectDirectory == null)
        {
            return OptionallyAttemptedGeneralResponse.UnsuccessfulWith("Project directory is null");
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


                FilePath filePathTemporary = (_fileCommands.ProjectDirectory! + fntFileName);
                string bmfcFileToSave = filePathTemporary.RemoveExtension() + ".bmfc";
                System.Console.WriteLine("Saving: " + bmfcFileToSave);

                var fileWatchManager = FileWatchManager.Self;

                // arbitrary wait time
                fileWatchManager.IgnoreNextChangeUntil(bmfcFileToSave);
                fileWatchManager.IgnoreNextChangeUntil(desiredFntFile);

                var pngFileNameBase = desiredFntFile.RemoveExtension();

                // we don't know how many files will be produced, so we just have to guess. For now, let's do 10, that should cover most cases:
                for (int i = 0; i < 10; i++)
                {
                    var pngWithNumber = $"{pngFileNameBase}_{i}.png";
                    fileWatchManager.IgnoreNextChangeUntil(pngWithNumber);
                }

                bmfcSave.Save(bmfcFileToSave);



                // Now call the executable
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = BmFontExeLocation;




                info.Arguments = "-c \"" + bmfcFileToSave + "\"" +
                    " -o \"" + _fileCommands.ProjectDirectory.FullPath + fntFileName + "\"";

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
                Process process = Process.Start(info);
                if (createTask)
                {
                    await WaitForExitAsync(process);
                }
                else
                {
                    process.WaitForExit();
                }

                if(desiredFntFile.Exists())
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

    string BmFontExeNoPath => "Gum.Libraries.bmfont.exe";

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

        void Process_Exited(object sender, EventArgs e)
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



    private void AssignEstimatedNeededSizeOn(BmfcSave bmfcSave)
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
