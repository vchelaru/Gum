using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using ToolsUtilities;

namespace Gum.Managers
{
    public class FontManager : Singleton<FontManager>
    {
        public string AbsoluteFontCacheFolder
        {
            get
            {
                return FileManager.RelativeDirectory + "FontCache/";
            }
        }

        public BitmapFont GetBitmapFontFor(string fontName, int fontSize, int outlineThickness, bool useFontSmoothing, bool isItalic = false, 
            bool isBold = false)
        {
            string fileName = AbsoluteFontCacheFolder + 
                FileManager.RemovePath(BmfcSave.GetFontCacheFileNameFor(fontSize, fontName, outlineThickness, useFontSmoothing, isItalic, isBold));

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
            FileManager.DeleteDirectory(AbsoluteFontCacheFolder);
        }

        public void CreateAllMissingFontFiles(GumProjectSave project)
        {
            var fontRanges = project.FontRanges;
            var spacingHorizontal = project.FontSpacingHorizontal;
            var spacingVertical = project.FontSpacingVertical;

            foreach (var element in project.StandardElements)
            {
                foreach(var state in element.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical);

                    // standard elements don't have instances
                }
            }
            foreach (var component in project.Components)
            {
                foreach (var state in component.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical);

                    foreach(var instance in component.Instances)
                    {
                        TryCreateFontFileFor(instance, state, fontRanges, spacingHorizontal, spacingVertical);
                    }
                }
            }
            foreach(var screen in project.Screens)
            {
                foreach (var state in screen.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical);

                    foreach (var instance in screen.Instances)
                    {
                        TryCreateFontFileFor(instance, state, fontRanges, spacingHorizontal, spacingVertical);
                    }
                }
            }
        }

        internal void ReactToFontValueSet(InstanceSave instance, GumProjectSave gumProject, StateSave stateSave, StateSave forcedValues)
        {
            if (stateSave == null)
            {
                throw new InvalidOperationException($"{nameof(stateSave)} is null");
            }

            TryCreateFontFileFor(instance, stateSave, gumProject.FontRanges, gumProject.FontSpacingHorizontal, gumProject.FontSpacingVertical, forcedValues);
        }

        public void TryCreateFontFileFor(InstanceSave instance, StateSave stateSave, string fontRanges, int spacingHorizontal, int spacingVertical, StateSave forcedValues = null)
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

            if (fontValue != null && fontSize != null)
            {
                BmfcSave.CreateBitmapFontFilesIfNecessary(
                    fontSize.Value,
                    fontValue,
                    outlineValue,
                    fontSmoothing, 
                    isItalic, 
                    isBold,
                    fontRanges,
                    spacingHorizontal,
                    spacingVertical
                    );
            }
        }
    }
}
