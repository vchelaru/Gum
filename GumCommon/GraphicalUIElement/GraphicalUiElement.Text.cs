using Gum.DataTypes;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Font/Text


        public void RefreshTextOverflowVerticalMode()
        {
            var asIText = mContainedObjectAsIpso as IText;
            if (asIText == null) return;

            // we want to let it spill over if it is sized by its children:
            if (this.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                asIText.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            }
            else
            {
                asIText.TextOverflowVerticalMode = TextOverflowVerticalMode;
            }
        }

#if FRB
    //FRB doesn't yet have a native TextRuntime - it uses codegen to create a TextRuntime.
    // Until it switches over, these properties must be here:

    bool useCustomFont;
    /// <summary>
    /// Whether to use the CustomFontFile to determine the font value. 
    /// If false, then the font is determiend by looking for an existing
    /// font based on:
    /// * Font
    /// * FontSize
    /// * IsItalic
    /// * IsBold
    /// * UseFontSmoothing
    /// * OutlineThickness
    /// </summary>
    public bool UseCustomFont
    {
        get { return useCustomFont; }
        set { useCustomFont = value; UpdateToFontValues(); }
    }

    string customFontFile;
    /// <summary>
    /// Specifies the name of the custom font. This can be specified relative to
    /// FileManager.RelativeDirectory, which is the Content folder for code-only projects,
    /// or the folder containing the .gumx project if loading a Gum project. This should
    /// include the .fnt extension.
    /// </summary>
    public string CustomFontFile
    {
        get { return customFontFile; }
        set { customFontFile = value; UpdateToFontValues(); }
    }

    string font;
    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from 
    /// </summary>
    public string Font
    {
        get { return font; }
        set { font = value; UpdateToFontValues(); }
    }

    int fontSize;
    public int FontSize
    {
        get { return fontSize; }
        set { fontSize = value; UpdateToFontValues(); }
    }

    bool isItalic;
    public bool IsItalic
    {
        get => isItalic;
        set { isItalic = value; UpdateToFontValues(); }
    }

    bool isBold;
    public bool IsBold
    {
        get => isBold;
        set { isBold = value; UpdateToFontValues(); }
    }

    // Not sure if we need to make this a public value, but we do need to store it
    // Update - yes we do need this to be public so it can be assigned in codegen:
    bool useFontSmoothing = true;
    public bool UseFontSmoothing
    {
        get { return useFontSmoothing; }
        set { useFontSmoothing = value; UpdateToFontValues(); }
    }

    int outlineThickness;
    public int OutlineThickness
    {
        get { return outlineThickness; }
        set { outlineThickness = value; UpdateToFontValues(); }
    }

#endif

        public void UpdateFontRecursive()
        {
            if (this.mContainedObjectAsIpso is IText asIText && isFontDirty)
            {

                if (!this.IsLayoutSuspended)
                {
                    UpdateFontFromProperties?.Invoke(asIText, this);
                    isFontDirty = false;
                }
            }

            if (this.Children != null)
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    this.Children[i].UpdateFontRecursive();
                }
            }
            else
            {
                for (int i = 0; i < this.mWhatThisContains.Count; i++)
                {
                    mWhatThisContains[i].UpdateFontRecursive();
                }
            }
        }

        public void UpdateToFontValues()
        {
            if (this.mContainedObjectAsIpso is IText asText)
            {
                UpdateFontFromProperties?.Invoke(asText, this);
            }
        }

        #endregion
    }
}
