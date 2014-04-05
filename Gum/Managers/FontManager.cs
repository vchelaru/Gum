using Gum.DataTypes.Variables;
using Gum.ToolStates;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Managers
{
    public class FontManager : Singleton<FontManager>
    {
        string AbsoluteFontCacheFolder
        {
            get
            {
                return FileManager.RelativeDirectory + "FontCache/";
            }
        }


        public BitmapFont GetBitmapFontFor(string fontName, int fontSize)
        {
            string fileName = AbsoluteFontCacheFolder + "Font" + fontSize + fontName + ".fnt";

            if (System.IO.File.Exists(fileName))
            {
                try
                {

                    BitmapFont bitmapFont = (BitmapFont)LoaderManager.Self.GetDisposable(fileName);
                    if (bitmapFont == null)
                    {
                        bitmapFont = new BitmapFont(fileName, (SystemManagers)null);
                        LoaderManager.Self.AddDisposable(fileName, bitmapFont);
                    }

                    return bitmapFont;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void DeleteFontCacheFolder()
        {
            FileManager.DeleteDirectory(AbsoluteFontCacheFolder);
        }

        internal void ReactToFontValueSet()
        {

            StateSave stateSave = SelectedState.Self.SelectedStateSave;


            string prefix = "";
            if (SelectedState.Self.SelectedInstance != null)
            {
                prefix = SelectedState.Self.SelectedInstance.Name + ".";
            }

            object fontSizeAsObject = stateSave.GetValueRecursive(prefix + "FontSize");

            BmfcSave.CreateBitmapFontFilesIfNecessary(
                (int)fontSizeAsObject,
                (string)stateSave.GetValueRecursive(prefix + "Font"));
        }
    }
}
