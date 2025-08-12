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
using System.Management.Instrumentation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToolsUtilities;

namespace Gum.Managers;

public class FontManager 
{
    private readonly IGuiCommands _guiCommands;

    public FontManager(IGuiCommands guiCommands)
    {
        _guiCommands = guiCommands;
    }

    public string AbsoluteFontCacheFolder
    {
        get
        {
            return FileManager.RelativeDirectory + "FontCache/";
        }
    }

    //public BitmapFont GetBitmapFontFor(string fontName, int fontSize, int outlineThickness, bool useFontSmoothing, bool isItalic = false, 
    //    bool isBold = false)
    //{
    //    string fileName = AbsoluteFontCacheFolder + 
    //        FileManager.RemovePath(BmfcSave.GetFontCacheFileNameFor(fontSize, fontName, outlineThickness, useFontSmoothing, isItalic, isBold));

    //    if (FileManager.FileExists(fileName))
    //    {
    //        try
    //        {

    //            BitmapFont bitmapFont = (BitmapFont)LoaderManager.Self.GetDisposable(fileName);
    //            if (bitmapFont == null)
    //            {
    //                bitmapFont = new BitmapFont(fileName, (SystemManagers)null);
    //                LoaderManager.Self.AddDisposable(fileName, bitmapFont);
    //            }

    //            return bitmapFont;
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    public void DeleteFontCacheFolder()
    {
        FileManager.DeleteDirectory(AbsoluteFontCacheFolder);
    }

    public async Task CreateAllMissingFontFiles(GumProjectSave project, bool forceRecreate = false)
    {
        var fontRanges = project.FontRanges;
        var spacingHorizontal = project.FontSpacingHorizontal;
        var spacingVertical = project.FontSpacingVertical;


        Dictionary<string, BmfcSave> bitmapFonts = new Dictionary<string, BmfcSave>();

        foreach (var element in project.StandardElements)
        {
            foreach(var state in element.AllStates)
            {
                BmfcSave? bmfcSave = TryGetBmfcSaveFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forcedValues:null);

                if(bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }    
                // standard elements don't have instances
            }
        }
        foreach (var element in project.Components)
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
                    if(bmfcSaveInner != null)
                    {
                        bitmapFonts[bmfcSaveInner.FontCacheFileName] = bmfcSaveInner;
                    }
                }
            }
        }
        foreach(var element in project.Screens)
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

        // Vic says 
        // Parallel.ForEach was used here to attept to speed up
        // font creation. However, doing this either makes the font
        // creation the same speed, or even slower. I have no idea why
        // I'm leaing this comment here and maybe someone else can help
        // solve this issue.

        var parallelOptions = new ParallelOptions();
        parallelOptions.MaxDegreeOfParallelism = 16;

        List<Task> tasks = new List<Task>();

        if(bitmapFonts.Count > 0)
        {
            var assembly = GetType().Assembly;

            TrySaveBmFontExe(assembly);
        }

        foreach (var item in bitmapFonts)

        {

            System.Diagnostics.Debug.WriteLine($"Starting {item.Key}");

            tasks.Add(TryCreateFontFor(item.Value, forceRecreate, showSpinner:false, createTask:true));

        }

        await Task.WhenAll(tasks);

        window.Hide();

        var end = DateTime.Now;
        var time = end - start;
        if(bitmapFonts.Count > 0)
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
    }

    private static BmfcSave? TryGetBmfcSaveFor(InstanceSave instance, StateSave stateSave, string fontRanges, int spacingHorizontal, int spacingVertical, StateSave forcedValues)
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

    private async Task<bool> TryCreateFontFor(BmfcSave bmfcSave, bool force, bool showSpinner, bool createTask)
    {
        EstimateNeededDimensions(bmfcSave);
        var didCreate = await CreateBitmapFontFilesIfNecessaryAsync(bmfcSave, force, false, showSpinner, createTask);
        return didCreate;
    }

    async Task<bool> CreateBitmapFontFilesIfNecessaryAsync(BmfcSave bmfcSave, bool force, bool forceMonoSpacedNumber, bool showSpinner, bool createTask)
    {

        var fntFileName = bmfcSave.FontCacheFileName;
        FilePath desiredFntFile = FileManager.RelativeDirectory + fntFileName;

        var didCreate = false;

        if (!desiredFntFile.Exists() || force)
        {
            Window? spinner = null;
            if(showSpinner)
            {
                spinner = _guiCommands.ShowSpinner();
            }
            string bmfcFileToSave = FileManager.RelativeDirectory + FileManager.RemoveExtension(fntFileName) + ".bmfc";
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
                " -o \"" + FileManager.RelativeDirectory + fntFileName + "\"";

            info.UseShellExecute = true;

            //info.UseShellExecute = false;
            //info.RedirectStandardError = true;
            //info.RedirectStandardInput = true;
            //info.RedirectStandardOutput = true;
            //info.CreateNoWindow = true;

            var filenameAndArgs = $"{info.FileName} {info.Arguments}";
            System.Diagnostics.Debug.WriteLine($"Running: {filenameAndArgs}");
            _guiCommands.PrintOutput(filenameAndArgs);


            Process process = Process.Start(info);
            if(createTask)
            {
                await WaitForExitAsync(process);
            }
            else
            {
                process.WaitForExit();
            }
            didCreate = true;

            if(spinner != null)
            {
                spinner.Hide();
            }
        }

        return didCreate;
    }

    static string BmFontExeNoPath => "Gum.Libraries.bmfont.exe";

    static string BmFontExeLocation
    {
        get
        {
            string resourceName = BmFontExeNoPath;

            string mainExecutablePath = FileManager.GetDirectory(Assembly.GetExecutingAssembly().Location);


            string bmFontExeLocation = System.IO.Path.Combine(mainExecutablePath, "Libraries\\bmfont.exe");

            return bmFontExeLocation;
        }
    }

    private static void TrySaveBmFontExe(Assembly assemblyContainingBitmapFontGenerator)
    {
        var bmFontExeLocation = BmFontExeLocation;
        if (!FileManager.FileExists(bmFontExeLocation))
        {
            FileManager.SaveEmbeddedResource(
                assemblyContainingBitmapFontGenerator,
                BmFontExeNoPath,
                bmFontExeLocation);

        }
    }

    static async Task<int> WaitForExitAsync(Process process, CancellationToken cancellationToken = default)
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



    private static void EstimateNeededDimensions(BmfcSave bmfcSave)
    {
        int spacingHorizontal = bmfcSave.SpacingHorizontal;
        int spacingVertical = bmfcSave.SpacingVertical;
        int fontSize = bmfcSave.FontSize;
        bool isBold = bmfcSave.IsBold;

        int numberWide, numberTall;

        var effectiveFontSize = fontSize + System.Math.Max(spacingHorizontal, spacingVertical);
        if (isBold)
        {
            effectiveFontSize += (int)(fontSize * 0.08);
        }

        EstimateBlocksNeeded(out numberWide, out numberTall, effectiveFontSize);

        bmfcSave.OutputWidth = numberWide * 256;
        bmfcSave.OutputHeight = numberTall * 256;
    }

    private static void EstimateBlocksNeeded(out int numberWide, out int numberTall, int effectiveFontSize)
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
        else if (effectiveFontSize < 63)
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
