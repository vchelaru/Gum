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

        public void Save(string fileName)
        {
            string template = FileManager.FromFileText("Content/BmfcTemplate.bmfc");

            template = template.Replace("FontNameVariable", FontName);
            template = template.Replace("FontSizeVariable", FontSize.ToString());


            FileManager.SaveText(template, fileName);

        }

        public static void CreateBitmapFontFilesIfNecessary(int fontSize, string fontName)
        {
                BmfcSave bmfcSave = new BmfcSave();
                bmfcSave.FontSize = fontSize;
                bmfcSave.FontName = fontName;
                bmfcSave.CreateBitmapFontFilesIfNecessary("Font" + bmfcSave.FontSize + bmfcSave.FontName + ".fnt");
        }

        public void CreateBitmapFontFilesIfNecessary(string fileName)
        {
            string resourceName = "RenderingLibrary.Libraries.bmfont.exe";
            string locationToSave = FileManager.RelativeDirectory  + "Libraries\\bmfont.exe";

            if (!System.IO.File.Exists(locationToSave))
            {
                FileManager.SaveEmbeddedResource(
                    Assembly.GetAssembly(typeof(BmfcSave)),
                    resourceName,
                    locationToSave);

            }

            string desiredFntFile = FileManager.RelativeDirectory + fileName;

            if (!System.IO.File.Exists(desiredFntFile))
            {

                string bmfcFileToSave = FileManager.RelativeDirectory + FileManager.RemoveExtension(fileName) + ".bmfc";

                Save(bmfcFileToSave);



                // Now call the executable
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = locationToSave.Replace("\\", "/");



                info.Arguments = "-c \"" + bmfcFileToSave + "\"" +
                    " -o \"" + FileManager.RelativeDirectory + fileName + "\"";
                info.Arguments = info.Arguments.Replace("\\", "/");

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
