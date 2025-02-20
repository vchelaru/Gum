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

        public void CreateAllMissingFontFiles(GumProjectSave project, bool forceRecreate = false)
        {
            var fontRanges = project.FontRanges;
            var spacingHorizontal = project.FontSpacingHorizontal;
            var spacingVertical = project.FontSpacingVertical;

            foreach (var element in project.StandardElements)
            {
                foreach(var state in element.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forceRecreate: forceRecreate);

                    // standard elements don't have instances
                }
            }
            foreach (var component in project.Components)
            {
                foreach (var state in component.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forceRecreate: forceRecreate);

                    foreach(var instance in component.Instances)
                    {
                        TryCreateFontFileFor(instance, state, fontRanges, spacingHorizontal, spacingVertical, forceRecreate: forceRecreate);
                    }
                }
            }
            foreach(var screen in project.Screens)
            {
                foreach (var state in screen.AllStates)
                {
                    TryCreateFontFileFor(null, state, fontRanges, spacingHorizontal, spacingVertical, forceRecreate: forceRecreate);

                    foreach (var instance in screen.Instances)
                    {
                        TryCreateFontFileFor(instance, state, fontRanges, spacingHorizontal, spacingVertical, forceRecreate: forceRecreate);
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

        public void TryCreateFontFileFor(InstanceSave instance, StateSave stateSave, string fontRanges, int spacingHorizontal, int spacingVertical, StateSave forcedValues = null, bool forceRecreate = false)
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
                //BmfcSave.CreateBitmapFontFilesIfNecessary(
                //    fontSize.Value,
                //    fontValue,
                //    outlineValue,
                //    fontSmoothing, 
                //    isItalic, 
                //    isBold,
                //    fontRanges,
                //    spacingHorizontal,
                //    spacingVertical
                //    );

                BmfcSave bmfcSave = new BmfcSave();
                bmfcSave.FontSize = fontSize.Value;
                bmfcSave.FontName = fontValue;
                bmfcSave.OutlineThickness = outlineValue;
                bmfcSave.UseSmoothing = fontSmoothing;
                bmfcSave.IsItalic = isItalic;
                bmfcSave.IsBold = isBold;
                bmfcSave.Ranges = fontRanges;
                bmfcSave.SpacingHorizontal = spacingHorizontal;
                bmfcSave.SpacingVertical = spacingVertical;

                int numberWide, numberTall;
                EstimateBlocksNeeded(out numberWide, out numberTall, fontSize.Value);

                bmfcSave.OutputWidth = numberWide * 256;
                bmfcSave.OutputHeight = numberTall * 256;

                bmfcSave.CreateBitmapFontFilesIfNecessary(bmfcSave.FontCacheFileName, force: false);
            }
        }

        private static void EstimateBlocksNeeded(out int numberWide, out int numberTall, int fontSize)
        {
            // todo - eventually this should look at the output and adjust in response. For now, we'll just estimate
            // based on the font size
            int numberOf256Blocks = 2;

            if(fontSize < 20)
            {
                numberOf256Blocks = 1;
            }
            else if(fontSize < 35)
            {
                numberOf256Blocks = 2;
            }
            else if(fontSize < 47)
            {
                numberOf256Blocks = 3;
            }
            else if(fontSize < 56)
            {
                numberOf256Blocks = 4;
            }
            else if (fontSize < 63)
            {
                numberOf256Blocks = 5;
            }
            else if (fontSize < 72)
            {
                numberOf256Blocks = 6;
            }
            else if (fontSize < 82)
            {
                numberOf256Blocks = 8;
            }
            else if (fontSize < 95)
            {
                numberOf256Blocks = 10;
            }
            else if (fontSize < 103)
            {
                numberOf256Blocks = 12;
            }
            else if (fontSize < 113)
            {
                numberOf256Blocks = 14;
            }
            else if (fontSize < 120)
            {
                numberOf256Blocks = 16;
            }
            else if(fontSize < 131)
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
}
