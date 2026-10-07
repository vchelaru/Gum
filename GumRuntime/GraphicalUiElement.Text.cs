using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

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

    // Performs deferred font loads for any element in the tree where isFontDirty was set.
    // Called from two places:
    //   1. WireframeObjectManager, immediately after IsAllLayoutSuspended is set back to false.
    //      At that point mIsLayoutSuspended is always false on every element (because ApplyState
    //      skips SuspendLayout when IsAllLayoutSuspended is true), so the guard below always passes.
    //   2. ResumeLayoutUpdateIfDirtyRecursive, which clears mIsLayoutSuspended on each element
    //      before calling this, so the guard passes for that element but not yet for its children
    //      (they are handled when recursion reaches them).
    public void UpdateFontRecursive()
    {
        if (this.mContainedObjectAsIpso is IText asIText && isFontDirty)
        {
            // Don't load the font if this element is still individually suspended; it will
            // be loaded when ResumeLayoutUpdateIfDirtyRecursive clears the flag for this node.
            if (!this.IsLayoutSuspended)
            {
                // Cleared first so the layout the load may run doesn't load the font again.
                isFontDirty = false;
                LoadFontFromProperties(asIText);
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

    // Called by font-related property setters (Font, FontSize, IsBold, IsItalic, etc.) and by
    // the string-based SetProperty path. When layout is suspended we defer the disk read by
    // setting isFontDirty; the actual load happens later via UpdateFontRecursive() or UpdateLayout.
    public void UpdateToFontValues()
    {
        if (IsAllLayoutSuspended || IsLayoutSuspended)
        {
            if (mContainedObjectAsIpso is IText)
            {
                isFontDirty = true;
            }
            return;
        }
        if(this.mContainedObjectAsIpso is IText asText)
        {
            LoadFontFromProperties(asText);
        }
    }

    // Every backend's font loader runs through here, so none of them lays out after a font change:
    // this does it once, when the text's measured size or descender changed. Skipped during the
    // deferred-font flush inside UpdateLayout, which already sizes this element.
    void LoadFontFromProperties(IText text)
    {
        RenderableMeasurement before = MeasureRenderable();
        UpdateFontFromProperties?.Invoke(text, this);
        if (!SuppressLayoutFromFontChange)
        {
            UpdateLayoutIfRenderableMeasurementChanged(before);
        }
    }

    #endregion
}
