using Gum.DataTypes;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Font/Text


        public void RefreshTextOverflowVerticalMode()
        {
            if (mContainedObjectAsIpso is IText asIText)
            {
                asIText.TextOverflowVerticalMode = (HeightUnits == DimensionUnitType.RelativeToChildren)
                    ? TextOverflowVerticalMode.SpillOver
                    : TextOverflowVerticalMode;
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
            // 1. Handle current node
            if (isFontDirty && !IsLayoutSuspended)
            {
                UpdateToFontValues();
                isFontDirty = false;
            }

            // 2. Determine the best collection to iterate
            // If Children exists, we recurse. If not, we use the flat list but do NOT recurse.
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.UpdateFontRecursive();
                }
            }
            else if (mWhatThisContains != null)
            {
                foreach (var item in mWhatThisContains)
                {
                    // We call UpdateToFontValues directly because mWhatThisContains 
                    // is already flat; recursing on it would be redundant.
                    item.UpdateToFontValues();
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
