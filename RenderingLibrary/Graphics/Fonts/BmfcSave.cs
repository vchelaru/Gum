using System.Reflection;
using System.Diagnostics;
using ToolsUtilities;
using System.IO;

namespace RenderingLibrary.Graphics.Fonts
{
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
            template = template.Replace("chars=32-126,160-255", $"chars={newRange}");

            FileManager.SaveText(template, fileName);
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

        public void CreateBitmapFontFilesIfNecessary(string fileName, bool force = false)
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

                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;

                Process process = Process.Start(info);

                while (!process.HasExited)
                {
                    System.Threading.Thread.Sleep(15);
                }

                string str;
                string output = null;
                string error = null;

                while ((str = process.StandardOutput.ReadLine()) != null)
                {
                    output += str + "\n";
                }

                while ((str = process.StandardError.ReadLine()) != null)
                {
                    error += str + "\n";
                }
            }
        }
    }
}
