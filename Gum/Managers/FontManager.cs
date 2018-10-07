using Gum.DataTypes;
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

        public BitmapFont GetBitmapFontFor(string fontName, int fontSize, int outlineThickness, bool useFontSmoothing)
        {
            string fileName = AbsoluteFontCacheFolder + 
                FileManager.RemovePath(BmfcSave.GetFontCacheFileNameFor(fontSize, fontName, outlineThickness, useFontSmoothing));

            if (FileManager.FileExists(fileName))
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
            try
            {
                FileManager.DeleteDirectory(AbsoluteFontCacheFolder);
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error deleting font cache:\n" + e.ToString());
            }
        }

        public void CreateAllMissingFontFiles(GumProjectSave project)
        {
            foreach(var element in project.StandardElements)
            {
                foreach(var state in element.AllStates)
                {
                    TryCreateFontFileFor(null, state);

                    // standard elements don't have instances
                }
            }
            foreach (var component in project.Components)
            {
                foreach (var state in component.AllStates)
                {
                    TryCreateFontFileFor(null, state);

                    foreach(var instance in component.Instances)
                    {
                        TryCreateFontFileFor(instance, state);
                    }
                }
            }
            foreach(var screen in project.Screens)
            {
                foreach (var state in screen.AllStates)
                {
                    TryCreateFontFileFor(null, state);

                    foreach (var instance in screen.Instances)
                    {
                        TryCreateFontFileFor(instance, state);
                    }
                }
            }
        }

        internal void ReactToFontValueSet(InstanceSave instance)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;

            // If the user has a category selected but no state in the category, then use the default:
            if (stateSave == null && SelectedState.Self.SelectedStateCategorySave != null)
            {
                stateSave = SelectedState.Self.SelectedElement.DefaultState;
            }

            if (stateSave == null)
            {
                throw new InvalidOperationException($"{nameof(stateSave)} is null");
            }

            TryCreateFontFileFor(instance, stateSave);
        }

        public void TryCreateFontFileFor(InstanceSave instance, StateSave stateSave)
        {
            string prefix = "";
            if (instance != null)
            {
                prefix = instance.Name + ".";
            }

            int? fontSize = null;
            object fontSizeAsObject = stateSave.GetValueRecursive(prefix + "FontSize");
            if(fontSizeAsObject != null)
            {
                fontSize = (int)fontSizeAsObject;
            }

            var fontValue =
                (string)stateSave.GetValueRecursive(prefix + "Font");

            int? outlineValue = 0;
            var outlineValueAsObject = stateSave.GetValueRecursive(prefix + "OutlineThickness");
            if (outlineValueAsObject != null)
            {
                outlineValue = (int)outlineValueAsObject;
            }

            // default to true to match how old behavior worked
            bool fontSmoothing = true;
            var fontSmoothingAsObject = stateSave.GetValueRecursive(prefix + "UseFontSmoothing");
            if (fontSmoothingAsObject != null)
            {
                fontSmoothing = (bool)fontSmoothingAsObject;
            }

            if (fontValue != null && fontSize != null && outlineValue != null)
            {
                BmfcSave.CreateBitmapFontFilesIfNecessary(
                    fontSize.Value,
                    fontValue,
                    outlineValue.Value,
                    fontSmoothing);
            }
        }
    }
}
