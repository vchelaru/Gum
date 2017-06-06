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

        public void Save(string fileName)
        {
#if WINDOWS_8 || UWP
            throw new NotImplementedException();
#else
            var assembly2 = Assembly.GetEntryAssembly();

            string directory = FileManager.GetDirectory(assembly2.Location);

            string template = FileManager.FromFileText(directory + "Content/BmfcTemplate.bmfc");

            template = template.Replace("FontNameVariable", FontName);
            template = template.Replace("FontSizeVariable", FontSize.ToString());
            template = template.Replace("OutlineThicknessVariable", OutlineThickness.ToString());

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

            FileManager.SaveText(template, fileName);
#endif        
        }

        public string FontCacheFileName
        {
            get
            {
                return GetFontCacheFileNameFor(FontSize, FontName, OutlineThickness);
            }

        }

        public static string GetFontCacheFileNameFor(int fontSize, string fontName, int outline)
        {
            string fileName = null;
            if (outline == 0)
            {
                fileName = "Font" + fontSize + fontName + ".fnt";
            }
            else
            {
                fileName = "Font" + fontSize + fontName + "_o" + outline + ".fnt";
            }

            fileName = System.IO.Path.Combine("FontCache", fileName);

            return fileName;
        }




        // tool-necessary implementations
#if !WINDOWS_8 && !UWP
        public static void CreateBitmapFontFilesIfNecessary(int fontSize, string fontName, int outline)
        {
            BmfcSave bmfcSave = new BmfcSave();
            bmfcSave.FontSize = fontSize;
            bmfcSave.FontName = fontName;
            bmfcSave.OutlineThickness = outline;


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
