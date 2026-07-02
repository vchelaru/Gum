#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.RenderingLibrary;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilitiesStandard.Helpers;
using System.Net;
using System.IO;
using Gum.Localization;
using System.Security.Policy;
using Gum.Managers;
using Gum.Converters;
using Gum.Wireframe;

#if !FRB && !RAYLIB
using MonoGameGum.GueDeriving;
#endif

#if GUM
using Gum.Services;
using Gum.ToolStates;
#endif



#if RAYLIB
using Gum.Renderables;
using Gum.GueDeriving;
namespace RaylibGum.Renderables;
#else
using Gum.Graphics.Animation;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
namespace Gum.Wireframe;
#endif

public class CustomSetPropertyOnRenderable
{
    private static ILocalizationService _localizationService;

    /// <summary>
    /// The active localization service used by the runtime. Assigning a new
    /// instance fires <see cref="LocalizationServiceChanged"/> so consumers
    /// (e.g. <c>GumService</c>) can re-wire <see cref="ILocalizationService.CurrentLanguageChanged"/>
    /// subscriptions for runtime language switching.
    /// </summary>
    public static ILocalizationService LocalizationService
    {
        get => _localizationService;
        set
        {
            if (ReferenceEquals(_localizationService, value))
            {
                return;
            }
            ILocalizationService previous = _localizationService;
            _localizationService = value;
            LocalizationServiceChanged?.Invoke(previous, value);
        }
    }

    /// <summary>
    /// Raised when <see cref="LocalizationService"/> is replaced. Arguments are
    /// (previousService, newService) — either may be null.
    /// </summary>
    public static event Action<ILocalizationService, ILocalizationService> LocalizationServiceChanged;

    private static readonly ConditionalWeakTable<GraphicalUiElement, string> _localizationKeys = new();

    /// <summary>
    /// Returns the original (pre-translation) string assigned via the localization
    /// path on the given element, or null if no localizable text has been assigned.
    /// </summary>
    public static string TryGetLocalizationKey(GraphicalUiElement element)
    {
        return _localizationKeys.TryGetValue(element, out string key) ? key : null;
    }

#if !FRB
    /// <summary>
    /// Optional font service used for on-demand font creation. In the Gum tool this is
    /// assigned at startup; game runtimes can assign their own implementation.
    /// </summary>
    public static IRuntimeFontService? FontService { get; set; }
#endif

#if !RAYLIB
    /// <summary>
    /// Optional in-memory font creator. When set, font generation bypasses disk entirely —
    /// the creator produces a <see cref="BitmapFont"/> directly from raw pixel data and
    /// .fnt metadata. If null or if creation fails, falls back to the disk-based
    /// <see cref="FontService"/> path.
    /// </summary>
    public static IInMemoryFontCreator? InMemoryFontCreator { get; set; }
#endif

#if RAYLIB
    /// <summary>
    /// Optional in-memory font creator. When set, font generation bypasses disk entirely — the
    /// creator produces a <see cref="Raylib_cs.Font"/> directly from a <see cref="BmfcSave"/>
    /// descriptor (for example, by rasterizing an atlas with KernSmith). If null or if creation
    /// returns null, falls back to the existing disk / system-font path. Raylib parallel to the
    /// <c>#if !RAYLIB</c> <see cref="IInMemoryFontCreator"/> property above.
    /// </summary>
    public static IRaylibFontCreator? InMemoryFontCreator { get; set; }
#endif

    public static event Action<string>? PropertyAssignmentError;


    /// <summary>
    /// Additional logic to perform before falling back to reflection. 
    /// This can be added by libraries adding additional runtime types
    /// </summary>
    public static Func<IRenderableIpso, GraphicalUiElement, string, object, bool>? AdditionalPropertyOnRenderable = null;

    public static void SetPropertyOnRenderable(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        // First try special-casing.  

        if (renderableIpso is Text renderableText)
        {
            handled = TrySetPropertyOnText(renderableText, graphicalUiElement, propertyName, value);
        }

        else if (renderableIpso is Sprite renderableSprite)
        {
            handled = TrySetPropertyOnSprite(renderableSprite, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is NineSlice)
        {
            handled = TrySetPropertyOnNineSlice(renderableIpso, graphicalUiElement, propertyName, value, handled);
        }

        if (!handled)
        {
            GraphicalUiElement.SetPropertyThroughReflection(renderableIpso, graphicalUiElement, propertyName, value);
            //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
        }
    }


    private static bool TrySetPropertyOnNineSlice(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value, bool handled)
    {
        var nineSlice = renderableIpso as NineSlice;

        if (propertyName == "SourceFile")
        {
            AssignSourceFileOnNineSlice(value as string, graphicalUiElement, nineSlice);
            handled = true;
        }
        //else if (propertyName == "Blend")
        //{
        //    var valueAsGumBlend = (RenderingLibrary.Blend)value;

        //    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

        //    nineSlice.BlendState = valueAsXnaBlend;

        //    handled = true;
        //}
        //else if (propertyName == nameof(NineSlice.CustomFrameTextureCoordinateWidth))
        //{
        //    var asFloat = value as float?;

        //    nineSlice.CustomFrameTextureCoordinateWidth = asFloat;

        //    handled = true;
        //}
        else if (propertyName == "Color")
        {
            // todo - need to convert
            //if (value is System.Drawing.Color drawingColor)
            //{
            //    nineSlice.Color = drawingColor;
            //}
            //else if (value is Microsoft.Xna.Framework.Color xnaColor)
            //{
            //    nineSlice.Color = xnaColor.ToSystemDrawing();

            //}
            //handled = true;
        }
        else if (propertyName == "Red")
        {
            nineSlice.Red = (int)value;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            nineSlice.Green = (int)value;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            nineSlice.Blue = (int)value;
            handled = true;
        }
        else if (propertyName == "Texture")
        {
            nineSlice.SetSingleTexture((Texture2D)value);
            handled = true;
        }

        // Texture coordiantes like TextureLeft, TextureRight, TextureWidth, and TextureHeight
        // are handled by GraphicalUiElement so we don't have to handle it here

        return handled;
    }

    private static void AssignSourceFileOnNineSlice(string value, GraphicalUiElement graphicalUiElement, NineSlice nineSlice)
    {
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

        if (string.IsNullOrEmpty(value))
        {
            nineSlice.SetSingleTexture(null);
        }
        // not yet supported:
        //else if (value.EndsWith(".achx"))
        //{
        //    AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

        //    nineSlice.AnimationChains = animationChainList;

        //    nineSlice.RefreshCurrentChainToDesiredName();

        //    nineSlice.UpdateToCurrentAnimationFrame();

        //    graphicalUiElement.UpdateTextureValuesFrom(nineSlice);

        //}
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value))
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;
                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            //check if part of atlas
            //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
            //var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(value);
            //if (atlasedTexture != null)
            //{
            //    nineSlice.LoadAtlasedTexture(value, atlasedTexture);
            //}
            //else
            {
                //if (NineSliceExtensions.GetIfShouldUsePattern(value))
                //{
                //    nineSlice.SetTexturesUsingPattern(value, SystemManagers.Default, false);
                //}
                //else
                {

                    //Texture2D? texture = Sprite.InvalidTexture;
                    Texture2D? texture = null;

                    try
                    {
                        texture =
                            loaderManager.LoadContent<Texture2D>(value);
                    }
                    catch (Exception e)
                    {
                        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on NineSlice named {nineSlice.Name}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        // do nothing?
                    }
                    nineSlice.SetSingleTexture(texture);

                }
            }
        }
    }

    private static bool TrySetPropertyOnSprite(Sprite sprite, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        SpriteRuntime? asSpriteRuntime = graphicalUiElement as SpriteRuntime;

        switch (propertyName)
        {

            case "SourceFile":
                var asString = value as String;
                handled = AssignSourceFileOnSprite(sprite, graphicalUiElement, asString);
                break;
            case nameof(Sprite.Alpha):
                {
                    int valueAsInt = (int)value;
                    sprite.Alpha = valueAsInt;
                    handled = true;
                    break;
                }
            case nameof(Sprite.Red):
                {
                    int valueAsInt = (int)value;
                    sprite.Red = valueAsInt;
                    handled = true;
                    break;

                }
            case nameof(Sprite.Green):
                {
                    int valueAsInt = (int)value;
                    sprite.Green = valueAsInt;
                    handled = true;
                    break;
                }
            case nameof(Sprite.Blue):
                {
                    int valueAsInt = (int)value;
                    sprite.Blue = valueAsInt;
                    handled = true;
                    break;
                }
            //else if (propertyName == nameof(Sprite.Color))
            //{
            //    if (value is System.Drawing.Color drawingColor)
            //    {
            //        sprite.Color = drawingColor;
            //    }
            //    else if (value is Microsoft.Xna.Framework.Color xnaColor)
            //    {
            //        sprite.Color = xnaColor.ToSystemDrawing();

            //    }
            //    handled = true;
            //}

            //else if (propertyName == "Blend")
            //{
            //    var valueAsGumBlend = (RenderingLibrary.Blend)value;

            //    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            //    sprite.BlendState = valueAsXnaBlend;

            //    handled = true;
            //}
            //else if (propertyName == nameof(Sprite.Animate))
            //{
            //    sprite.Animate = (bool)value;
            //    handled = true;
            //}
            //else if (propertyName == nameof(Sprite.CurrentChainName))
            //{
            //    sprite.CurrentChainName = (string)value;
            //    graphicalUiElement.UpdateTextureValuesFrom(sprite);
            //    graphicalUiElement.UpdateLayout();
            //    handled = true;
            //}
            case nameof(Sprite.Texture):
                {
                    sprite.Texture = (Texture2D)value;
                    handled = true;
                    break;
                }
        }


        return handled;
    }

    #region Text

    private static bool TrySetPropertyOnText(Text textRenderable, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as Gum.GueDeriving.TextRuntime;
#endif

        void ReactToFontValueChange()
        {
            UpdateToFontValues(textRenderable, graphicalUiElement);

            handled = true;
        }

        if (propertyName == "Text" || propertyName == "TextNoTranslate")
        {
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:
                textRenderable.Width = null;
            }

            var valueAsString = value as string;

            // Track the original (untranslated) value so RefreshLocalization can
            // re-translate this element if CurrentLanguage changes later.
            if (propertyName == "TextNoTranslate")
            {
                _localizationKeys.Remove(graphicalUiElement);
            }
            else if (LocalizationService != null && valueAsString != null)
            {
                _localizationKeys.AddOrUpdate(graphicalUiElement, valueAsString);
            }
            else
            {
                _localizationKeys.Remove(graphicalUiElement);
            }

            var rawText = valueAsString;


            if (LocalizationService != null && propertyName == "Text")
            {
                rawText = LocalizationService.Translate(rawText);
            }

            textRenderable.RawText = rawText;
            // todo - markup

            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                graphicalUiElement.UpdateLayout();
            }
            handled = true;
        }
        else if (propertyName == "Font Scale" || propertyName == "FontScale")
        {
            textRenderable.FontScale = (float)value;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                graphicalUiElement.UpdateLayout();
            }
            handled = true;

        }
        else if (propertyName == "Font")
        {
            if (value is Font font)
            {
                textRenderable.Font = font;
                handled = true;
            }
            else if (value is string fontString)
            {
                if (textRuntime != null)
                {
                    textRuntime.Font = fontString;
                }

                ReactToFontValueChange();
                handled = true;
            }
        }

        else if (propertyName == nameof(textRuntime.UseCustomFont))
        {
            if (textRuntime != null)
            {
                textRuntime.UseCustomFont = (bool)value;
            }
            //var asText = ((Text)mContainedObjectAsIpso);
            //if (!string.IsNullOrEmpty(asText.StoredMarkupText))
            //{
            //    SetBbCodeText(asText, graphicalUiElement, asText.StoredMarkupText);
            //}
            ReactToFontValueChange();
        }

        else if (propertyName == nameof(textRuntime.CustomFontFile))
        {
            if (textRuntime != null)
            {
                textRuntime.CustomFontFile = (string)value;
                ReactToFontValueChange();
            }

        }

        else if (propertyName == nameof(textRuntime.FontSize))
        {
            if (textRuntime != null)
            {
                textRuntime.FontSize = (int)value;
                ReactToFontValueChange();
            }
        }
        else if (propertyName == nameof(textRuntime.OutlineThickness))
        {
            if (textRuntime != null)
            {
                textRuntime.OutlineThickness = (int)value;
                ReactToFontValueChange();
            }
        }
        else if (propertyName == nameof(textRuntime.IsItalic))
        {
            if (textRuntime != null)
            {
                textRuntime.IsItalic = (bool)value;
                ReactToFontValueChange();
            }
        }
        else if (propertyName == nameof(textRuntime.IsBold))
        {
            if (textRuntime != null)
            {
                textRuntime.IsBold = (bool)value;
                ReactToFontValueChange();
            }
        }
        else if (propertyName == nameof(textRuntime.LineHeightMultiplier))
        {
            if (textRuntime != null)
            {
                textRuntime.LineHeightMultiplier = (float)value;
            }
        }
        else if (propertyName == nameof(textRuntime.UseFontSmoothing))
        {
            if (textRuntime != null)
            {
                textRuntime.UseFontSmoothing = (bool)value;
                ReactToFontValueChange();
            }
        }
        return handled;
    }

    // For some reason this crashes on web when uploading to itch:
    //public static HashSet<string> Tags { get; private set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    // OrdinalIgnoreCase works fine:
    public static HashSet<string> Tags { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "alpha",
        "red",
        "blue",
        "green",
        "color",
        "font",
        "fontsize",
        "outlinethickness",
        "isitalic",
        "isbold",
        "usefontsmoothing",
        "fontscale",
        "lineheightmultiplier",
        // Added Sept 30, 2025 to handle parsing custom blocks
        "custom"

    };

    public static void UpdateToFontValues(IText text, GraphicalUiElement graphicalUiElement)
    {
        // Font deferred-loading system
        //
        // This method is the set-by-string path for font properties (Font, FontSize, IsBold, etc.)
        // reached via SetProperty -> SetPropertyOnRenderable -> TrySetPropertyOnText.
        // The direct-property-setter path goes through GraphicalUiElement.UpdateToFontValues() instead.
        //
        // KNOWN GAP: the two paths handle suspension differently:
        //   - GUE.UpdateToFontValues() defers for BOTH IsAllLayoutSuspended and IsLayoutSuspended.
        //   - This static method only defers for IsAllLayoutSuspended (see reason below).
        //   This means that when fonts are set by string during ApplyState (which uses instance-level
        //   SuspendLayout, not IsAllLayoutSuspended), those font loads still happen immediately.
        //   Fixing that gap requires resolving the cascading-layout issue described below.
        //
        // January 28, 2025 - why we cannot simply early-return for IsLayoutSuspended:
        // If we defer here, bitmap values are not assigned until layout is later resumed via
        // ResumeLayoutUpdateIfDirtyRecursive. At that point mIsLayoutSuspended is already false,
        // so when UpdateFontRecursive assigns the BitmapFont to a Text with Width or Height
        // RelativeToChildren, it triggers UpdateLayout which cascades up through parents that have
        // already completed their own layout pass. This causes redundant layout calls throughout
        // a list box or any deeply nested stack. Solving this would require suppressing that
        // UpdateLayout call inside UpdateToFontValues when called from UpdateFontRecursive, or
        // performing a single top-down layout pass at the end rather than cascading bottom-up.
        //
        // IsAllLayoutSuspended is safe to defer because:
        //   a) WireframeObjectManager calls RootGue.UpdateFontRecursive() before RootGue.UpdateLayout(),
        //      so any per-element UpdateLayout calls triggered by font assignment are immediately
        //      superseded by the full UpdateLayout pass.
        //   b) mIsLayoutSuspended is always false during IsAllLayoutSuspended (ApplyState skips
        //      SuspendLayout when the global flag is set), so there is no cascading risk.
        if (GraphicalUiElement.IsAllLayoutSuspended)
        {
            graphicalUiElement.IsFontDirty = true;
            return;
        }

        var asText = (Text)text;
        var textRuntime = graphicalUiElement as TextRuntime;

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

        if(textRuntime != null)
        {
            // The font family and size are authoritative on the TextRuntime — both the direct
            // property setters and the string/state path write them there. Sync them onto the
            // renderable so its font-based fallbacks (and rendering) stay correct. This subsumes
            // what the former HandleUpdateFontValues stub did, but also actually loads the font.
            asText.FontFamily = textRuntime.Font;
            // Skip the size sync when unchanged: the setter recomputes line height (a native text
            // measure), and on the double-call string path the second pass would otherwise repeat it.
            if (asText.FontSize != textRuntime.FontSize)
            {
                asText.FontSize = textRuntime.FontSize;
            }

            if (textRuntime.UseCustomFont == true)
            {
                // todo here:
                string fontName = textRuntime.CustomFontFile;

                string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                var fontFromGum = loaderManager.LoadContent<Raylib_cs.Font>(fullFileName);
                if (fontFromGum.BaseSize == 0)
                {
                    fontFromGum = loaderManager.LoadContent<Raylib_cs.Font>(asText.FontFamily);
                }
                AssignFontIfChanged(asText, fontFromGum);
            }
            else
            {
                if (textRuntime.FontSize > 0 && !string.IsNullOrEmpty(textRuntime.Font))
                {
                    string fontName = textRuntime.GetFontCacheFileName(
                        BmfcSave.IsFontFilePath(textRuntime.Font) ? textRuntime.Font : null);

                    string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                    // Cache hit: reuse the already-generated/loaded font rather than regenerating.
                    // A Raylib_cs.Font wraps an unmanaged GPU texture, so regenerating on every font
                    // property change would leak VRAM (the previous texture is never reclaimed). This
                    // is the LoaderManager cache the MonoGame path also uses.
                    //
                    // Only short-circuit on a USABLE cached font (BaseSize != 0). When no FontCache .fnt
                    // exists on disk and no stream hook supplies it, the loader caches an empty
                    // default(Font) (BaseSize 0) under this key (ContentLoader.LoadFont). Without this
                    // guard the early-return would hand that empty font to every text after the first
                    // instead of falling through to the disk / system-font fallback below — so text lost
                    // its font and, being RelativeToChildren, collapsed to zero size. Mirrors the MonoGame
                    // path, which assigns the resolved font once at the end rather than early-returning on
                    // any cache entry.
                    if (loaderManager.GetDisposable(fullFileName) is ManagedFont cachedManagedFont
                        && cachedManagedFont.Font.BaseSize != 0)
                    {
                        AssignFontIfChanged(asText, cachedManagedFont.Font);
                        return;
                    }

                    // In-memory font creation (no disk I/O). The BmfcSave construction below is kept
                    // byte-identical to the MonoGame path in Gum/Wireframe/CustomSetPropertyOnRenderable.cs;
                    // only the created-font type (Raylib_cs.Font vs BitmapFont) and how it is cached are
                    // necessarily platform-gated. Null/failure falls through to the FontCache .fnt path.
                    if (InMemoryFontCreator != null)
                    {
                        try
                        {
                            BmfcSave bmfcSave = new BmfcSave();
                            textRuntime.CopyFontGenerationFieldsTo(bmfcSave, resolvedFontFilePath: null);

                            var gumProject = ObjectFinder.Self.GumProjectSave;
                            bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                            bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                            bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;

                            Raylib_cs.Font? createdFont = InMemoryFontCreator.TryCreateFont(bmfcSave);
                            if (createdFont.HasValue && createdFont.Value.BaseSize != 0)
                            {
                                // Heal a poisoned cache slot: the disk-fallback path below can legitimately
                                // cache an empty (BaseSize 0) placeholder under this exact key when no
                                // FontCache .fnt exists on disk (see the cache-hit guard above) -- e.g. when
                                // a font is requested before InMemoryFontCreator gets wired up. AddDisposable's
                                // default ExistingContentBehavior.ThrowException would throw on that occupied
                                // slot; left unguarded, the caller's catch swallows it and the newly created
                                // (working) font -- and its GPU texture -- is discarded, forever re-triggering
                                // this (expensive) rasterization on every subsequent request. Dispose the old
                                // entry first (a no-op if nothing is cached) so the slot is free.
                                loaderManager.Dispose(fullFileName);
                                loaderManager.AddDisposable(fullFileName, new ManagedFont(createdFont.Value));
                                AssignFontIfChanged(asText, createdFont.Value);
                                return;
                            }
                        }
                        catch
                        {
                            // Fall through to the disk / system-font path.
                        }
                    }

                    var fontFromGum = loaderManager.LoadContent<Raylib_cs.Font>(fullFileName);
                    if (fontFromGum.BaseSize == 0)
                    {
                        fontFromGum = loaderManager.LoadContent<Raylib_cs.Font>(asText.FontFamily);
                    }
                    AssignFontIfChanged(asText, fontFromGum);
                }
            }
        }

    }

    // Mirrors the MonoGame loader's `if (BitmapFont != fontToSet)` guard. A single SetProperty can
    // reach this loader twice — once via the TextRuntime setter (UpdateFontFromProperties) and once
    // via the explicit ReactToFontValueChange call — and with font caching on the second load is a
    // cache hit, so skipping the redundant reassignment keeps it to one font assignment.
    // Raylib_cs.Font is a struct, so font identity is compared via the loaded atlas texture id.
    private static void AssignFontIfChanged(Text asText, Raylib_cs.Font font)
    {
        if (asText.Font.Texture.Id != font.Texture.Id)
        {
            asText.Font = font;
        }
    }

    #endregion

    public static bool AssignSourceFileOnSprite(Sprite sprite, GraphicalUiElement graphicalUiElement, string value)
    {
        bool handled;

        var loaderManager =
            global::RenderingLibrary.Content.LoaderManager.Self;

        if (string.IsNullOrEmpty(value))
        {
            sprite.Texture = null;

            graphicalUiElement.UpdateLayout();
        }
        //else if (value.EndsWith(".achx"))
        //{
        //    AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

        //    sprite.AnimationChains = animationChainList;

        //    sprite.RefreshCurrentChainToDesiredName();

        //    sprite.UpdateToCurrentAnimationFrame();

        //    graphicalUiElement.UpdateTextureValuesFrom(sprite);
        //    handled = true;
        //}
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;

                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            // see if an atlas exists:
            //var atlasedTexture = loaderManager.TryLoadContent<AtlasedTexture>(value);

            //if (atlasedTexture != null)
            //{
            //    graphicalUiElement.UpdateLayout();
            //}
            //else
            {
                // We used to check if the file exists. But internally something may
                // alias a file. Ultimately the content loader should make that decision,
                // not the GUE
                try
                {
                    sprite.Texture = loaderManager.LoadContent<Texture2D>(value);
                }
                catch (Exception ex)
                // Jan 1, 2025 - we used to only catch certain types of exceptions, but this list keeps growing as there
                // are a variety of types of crashes that can occur. NineSlice catches all exceptions, so let's just do that!
                //when (ex is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException or WebException or IOException)
                {
                    if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                    {
                        string message = $"Error setting SourceFile on Sprite";

                        if (graphicalUiElement.Tag != null)
                        {
                            message += $" in {graphicalUiElement.Tag}";
                        }
                        message += $"\n{value}";
                        message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
                        message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
                        if (ObjectFinder.Self.GumProjectSave == null)
                        {
                            message += "\nNo Gum project has been loaded";
                        }

                        throw new System.IO.FileNotFoundException(message, ex);
                    }
                    sprite.Texture = null;
                }
                graphicalUiElement.UpdateLayout();
            }
        }
        handled = true;
        return handled;
    }


    public static void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers, Layer layer)
    {
        var managers = iSystemManagers as SystemManagers;

        //if (renderable is Sprite)
        //{
        //    managers.SpriteManager.Add(renderable as Sprite, layer);
        //}
        //else if (renderable is NineSlice)
        //{
        //    managers.SpriteManager.Add(renderable as NineSlice, layer);
        //}
        //else if (renderable is LineRectangle)
        //{
        //    managers.ShapeManager.Add(renderable as LineRectangle, layer);
        //}
        //else if (renderable is SolidRectangle)
        //{
        //    managers.ShapeManager.Add(renderable as SolidRectangle, layer);
        //}
        //else if (renderable is Text)
        //{
        //    managers.TextManager.Add(renderable as Text, layer);
        //}
        //else if (renderable is LineCircle)
        //{
        //    managers.ShapeManager.Add(renderable as LineCircle, layer);
        //}
        //else if (renderable is LinePolygon)
        //{
        //    managers.ShapeManager.Add(renderable as LinePolygon, layer);
        //}
        //else if (renderable is InvisibleRenderable)
        //{
        //    managers.SpriteManager.Add(renderable as InvisibleRenderable, layer);
        //}
        //else
        {
            if (layer == null)
            {
                managers.Renderer.Layers[0].Add(renderable);
            }
            else
            {
                layer.Add(renderable);
            }
        }
    }

    /// <summary>
    /// Detaches <paramref name="renderable"/> from the renderer so it stops drawing. This is the
    /// remove counterpart to <see cref="AddRenderableToManagers"/> and is wired into
    /// <see cref="GraphicalUiElement.RemoveRenderableFromManagers"/> by <c>SystemManagers.Initialize</c>
    /// (issue #3048). Because the Raylib add path is purely layer-based (no Sprite/Shape/TextManager),
    /// removal just searches the renderer's layers for the renderable and removes it.
    /// </summary>
    public static void RemoveRenderableFromManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers)
    {
        if (renderable == null)
        {
            return;
        }

        var managers = (SystemManagers)iSystemManagers;

        foreach (var layer in managers.Renderer.Layers)
        {
            if (layer.Renderables.Contains(renderable))
            {
                layer.Remove(renderable);
                return;
            }
        }
    }
}
