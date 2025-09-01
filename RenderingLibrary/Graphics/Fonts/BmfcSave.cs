using System.Reflection;
using System.Diagnostics;
using ToolsUtilities;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RenderingLibrary.Graphics.Fonts;

public class BmfcSave
{
    public string FontName = "Arial";
    public int FontSize = 20;
    public int OutlineThickness = 0;
    public bool UseSmoothing = true;
    public bool IsItalic = false;
    public bool IsBold = false;
    public int SpacingHorizontal = 1;
    public int SpacingVertical = 1;
    const string DefaultRanges = "32-126,160-255";
    public string Ranges = DefaultRanges;
    public int OutputWidth = 512;
    public int OutputHeight = 256;

    public override string ToString()
    {
        return $"{FontName} {FontSize}";
    }

    public void Save(string fileName)
    {
        var assembly2 = Assembly.GetEntryAssembly();

        string directory = FileManager.GetDirectory(assembly2.Location);

        var bmfcTemplateFullPath =
            directory + "Content/BmfcTemplate.bmfc";

        if(!System.IO.File.Exists(bmfcTemplateFullPath))
        {
            throw new FileNotFoundException(bmfcTemplateFullPath);
        }

        string template = FileManager.FromFileText(bmfcTemplateFullPath);

        template = template.Replace("FontNameVariable", FontName);
        template = template.Replace("FontSizeVariable", FontSize.ToString());
        template = template.Replace("OutlineThicknessVariable", OutlineThickness.ToString());
        template = template.Replace("{UseSmoothing}", UseSmoothing ? "1" : "0");
        template = template.Replace("{IsItalic}", IsItalic ? "1" : "0");
        template = template.Replace("{IsBold}", IsBold ? "1" : "0");

        template = template.Replace("{SpacingHorizontal}", SpacingHorizontal.ToString());
        template = template.Replace("{SpacingVertical}", SpacingVertical.ToString());
        template = template.Replace("{OutputWidth}", OutputWidth.ToString());
        template = template.Replace("{OutputHeight}", OutputHeight.ToString());



        //alphaChnl=alphaChnlValue
        //redChnl=redChnlValue
        //greenChnl=greenChnlValue
        //blueChnl=blueChnlValue
        if (OutlineThickness == 0)
        {
            template = template.Replace("alphaChnlValue", "0");
            template = template.Replace("redChnlValue", "4");
            template = template.Replace("greenChnlValue", "4");
            template = template.Replace("blueChnlValue", "4");
        }
        else
        {
            template = template.Replace("alphaChnlValue", "1");
            template = template.Replace("redChnlValue", "0");
            template = template.Replace("greenChnlValue", "0");
            template = template.Replace("blueChnlValue", "0");
        }

        var newRange = Ranges;

        var isValidRange = GetIfIsValidRange(newRange);

        if(!isValidRange)
        {
            newRange = DefaultRanges;
        }
        
        else
        {
            newRange = EnsureRangesContainSpace(newRange);
        }
        
        var charsReplacement = GenerateSplitRangesString(newRange);
        template = template.Replace("chars=32-126,160-255", charsReplacement);

        FileManager.SaveText(template, fileName);
    }
    
    private static string GenerateSplitRangesString(string ranges, int maxBlocksPerLine = 10)
    {
        var allChars = ParseCharRanges(ranges);
        var blocks = ConvertToRanges(allChars);

        var builder = new StringBuilder();
        for(int i = 0; i < blocks.Count; i++)
        {
            if(i % maxBlocksPerLine == 0)
            {
                if(i > 0)
                {
                    builder.Append(Environment.NewLine);
                }
                builder.Append("chars=");
            }
            else
            {
                builder.Append(',');
            }

            var block = blocks[i];
            if(block.start == block.end)
            {
                builder.Append(block.start);
            }
            else
            {
                builder.Append(block.start).Append('-').Append(block.end);
            }
        }
        return builder.ToString();
    }

    private static List<int> ParseCharRanges(string charsStr)
    {
        var allChars = new List<int>();
        var ranges = charsStr.Split([','], StringSplitOptions.RemoveEmptyEntries);
        foreach(var part in ranges)
        {
            if(part.Contains('-'))
            {
                var split = part.Split('-');
                if(int.TryParse(split[0], out int start) && int.TryParse(split[1], out int end))
                {
                    for(int i = start; i <= end; i++)
                    {
                        allChars.Add(i);
                    }
                }
            }
            else if(int.TryParse(part, out int value))
            {
                allChars.Add(value);
            }
        }
        return allChars;
    }

    static List<(int start, int end)> ConvertToRanges(List<int> codes)
    {
        var ranges = new List<(int start, int end)>();
        if(codes.Count == 0)
        {
            return ranges;
        }

        codes.Sort();
        int start = codes[0];
        int prev = codes[0];
        for(int i = 1; i < codes.Count; i++)
        {
            int codepoint = codes[i];
            if(codepoint == prev + 1)
            {
                prev = codepoint;
            }
            else
            {
                ranges.Add((start, prev));
                start = prev = codepoint;
            }
        }
        ranges.Add((start, prev));
        return ranges;
    }
    
    public static string EnsureRangesContainSpace(string ranges)
    {
        const int spaceChar = (int)' ';

        if (string.IsNullOrEmpty(ranges))
        {
            return spaceChar.ToString();
        }

        bool containsSpace = ranges.Split(',').Any(part =>
        {
            if (part.Contains('-'))
            {
                var split = part.Split('-');
                if (int.TryParse(split[0], out var start) && int.TryParse(split[1], out var end))
                {
                    return spaceChar >= start && spaceChar <= end;
                }
            }
            else if (int.TryParse(part, out var value))
            {
                return value == spaceChar;
            }
            return false;
        });

        if (!containsSpace)
        {
            ranges = spaceChar + "," + ranges;
        }

        return ranges;
    }
    
    public static bool GetIfIsValidRange(string newRange)
    {
        try
        {
            if(newRange?.Contains(" ") == true)
            {
                return false; // no spaces allowed, bmfontgenerator doesn't like it
            }
            var individualRanges = newRange.Split(',');

            if(individualRanges.Length == 0)
            {
                return false;
            }
            foreach(var individualRange in individualRanges)
            {
                if(individualRange.Contains("-"))
                {
                    var splitNumbers = individualRange.Split('-');

                    if(splitNumbers.Length != 2)
                    {
                        return false;
                    }
                    else
                    {
                        var firstParsed = int.TryParse(splitNumbers[0], out int result1);
                        var secondParsed = int.TryParse(splitNumbers[1], out int result2);

                        if(result1 >= result2)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // this should be a regular number:
                    var didParseCorrectly = int.TryParse(individualRange, out int parseResult);

                    if(!didParseCorrectly)
                    {
                        return false;
                    }
                }
            }

            return true;

        }
        catch
        {
            return false;
        }
    }

    public static string TryFixRange(string oldRange)
    {
        string newRange = string.Empty;
        if(!string.IsNullOrEmpty(oldRange))
        {
            newRange = oldRange.Replace(" ", string.Empty);
        }
        return newRange;
    }
    
    public static string GenerateRangesFromFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var uniqueValues = new System.Collections.Generic.HashSet<int>();
        foreach (var c in text)
        {
            uniqueValues.Add((int)c);
        }

        var ordered = uniqueValues.OrderBy(item => item).ToList();
        var builder = new System.Text.StringBuilder();

        void AppendRange(int start, int end)
        {
            if(builder.Length > 0)
            {
                builder.Append(',');
            }
            if(start == end)
            {
                builder.Append(start);
            }
            else
            {
                builder.Append(start).Append('-').Append(end);
            }
        }

        int currentStart = -1;
        int previous = -1;
        foreach(var val in ordered)
        {
            if(currentStart == -1)
            {
                currentStart = previous = val;
            }
            else if(val == previous + 1)
            {
                previous = val;
            }
            else
            {
                AppendRange(currentStart, previous);
                currentStart = previous = val;
            }
        }

        if(currentStart != -1)
        {
            AppendRange(currentStart, previous);
        }

        return builder.ToString();
    }
    
    public string FontCacheFileName
    {
        get
        {
            return GetFontCacheFileNameFor(FontSize, FontName, OutlineThickness, UseSmoothing, IsItalic, IsBold);
        }

    }

    public static string GetFontCacheFileNameFor(int fontSize, string fontName, int outline, bool useFontSmoothing,
        bool isItalic = false, bool isBold = false)
    {
        string fileName = null;


        // don't allow some charactersin the file name:
        fontName = fontName.Replace(' ', '_');

        fileName = "Font" + fontSize + fontName;
        if (outline != 0)
        {
            fileName += "_o" + outline;
        }

        if(useFontSmoothing == false)
        {
            fileName += "_noSmooth";
        }

        if(isItalic)
        {
            fileName += "_Italic";
        }

        if(isBold)
        {
            fileName += "_Bold";
        }

        fileName += ".fnt";

        fileName = System.IO.Path.Combine("FontCache", fileName);

        return fileName;
    }


    /// <summary>
    /// Waits asynchronously for the process to exit.
    /// </summary>
    /// <param name="process">The process to wait for cancellation.</param>
    /// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
    /// immediately as canceled.</param>
    /// <returns>A Task representing waiting for the process to end.</returns>
    [Obsolete("This is only used when creating bitmap fonts with bitmap font generator's .exe, which is getting moved out of this class")]
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

    // tool-necessary implementations
    public static void CreateBitmapFontFilesIfNecessary(int fontSize, string fontName, int outline, bool fontSmoothing,
        bool isItalic = false, bool isBold = false, string fontRanges = DefaultRanges, int spacingHorizontal = 1, int spacingVertical = 1)
    {
        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize;
        bmfcSave.FontName = fontName;
        bmfcSave.OutlineThickness = outline;
        bmfcSave.UseSmoothing = fontSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.Ranges = fontRanges;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;

        bmfcSave.CreateBitmapFontFilesIfNecessary(bmfcSave.FontCacheFileName, force:false);
    }

    public bool CreateBitmapFontFilesIfNecessary(string fileName, bool force = false, bool forceMonoSpacedNumber = false)
    {
        string resourceName = "RenderingLibrary.Libraries.bmfont.exe";

        string mainExecutablePath = FileManager.GetDirectory( Assembly.GetExecutingAssembly().Location );


        string bmFontExeLocation = Path.Combine(mainExecutablePath, "Libraries\\bmfont.exe");

        if (!FileManager.FileExists(bmFontExeLocation))
        {
            FileManager.SaveEmbeddedResource(
                Assembly.GetAssembly(typeof(BmfcSave)),
                resourceName,
                bmFontExeLocation);

        }

        string desiredFntFile = FileManager.RelativeDirectory + fileName;

        var didCreate = false;

        if (!FileManager.FileExists(desiredFntFile) || force)
        {

            string bmfcFileToSave = FileManager.RelativeDirectory + FileManager.RemoveExtension(fileName) + ".bmfc";
            System.Console.WriteLine("Saving: " + bmfcFileToSave);

            Save(bmfcFileToSave);



            // Now call the executable
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = bmFontExeLocation;


            info.Arguments = "-c \"" + bmfcFileToSave + "\"" +
                " -o \"" + FileManager.RelativeDirectory + fileName + "\"";

            System.Diagnostics.Debug.WriteLine("   " + info.FileName + " " + info.Arguments);
            info.UseShellExecute = true;

            //info.UseShellExecute = false;
            //info.RedirectStandardError = true;
            //info.RedirectStandardInput = true;
            //info.RedirectStandardOutput = true;
            //info.CreateNoWindow = true;

            Process process = Process.Start(info);
            process.WaitForExit(60_000);
            didCreate = true;
        }

        return didCreate;
    }

    // todo - this needs to move to Gum Tool and not GumCommon!!
    [Obsolete("This should be moved to the Gum Tool, not the common library. Do not call this, it will go away in future versions of Guml", error:true)]
    public async Task<bool> CreateBitmapFontFilesIfNecessaryAsync(string fileName, Assembly assemblyContainingBitmapFontGenerator,
        bool force = false, 
        bool forceMonoSpacedNumber = false)
    {
        string resourceName = "Gum.Libraries.bmfont.exe";

        string mainExecutablePath = FileManager.GetDirectory(Assembly.GetExecutingAssembly().Location);


        string bmFontExeLocation = Path.Combine(mainExecutablePath, "Libraries\\bmfont.exe");

        if (!FileManager.FileExists(bmFontExeLocation))
        {
            FileManager.SaveEmbeddedResource(
                assemblyContainingBitmapFontGenerator,
                resourceName,
                bmFontExeLocation);

        }

        string desiredFntFile = FileManager.RelativeDirectory + fileName;

        var didCreate = false;

        if (!FileManager.FileExists(desiredFntFile) || force)
        {

            string bmfcFileToSave = FileManager.RelativeDirectory + FileManager.RemoveExtension(fileName) + ".bmfc";
            System.Console.WriteLine("Saving: " + bmfcFileToSave);

            Save(bmfcFileToSave);



            // Now call the executable
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = bmFontExeLocation;

            System.Console.WriteLine("Running: " + info.FileName + " " + info.Arguments);

            info.Arguments = "-c \"" + bmfcFileToSave + "\"" +
                " -o \"" + FileManager.RelativeDirectory + fileName + "\"";

            info.UseShellExecute = true;

            //info.UseShellExecute = false;
            //info.RedirectStandardError = true;
            //info.RedirectStandardInput = true;
            //info.RedirectStandardOutput = true;
            //info.CreateNoWindow = true;


            Process process = Process.Start(info);
            await WaitForExitAsync(process);
            didCreate = true;
        }

        return didCreate;
    }


}
