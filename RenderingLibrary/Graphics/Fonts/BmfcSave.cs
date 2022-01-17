using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using ToolsUtilities;

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
        const string DefaultRanges = "32-126,160-255";
        public string Ranges = DefaultRanges;

        public void Save(string fileName)
        {
#if UWP
            throw new NotImplementedException();
#else
            var assembly2 = Assembly.GetEntryAssembly();

            string directory = FileManager.GetDirectory(assembly2.Location);

            string template = FileManager.FromFileText(directory + "Content/BmfcTemplate.bmfc");

            template = template.Replace("FontNameVariable", FontName);
            template = template.Replace("FontSizeVariable", FontSize.ToString());
            template = template.Replace("OutlineThicknessVariable", OutlineThickness.ToString());
            template = template.Replace("{UseSmoothing}", UseSmoothing ? "1" : "0");
            template = template.Replace("{IsItalic}", IsItalic ? "1" : "0");
            template = template.Replace("{IsBold}", IsBold ? "1" : "0");

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
#endif        
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
                fileName = "Font" + fontSize + fontName + "_o" + outline;
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
#if !UWP
        public static void CreateBitmapFontFilesIfNecessary(int fontSize, string fontName, int outline, bool fontSmoothing,
            bool isItalic = false, bool isBold = false, string fontRanges = DefaultRanges)
        {
            BmfcSave bmfcSave = new BmfcSave();
            bmfcSave.FontSize = fontSize;
            bmfcSave.FontName = fontName;
            bmfcSave.OutlineThickness = outline;
            bmfcSave.UseSmoothing = fontSmoothing;
            bmfcSave.IsItalic = isItalic;
            bmfcSave.IsBold = isBold;
            bmfcSave.Ranges = fontRanges;

            bmfcSave.CreateBitmapFontFilesIfNecessary(bmfcSave.FontCacheFileName);
        }

        public void CreateBitmapFontFilesIfNecessary(string fileName)
        {
            string resourceName = "RenderingLibrary.Libraries.bmfont.exe";
            string locationToSave = FileManager.RelativeDirectory + "Libraries\\bmfont.exe";

            if (!FileManager.FileExists(locationToSave))
            {
                FileManager.SaveEmbeddedResource(
                    Assembly.GetAssembly(typeof(BmfcSave)),
                    resourceName,
                    locationToSave);

            }

            string desiredFntFile = FileManager.RelativeDirectory + fileName;

            if (!FileManager.FileExists(desiredFntFile))
            {

                string bmfcFileToSave = FileManager.RelativeDirectory + FileManager.RemoveExtension(fileName) + ".bmfc";

                Save(bmfcFileToSave);



                // Now call the executable
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = locationToSave;



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
#endif
    }
}
