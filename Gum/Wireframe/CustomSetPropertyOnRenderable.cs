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
using Gum.GueDeriving;
#endif




#if RAYLIB
using Gum.Renderables;
using Gum.GueDeriving;
using Raylib_cs;
// The font a BBCode font-run resolves to. On Raylib that is a Raylib_cs.Font; on the XNA-family
// backends it is a BitmapFont. Mirrored #if so the shared BBCode stack-resolution loop below stays
// type-neutral (see ApplyFontVariables) - only the font-CREATION body is platform-gated.
using ResolvedFont = Raylib_cs.Font;
namespace RaylibGum.Renderables;
#else
using Gum.Graphics.Animation;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using ToolsUtilities;
using ResolvedFont = RenderingLibrary.Graphics.BitmapFont;
namespace Gum.Wireframe;
#endif

public class CustomSetPropertyOnRenderable
{
    private static ILocalizationService? _localizationService;

    /// <summary>
    /// The active localization service used by the runtime. Assigning a new
    /// instance fires <see cref="LocalizationServiceChanged"/> so consumers
    /// (e.g. <c>GumService</c>) can re-wire <see cref="ILocalizationService.CurrentLanguageChanged"/>
    /// subscriptions for runtime language switching.
    /// </summary>
    public static ILocalizationService? LocalizationService
    {
        get => _localizationService;
        set
        {
            if (ReferenceEquals(_localizationService, value))
            {
                return;
            }
            ILocalizationService? previous = _localizationService;
            _localizationService = value;
            LocalizationServiceChanged?.Invoke(previous, value);
        }
    }

    /// <summary>
    /// Raised when <see cref="LocalizationService"/> is replaced. Arguments are
    /// (previousService, newService) — either may be null.
    /// </summary>
    public static event Action<ILocalizationService?, ILocalizationService?>? LocalizationServiceChanged;

    private static readonly ConditionalWeakTable<GraphicalUiElement, string> _localizationKeys = new();

    /// <summary>
    /// Returns the original (pre-translation) string assigned via the localization
    /// path on the given element, or null if no localizable text has been assigned
    /// (or it was overwritten via <c>SetTextNoTranslate</c>).
    /// </summary>
    public static string? TryGetLocalizationKey(GraphicalUiElement element)
    {
        return _localizationKeys.TryGetValue(element, out string? key) ? key : null;
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

    /// <summary>
    /// Optional resolver that turns a render-target shader file reference (e.g. a <c>.fx</c> path
    /// assigned via <c>ContainerRuntime.SourceShaderFile</c>) into a platform effect object, which
    /// is stored in <see cref="RenderableBase.RenderTargetEffect"/>. Gum core ships no shader
    /// loader; a separate opt-in library (the shader equivalent of Gum.Shapes) or the consumer
    /// registers this — typically capturing its own graphics device in the closure. The returned
    /// object is boxed into the backend-agnostic <see cref="RenderableBase.RenderTargetEffect"/>
    /// slot (on xnalike, a <c>Microsoft.Xna.Framework.Graphics.Effect</c>). If null, assigning a
    /// shader file is a graceful no-op and the container renders unshaded. Return null to signal a
    /// failed load (missing file / compile error); resolution then honors
    /// <see cref="GraphicalUiElement.MissingFileBehavior"/>, mirroring sprite source-file handling.
    /// </summary>
    public static Func<string, object?>? RenderTargetEffectResolver { get; set; }

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
#if !RAYLIB


#if !FRB
        // Issue #2925 — dispatch by RUNTIME type for CircleRuntime / RectangleRuntime before
        // falling through to the renderable-type tree. Both runtimes now own two renderable
        // slots (fill + stroke) and their mContainedObjectAsIpso may be the fill slot, so a
        // renderable-type-only dispatch lands legacy "Color" / "Alpha" / "Red" / etc. on the
        // wrong slot. Routing through the runtime's typed property setters keeps each variable
        // landing where its pre-two-slot equivalent did, independent of which slot is contained.
        else if (graphicalUiElement is Gum.GueDeriving.CircleRuntime circleRuntime)
        {
            handled = TrySetPropertyOnCircleRuntime(circleRuntime, propertyName, value);
        }
        else if (graphicalUiElement is Gum.GueDeriving.RectangleRuntime rectangleRuntime)
        {
            handled = TrySetPropertyOnRectangleRuntime(rectangleRuntime, propertyName, value);
        }
#endif
        else if (renderableIpso is LineCircle)
        {
            handled = TrySetPropertyOnLineCircle(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is LineRectangle)
        {
            handled = TrySetPropertyOnLineRectangle(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is LinePolygon)
        {
            handled = TrySetPropertyOnLinePolygon(renderableIpso, propertyName, value);
        }
        else if (renderableIpso is SolidRectangle)
        {
            handled = TrySetPropertyOnSolidRectangle(renderableIpso, propertyName, value, handled);
        }
#endif

        else if (renderableIpso is Sprite renderableSprite)
        {
            handled = TrySetPropertyOnSprite(renderableSprite, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is NineSlice nineSlice)
        {
            handled = TrySetPropertyOnNineSlice(nineSlice, graphicalUiElement, propertyName, value, handled);
        }
        else if (renderableIpso is InvisibleRenderable invisibleRenderable)
        {
            handled = TrySetPropertyOnContainer(invisibleRenderable, graphicalUiElement, propertyName, value);
        }

        if(!handled && AdditionalPropertyOnRenderable != null)
        {
            handled = AdditionalPropertyOnRenderable(renderableIpso, graphicalUiElement, propertyName, value);
        }

        // If special case didn't work, let's try reflection
        if (!handled)
        {
            if (propertyName == "Parent")
            {
                // do something
            }
            else
            {
                System.Reflection.PropertyInfo propertyInfo = renderableIpso.GetType().GetProperty(propertyName);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var valueType = value.GetType();
                    if (valueType != propertyInfo.PropertyType)
                    {
                        if(valueType == typeof(PositionUnitType) && propertyInfo.PropertyType == typeof(GeneralUnitType))
                        {
                            value = UnitConverter.ConvertToGeneralUnit((PositionUnitType)value);
                        }
                        else
                        {
                            value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                        }
                    }
                    propertyInfo.SetValue(renderableIpso, value, null);
                }
            }
        }
    }

#if !RAYLIB
    private static bool TrySetPropertyOnSolidRectangle(IRenderableIpso renderableIpso, string propertyName, object value, bool handled)
    {
        var solidRect = renderableIpso as SolidRectangle;

        if (propertyName == "Blend")
        {
            var valueAsGumBlend = (Gum.RenderingLibrary.Blend)value;

            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            solidRect.BlendState = valueAsXnaBlend;

            handled = true;
        }
        else if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;
            solidRect.Alpha = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;
            solidRect.Red = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;
            solidRect.Green = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;
            solidRect.Blue = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Color")
        {
            //var valueAsColor = (Color)value;
            if (value is System.Drawing.Color drawingColor)
            {
                solidRect.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                solidRect.Color = xnaColor.ToSystemDrawing();

            }

            handled = true;
        }

        return handled;
    }
#endif

    private static bool TrySetPropertyOnContainer(InvisibleRenderable invisibleRenderable, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName)
        {
            case "IsRenderTarget":
                invisibleRenderable.IsRenderTarget = value as bool? ?? false;
                return true;
            case "SourceShaderFile":
                AssignSourceShaderFileOnContainer(invisibleRenderable, graphicalUiElement, value as string);
                return true;
            case "Alpha":
                if (value is int asInt)
                {
                    invisibleRenderable.Alpha = asInt;
                }
                else if (value is float asFloat)
                {
                    invisibleRenderable.Alpha = (int)asFloat;
                }
                else
                {
                    invisibleRenderable.Alpha = 255;
                }
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a render-target shader file reference (e.g. a <c>.fx</c> path) into a platform
    /// effect via <see cref="RenderTargetEffectResolver"/> and stores it in the container's
    /// <see cref="RenderableBase.RenderTargetEffect"/> slot. Mirrors
    /// <see cref="AssignSourceFileOnSprite"/>: the resolved effect is cached in
    /// <see cref="LoaderManager"/> by normalized path so a .fx referenced by multiple containers
    /// loads once, and a failed resolve honors <see cref="GraphicalUiElement.MissingFileBehavior"/>.
    /// With no resolver registered this is a graceful no-op (the container renders unshaded),
    /// matching how a missing texture degrades. Called internally by the string property path.
    /// </summary>
    public static void AssignSourceShaderFileOnContainer(IRenderTargetRenderable effectOwner, GraphicalUiElement graphicalUiElement, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            effectOwner.RenderTargetEffect = null;
            return;
        }

        // No resolver registered: render unshaded rather than crash. A separate opt-in library
        // (the shader equivalent of Gum.Shapes) or the consumer registers the resolver.
        if (RenderTargetEffectResolver == null)
        {
            return;
        }

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

        if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
        {
            value = ToolsUtilities.FileManager.RelativeDirectory + value;
            value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
        }

        // LoaderManager caches by normalized path (same convention as the texture cache) so a .fx
        // shared by multiple containers compiles/loads once. The resolver only does path -> effect.
        if (loaderManager.CacheTextures)
        {
            var cachedEffect = loaderManager.GetDisposable(value);
            if (cachedEffect != null)
            {
                effectOwner.RenderTargetEffect = cachedEffect;
                return;
            }
        }

        object? resolvedEffect = null;
        Exception? resolveException = null;
        try
        {
            resolvedEffect = RenderTargetEffectResolver(value);
        }
        catch (Exception ex)
        {
            resolveException = ex;
        }

        if (resolvedEffect == null)
        {
            // Resolver registered but couldn't produce an effect (missing .fx or compile error).
            // Mirror Sprite source-file handling: honor MissingFileBehavior, else report the error.
            string message = $"Error setting SourceShaderFile on Container";
            if (graphicalUiElement.Tag != null)
            {
                message += $" in {graphicalUiElement.Tag}";
            }
            message += $"\n{value}";
            message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
            message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
            if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
            {
                if (ObjectFinder.Self.GumProjectSave == null)
                {
                    message += "\nNo Gum project has been loaded";
                }
                throw new System.IO.FileNotFoundException(message, resolveException);
            }
            effectOwner.RenderTargetEffect = null;
            PropertyAssignmentError?.Invoke(resolveException != null ? message + "\n" + resolveException.ToString() : message);
            return;
        }

        if (loaderManager.CacheTextures && resolvedEffect is IDisposable disposableEffect)
        {
            loaderManager.AddDisposable(value, disposableEffect);
        }

        effectOwner.RenderTargetEffect = resolvedEffect;
    }

    private static bool TrySetPropertyOnNineSlice(NineSlice nineSlice, GraphicalUiElement graphicalUiElement, string propertyName, object value, bool handled)
    {

        if (propertyName == "SourceFile")
        {
            AssignSourceFileOnNineSlice(value as string, graphicalUiElement, nineSlice);
            handled = true;
        }
        else if (propertyName == "Blend")
        {
            var valueAsGumBlend = (Gum.RenderingLibrary.Blend)value;

#if !RAYLIB
            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            nineSlice.BlendState = valueAsXnaBlend;
#else
            // Gum.Renderables.NineSlice (Raylib) stores the Gum-level Blend enum directly (see its
            // Blend property) rather than an XNA BlendState object, so no ToBlendState() bridge is
            // needed here, unlike the XNA-family branch above.
            nineSlice.Blend = valueAsGumBlend;
#endif

            handled = true;
        }
        else if (propertyName == nameof(NineSlice.CustomFrameTextureCoordinateWidth))
        {
            var asFloat = value as float?;

            nineSlice.CustomFrameTextureCoordinateWidth = asFloat;

            handled = true;
        }
        else if (propertyName == "Color")
        {
#if !RAYLIB
            if (value is System.Drawing.Color drawingColor)
            {
                nineSlice.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                nineSlice.Color = xnaColor.ToSystemDrawing();

            }
            handled = true;
#else
            // TODO #3629 - no System.Drawing.Color -> Raylib_cs.Color converter exists yet, so this
            // is a tracked no-op. SetPropertyOnRenderable falls back to reflection afterward, which
            // also can't bridge the type mismatch, so a data-driven "Color" value is silently
            // dropped on Raylib until #3629 lands.
#endif
        }
        else if(propertyName == "Red")
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
        else if(propertyName == nameof(NineSlice.BorderScale))
        {
            nineSlice.BorderScale = (float)value;
            handled = true;
        }
        else if(propertyName == nameof(NineSlice.IsTilingMiddleSections))
        {
            nineSlice.IsTilingMiddleSections = (bool)value;
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
        else if (value.EndsWith(".achx"))
        {
            AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

            nineSlice.AnimationChains = animationChainList;

            nineSlice.RefreshCurrentChainToDesiredName();

            nineSlice.UpdateToCurrentAnimationFrame();

            graphicalUiElement.UpdateTextureValuesFrom(nineSlice);

        }
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value))
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;
                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            //check if part of atlas
            //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
#if !RAYLIB
            var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(value);
            if (atlasedTexture != null)
            {
                nineSlice.LoadAtlasedTexture(value, atlasedTexture);
            }
            else
#endif
            {
#if !RAYLIB
                if (NineSliceExtensions.GetIfShouldUsePattern(value))
                {
                    nineSlice.SetTexturesUsingPattern(value, SystemManagers.Default, false);
                }
                else
#endif
                {

#if !RAYLIB
                    Microsoft.Xna.Framework.Graphics.Texture2D? texture =
                        Sprite.InvalidTexture;

                    try
                    {
                        texture =
                            loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(value);
                    }
                    catch (Exception)
                    {
                        // Treated the same as a missing file below.
                        texture = null;
                    }

                    if (texture == null)
                    {
                        // On desktop the loader returns null for a missing file instead of throwing, and a
                        // genuine load error is funneled here too. Honor MissingFileBehavior; otherwise fall
                        // back to the invalid-texture placeholder, matching the prior catch behavior.
                        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on NineSlice named {nineSlice.Name}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        texture = Sprite.InvalidTexture;
                    }
                    nineSlice.SetSingleTexture(texture);
#else
                    // Raylib has neither atlas (AtlasedTexture) nor tiled-pattern
                    // (SetTexturesUsingPattern) NineSlice support, and no InvalidTexture placeholder
                    // (Sprite.InvalidTexture lives on the XNA-only RenderingLibrary.Graphics.Sprite) -
                    // so a load failure here can only honor MissingFileBehavior.ThrowException or
                    // leave the texture unset.
                    Texture2D? texture = null;

                    try
                    {
                        texture =
                            loaderManager.LoadContent<Texture2D>(value);
                    }
                    catch (Exception)
                    {
                        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on NineSlice named {nineSlice.Name}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        // do nothing?
                    }
                    nineSlice.SetSingleTexture(texture);
#endif

                }
            }
        }
    }

    private static bool TrySetPropertyOnSprite(Sprite sprite, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        switch (propertyName)
        {
            case "SourceFile":
                {
                    var asString = value as String;
                    handled = AssignSourceFileOnSprite(sprite, graphicalUiElement, asString);
                    break;
                }
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
#if !RAYLIB
            case nameof(Sprite.Color):
                {
                    if (value is System.Drawing.Color drawingColor)
                    {
                        sprite.Color = drawingColor;
                    }
                    else if (value is Microsoft.Xna.Framework.Color xnaColor)
                    {
                        sprite.Color = xnaColor.ToSystemDrawing();
                    }
                    handled = true;
                    break;
                }
#endif
            case "Blend":
                {
                    var valueAsGumBlend = (Gum.RenderingLibrary.Blend)value;
#if RAYLIB
                    sprite.Blend = valueAsGumBlend;
#else
                    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                    sprite.BlendState = valueAsXnaBlend;
#endif
                    handled = true;
                    break;
                }
            case nameof(Sprite.Animate):
                {
                    sprite.Animate = (bool)value;
                    handled = true;
                    break;
                }
            case nameof(Sprite.CurrentChainName):
                {
                    sprite.CurrentChainName = (string)value;
                    graphicalUiElement.UpdateTextureValuesFrom(sprite);
                    graphicalUiElement.UpdateLayout();
                    handled = true;
                    break;
                }
#if !FRB
            case nameof(SpriteRuntime.RenderTargetTextureSource):
                {
                    var runtime = graphicalUiElement as SpriteRuntime;
                    if (runtime != null)
                    {
                        if (value == null)
                        {
                            runtime.RenderTargetTextureSource = null;
                        }
                        else if (value is IRenderableIpso renderableIpso)
                        {
                            runtime.RenderTargetTextureSource = renderableIpso;
                        }
                        else if (value is string asStringValue)
                        {
                            runtime.RenderTargetTextureSource =
                                (graphicalUiElement.GetTopParent() as GraphicalUiElement)?.FindByName(asStringValue);
                        }
                        handled = true;
                    }
                    break;
                }
#endif
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
            // - "Text" with an active LocalizationService stores the raw input.
            // - "TextNoTranslate" always clears any previously-stored key so user
            //   input or explicitly-untranslated values aren't re-translated later.
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

            textRenderable.InlineVariables.Clear();
            if (valueAsString?.Contains("[") == true)
            {
                textRenderable.StoredMarkupText = valueAsString;
                SetBbCodeText(textRenderable, graphicalUiElement, textRenderable.StoredMarkupText);
            }
            else
            {
                textRenderable.StoredMarkupText = null;
                var rawText = valueAsString;
                if(LocalizationService != null && propertyName == "Text")
                {
                    rawText = LocalizationService.Translate(rawText);
                }

                if(rawText?.Contains("[") == true)
                {
                    textRenderable.StoredMarkupText = rawText;
                    SetBbCodeText(textRenderable, graphicalUiElement, textRenderable.StoredMarkupText);
                }
                else
                {
                    textRenderable.RawText = rawText;
                }
            }
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
#if RAYLIB
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
            }
#else
            if(textRuntime != null)
            {
                textRuntime.Font = value as string;
            }

            ReactToFontValueChange();
#endif
        }
#if XNALIKE || RAYLIB
        else if (propertyName == nameof(textRuntime.UseCustomFont))
        {
            if (textRuntime != null)
            {
                textRuntime.UseCustomFont = (bool)value;
            }

            ReactToFontValueChange();
        }

        else if (propertyName == nameof(textRuntime.CustomFontFile))
        {
            if (textRuntime != null)
            {
                textRuntime.CustomFontFile = (string)value;
            }
            ReactToFontValueChange();

        }
#if USE_GUMCOMMON
        else if(propertyName == nameof(Gum.GueDeriving.TextRuntime.BitmapFont))
        {
            if(textRuntime != null)
            {
                textRuntime.BitmapFont = (BitmapFont)value;
            }
            handled = true;
        }
#endif
#endif
        else if (propertyName == nameof(textRuntime.FontSize))
        {
            if (textRuntime != null)
            {
                textRuntime.FontSize = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.OutlineThickness))
        {
            if (textRuntime != null)
            {
                textRuntime.OutlineThickness = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.IsItalic))
        {
            if (textRuntime != null)
            {
                textRuntime.IsItalic = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.IsBold))
        {
            if (textRuntime != null)
            {
                textRuntime.IsBold = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == "LineHeightMultiplier")
        {
#if RAYLIB
            if (textRuntime != null)
            {
                textRuntime.LineHeightMultiplier = (float)value;
            }
#else
            textRenderable.LineHeightMultiplier = (float)value;
#endif
        }
        else if (propertyName == nameof(textRuntime.UseFontSmoothing))
        {
            if (textRuntime != null)
            {
                textRuntime.UseFontSmoothing = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(Blend))
        {
#if XNALIKE
            var valueAsGumBlend = (RenderingLibrary.Blend)value;

            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            textRenderable.BlendState = valueAsXnaBlend;
            handled = true;
#endif
        }
        else if (propertyName == "Alpha")
        {
#if XNALIKE
            int valueAsInt = (int)value;
            textRenderable.Alpha = valueAsInt;
            handled = true;
#endif
        }
        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;
            textRenderable.Red = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;
            textRenderable.Green = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;
            textRenderable.Blue = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Color")
        {
#if XNALIKE
            if (value is System.Drawing.Color drawingColor)
            {
                textRenderable.Color = drawingColor;
                handled = true;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                textRenderable.Color = xnaColor.ToSystemDrawing();
                handled = true;
            }
#endif
        }

        else if (propertyName == "HorizontalAlignment")
        {
            textRenderable.HorizontalAlignment = (HorizontalAlignment)value;
            handled = true;
        }
        else if (propertyName == "VerticalAlignment")
        {
            textRenderable.VerticalAlignment = (VerticalAlignment)value;
            handled = true;
        }
        else if (propertyName == "MaxLettersToShow")
        {
#if XNALIKE
            textRenderable.MaxLettersToShow = (int?)value;
            handled = true;
#endif
        }
        else if (propertyName == "MaxNumberOfLines")
        {
            textRenderable.MaxNumberOfLines = (int?)value;
            handled = true;
        }

        else if (propertyName == nameof(TextOverflowHorizontalMode))
        {
            var textOverflowMode = (TextOverflowHorizontalMode)value;

            if (textOverflowMode == TextOverflowHorizontalMode.EllipsisLetter)
            {
                textRenderable.IsTruncatingWithEllipsisOnLastLine = true;
            }
            else
            {
                textRenderable.IsTruncatingWithEllipsisOnLastLine = false;
            }
        }
        else if (propertyName == nameof(TextOverflowVerticalMode))
        {
            graphicalUiElement.TextOverflowVerticalMode = (TextOverflowVerticalMode)value;
            graphicalUiElement.RefreshTextOverflowVerticalMode();

        }

        return handled;
    }

    /// <summary>
    /// The canonical set of recognized BbCode tag names. Forwarder to the GumCommon-side
    /// <see cref="BbCodeParser.KnownTags"/> — kept here so existing callers that reference
    /// <c>CustomSetPropertyOnRenderable.Tags</c> continue to compile.
    /// </summary>
    public static HashSet<string> Tags => BbCodeParser.KnownTags;

    static Stack<int> fontSizeStack = new Stack<int>();
    static Stack<string> fontNameStack = new Stack<string>();
    static Stack<int> outlineThicknessStack = new Stack<int>();
    static Stack<bool> useFontSmoothingStack = new Stack<bool>();
    static Stack<bool> isItalicStack = new Stack<bool>();
    static Stack<bool> isBoldStack = new Stack<bool>();
    static Stack<bool> useCustomFontStack = new Stack<bool>();

    static List<TagInfo> allTags = new List<TagInfo>();

    /// <summary>
    /// Parses BBCode markup, assigns the tag-stripped text to <see cref="Text.RawText"/>, and populates
    /// <see cref="Text.InlineVariables"/> with the per-run styling the renderer applies: Color / Red /
    /// Green / Blue and FontScale runs in the loop below, plus the resolved-font runs for the font family
    /// tags (Font / FontSize / OutlineThickness / IsBold / IsItalic) produced by
    /// <see cref="ApplyFontVariables"/> using the same stack model on every platform. A <c>[Custom]</c> tag's
    /// per-letter callback is applied on the XNA-family backends (<c>#if !RAYLIB</c> below); on Raylib the tag
    /// is recognized by the parser but has no effect yet (#3471).
    /// </summary>
    private static void SetBbCodeText(Text asText, GraphicalUiElement graphicalUiElement, string bbcode)
    {
        // The rendering/wrapping code ignores '\r', so normalize CRLF to LF before computing indexes.
        // Parsing and stripping from the same normalized string keeps InlineVariable indexes aligned
        // with the RawText the renderer sees.
        var normalized = bbcode?.Replace("\r\n", "\n");

        var results = BbCodeParser.Parse(normalized, Tags);
        var strippedText = BbCodeParser.RemoveTags(normalized, results);

        asText.RawText = strippedText;

        // Color / Red / Green / Blue / FontScale runs don't use the font-resolution stacks, so they are
        // emitted regardless of whether the owning element is a TextRuntime.
        foreach (var item in results)
        {
            object castedValue = item.Open.Argument;
            var shouldApply = false;
            switch (item.Name)
            {
                case "Red":
                case "Green":
                case "Blue":
                    castedValue = byte.Parse(item.Open.Argument);
                    shouldApply = true;
                    break;
                case "Color":
                    {
                        if (item.Open.Argument?.StartsWith("0x") == true && int.TryParse(item.Open.Argument.Substring(2),
                                                                            NumberStyles.AllowHexSpecifier,
                                                                            null,
                                                                            out int result))
                        {
                            castedValue = System.Drawing.Color.FromArgb(result);
                        }
                        else
                        {
                            castedValue = System.Drawing.Color.FromName(item.Open.Argument);
                        }
                        shouldApply = true;
                    }
                    break;
                case "FontScale":
                    {
                        if (float.TryParse(item.Open.Argument, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                        {
                            castedValue = parsed;
                            shouldApply = true;
                        }
                    }
                    break;
#if !RAYLIB
                case "Custom":
                    if(castedValue is string functionName)
                    {
                        var startStripped = item.Open.StartStrippedIndex;

                        var substring = strippedText.Substring(startStripped, item.Close.StartStrippedIndex - startStripped);


                        // this function needs to be called on every letter:
                        for (int i = 0; i < substring.Length; i++)
                        {
                            var call = new ParameterizedLetterCustomizationCall
                            {
                                FunctionName = functionName,
                                CharacterIndex = startStripped + i,
                                TextBlock = substring
                            };
                            // we probably need to check and add variables only as needed
                            var inlineVariable = new InlineVariable
                            {
                                CharacterCount = 1,
                                StartIndex = startStripped + i,
                                VariableName = "Custom",
                                Value = call
                            };

                            asText.InlineVariables.Add(inlineVariable);
                        }
                    }

                    // we apply the inline ourselves
                    shouldApply = false;
                    break;
#endif
                    // Font / FontSize / OutlineThickness / IsBold / IsItalic family swaps are handled below in
                    // ApplyFontVariables (stack model), not here.
            }

            if (shouldApply)
            {
                asText.InlineVariables.Add(new InlineVariable
                {
                    CharacterCount = item.Close.StartStrippedIndex - item.Open.StartStrippedIndex,
                    StartIndex = item.Open.StartStrippedIndex,
                    VariableName = item.Name,
                    Value = castedValue
                });
            }
        }

#if FRB
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as Gum.GueDeriving.TextRuntime;
#endif

        // Per-run font resolution requires the seven stacks to be seeded from a TextRuntime's base font
        // values. A Text owned by a non-TextRuntime GraphicalUiElement has no such values, so we neither seed
        // nor resolve here - otherwise ApplyFontVariables would peek/pop unseeded (stale or empty) stacks,
        // giving a wrong font or an InvalidOperationException. One guard covers seeding and resolution; the
        // text is still wrapped/measured below regardless. (Under #if FRB, textRuntime == graphicalUiElement,
        // never null, so this guard is always-true and FRB behavior is unchanged - a pure structural move.)
        if (textRuntime != null)
        {
            fontNameStack.Clear();
            if (textRuntime.UseCustomFont)
            {
                var customFont = textRuntime.CustomFontFile;
                if (customFont?.EndsWith(".fnt") == true)
                {
                    customFont = customFont.Substring(0, customFont.Length - ".fnt".Length);
                }
                fontNameStack.Push(customFont);
            }
            else
            {
                fontNameStack.Push(textRuntime.Font);
            }

            fontSizeStack.Clear();
            fontSizeStack.Push(textRuntime.FontSize);

            outlineThicknessStack.Clear();
            outlineThicknessStack.Push(textRuntime.OutlineThickness);

            useFontSmoothingStack.Clear();
            useFontSmoothingStack.Push(textRuntime.UseFontSmoothing);

            isItalicStack.Clear();
            isItalicStack.Push(textRuntime.IsItalic);

            isBoldStack.Clear();
            isBoldStack.Push(textRuntime.IsBold);

            useCustomFontStack.Clear();
            useCustomFontStack.Push(textRuntime.UseCustomFont);

            ApplyFontVariables(asText, results);
        }

        // #3481: RawText was assigned above (which wrapped + measured the text) before any InlineVariables
        // existed, so that first pass was blind to inline [FontScale=N]/[FontSize=N] runs. Re-wrap first so
        // line breaks account for an enlarged run's real size (#3520 MonoGame / #3532 Raylib), then re-measure
        // so the reported size accounts for per-line scale (#3481) — otherwise a tall run overflows its slot
        // and overlaps the next stacked sibling, or a wide run spills past a fixed wrap width. Runs
        // unconditionally (the text must wrap whether or not per-run fonts were resolved).
        asText.UpdateWrappedText();
        asText.UpdatePreRenderDimensions();
    }

    /// <summary>
    /// Resolves the Font / FontSize / OutlineThickness / IsBold / IsItalic BBCode tags into per-run
    /// resolved-font (<c>"BitmapFont"</c>) inline variables using a stack model: each open tag pushes its
    /// value and each close tag pops it, so a run resolves to the font implied by every tag open over it.
    /// The push/pop/sort/character-count loop is identical on every platform; only the font-CREATION body
    /// (<see cref="GetAndCreateFontIfNecessary"/>) is platform-specific, since Raylib produces a
    /// <see cref="Raylib_cs.Font"/> (or, with no creator, falls back to scaling the base atlas) while the
    /// XNA-family backends produce a <see cref="BitmapFont"/>.
    /// </summary>
    private static void ApplyFontVariables(Text asText, List<FoundTag> results)
    {
        allTags.Clear();
        allTags.AddRange(results.Select(item => item.Open));
        allTags.AddRange(results.Select(item => item.Close));
        allTags.Sort((a, b) => a.StartIndex - b.StartIndex);

        InlineVariable lastFontInlineVariable = null;
        // Every BBCode push (open tag) and pop (close tag) below calls
        // GetAndCreateFontIfNecessary. The pop case re-asks for a font that was
        // already resolved on the matching push — caching is intentionally NOT
        // this method's job. LoaderManager handles cache hits on the second
        // lookup, so the cost of asking twice is the cache check, not a full
        // font generation. Do not add caching here; if a generation path is
        // slow on cache hits, fix it in the underlying loader.
        foreach (var tag in allTags)
        {
            // The run's value: a resolved Raylib_cs.Font (crisp), or - for a [FontSize] tag with no font
            // creator wired - the raw pixel size as a float (ResolveRunFont then scales the base atlas).
            object castedValue = null;
            string convertedName = "BitmapFont"; // shared historical run-marker name (see ResolvedFont note).
            switch (tag.Name)
            {
                case "Font":
                    if (!string.IsNullOrEmpty(tag.Argument))
                    {
                        // tolerate ".fnt" suffix
                        var argument = tag.Argument;
                        if (argument?.EndsWith(".fnt") == true)
                        {
                            argument = argument.Substring(0, argument.Length - ".fnt".Length);
                        }
                        fontNameStack.Push(argument);
                    }
                    else
                    {
                        fontNameStack.Pop();
                    }
                    castedValue = GetAndCreateFontIfNecessary();
                    break;
                case "FontSize":
                    if (int.TryParse(tag.Argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSize))
                    {
                        fontSizeStack.Push(parsedSize);
                        castedValue = GetAndCreateFontIfNecessary();
#if RAYLIB
                        // Platform-necessary Raylib fallback: with no font creator wired, no crisp font can be
                        // rasterized at the requested size, so store the raw pixel size as a float and let the
                        // renderer scale the base atlas to it (a blurry-but-correct approximation). The XNA
                        // path never needs this - it can new BitmapFont(...) at any size.
                        castedValue ??= (float)parsedSize;
#endif
                    }
                    else
                    {
                        fontSizeStack.Pop();
                        castedValue = GetAndCreateFontIfNecessary();
                    }
                    break;
                case "OutlineThickness":
                    if (int.TryParse(tag.Argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedOutline))
                    {
                        outlineThicknessStack.Push(parsedOutline);
                    }
                    else
                    {
                        outlineThicknessStack.Pop();
                    }
                    castedValue = GetAndCreateFontIfNecessary();
                    break;
                case "IsItalic":
                    if (bool.TryParse(tag.Argument, out bool parsedItalic))
                    {
                        isItalicStack.Push(parsedItalic);
                    }
                    else
                    {
                        isItalicStack.Pop();
                    }
                    castedValue = GetAndCreateFontIfNecessary();
                    break;
                case "IsBold":
                    if (bool.TryParse(tag.Argument, out bool parsedBold))
                    {
                        isBoldStack.Push(parsedBold);
                    }
                    else
                    {
                        isBoldStack.Pop();
                    }
                    castedValue = GetAndCreateFontIfNecessary();
                    break;
                case "UseCustomFont":
                    // Never fires today (UseCustomFont is not a BbCodeParser.KnownTag), but the stack push/pop
                    // is kept parallel to the MonoGame path for convergence.
                    if (bool.TryParse(tag.Argument, out bool parsedCustom))
                    {
                        useCustomFontStack.Push(parsedCustom);
                    }
                    else
                    {
                        useCustomFontStack.Pop();
                    }
                    castedValue = GetAndCreateFontIfNecessary();
                    break;
            }

            if (castedValue != null)
            {
                if (lastFontInlineVariable != null)
                {
                    lastFontInlineVariable.CharacterCount = tag.StartStrippedIndex - lastFontInlineVariable.StartIndex;
                }

                var inlineVariable = new InlineVariable
                {
                    StartIndex = tag.StartStrippedIndex,
                    VariableName = convertedName,
                    Value = castedValue
                };

                asText.InlineVariables.Add(inlineVariable);

                lastFontInlineVariable = inlineVariable;
            }
        }

        // Close off the last font run so it extends to the end of the text.
        if (lastFontInlineVariable != null)
        {
            lastFontInlineVariable.CharacterCount = asText.RawText.Length - lastFontInlineVariable.StartIndex;
        }

        // Creates (or returns a cached) resolved font for the current stack state. Only the font-CREATION
        // body is platform-specific; everything above is shared on every platform. Returns null when no
        // creator is available or creation fails - the caller then leaves the range at the base font (no
        // run) or, for a [FontSize] tag, falls back to scaling the base atlas (Raylib only). The Raylib body
        // preserves the Dispose-then-AddDisposable cache-heal from the former TryGetOrCreateFontAtSize.
        ResolvedFont? GetAndCreateFontIfNecessary()
        {
#if !RAYLIB
            var fontFileName = GetFontFileName();

            var font = global::RenderingLibrary.Content.LoaderManager.Self.GetDisposable(fontFileName) as BitmapFont;

            if (font == null)
            {
                font = GetFontDisposable(fontFileName);
            }

            // no cache, does it need to be created?
            if (font == null)
            {
                // Try in-memory font creation first (no disk I/O)
                if (InMemoryFontCreator != null)
                {
                    try
                    {
                        string? bbFontFile = BmfcSave.IsFontFilePath(fontNameStack.Peek()) ? fontNameStack.Peek() : null;

                        BmfcSave bmfcSave = new BmfcSave();
                        bmfcSave.FontSize = fontSizeStack.Peek();
                        bmfcSave.OutlineThickness = outlineThicknessStack.Peek();
                        bmfcSave.UseSmoothing = useFontSmoothingStack.Peek();
                        bmfcSave.IsItalic = isItalicStack.Peek();
                        bmfcSave.IsBold = isBoldStack.Peek();

                        if (bbFontFile != null)
                        {
                            bmfcSave.FontFile = ResolveFontFilePath(bbFontFile);
                            bmfcSave.FontName = System.IO.Path.GetFileNameWithoutExtension(bbFontFile);
                        }
                        else
                        {
                            bmfcSave.FontName = fontNameStack.Peek();
                        }

                        var gumProject = ObjectFinder.Self.GumProjectSave;
                        bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                        bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                        bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;

                        font = InMemoryFontCreator.TryCreateFont(bmfcSave);
                        if (font != null)
                        {
                            // #3530: Replace so re-adding an already-occupied key heals it instead of throwing.
                            global::RenderingLibrary.Content.LoaderManager.Self.AddDisposable(fontFileName, font,
                                global::RenderingLibrary.Content.LoaderManager.ExistingContentBehavior.Replace);
                        }
                    }
                    catch
                    {
                        // Fall through to disk-based path
                    }
                }

                // Fall back to disk-based font creation
                if (font == null)
                {
                    // this could be a custom font, so let's see if it exists:

                    string fileName = String.Empty;
                    if (ToolsUtilities.FileManager.FileExists(fontFileName))
                    {
                        fileName = fontFileName;
                    }
#if !FRB
                    else if (FontService != null)
                    {
                        fileName = FontService.AbsoluteFontCacheFolder +
                            ToolsUtilities.FileManager.RemovePath(fontFileName);
                    }

                    if (FontService != null && !ToolsUtilities.FileManager.FileExists(fileName))
                    {
                        // user could have typed anything in there, so who knows if this will succeed. Therefore, try/catch:
                        try
                        {
                            string? bbFontFileForDisk = BmfcSave.IsFontFilePath(fontNameStack.Peek()) ? fontNameStack.Peek() : null;

                            BmfcSave bmfcSave = new BmfcSave();
                            bmfcSave.FontSize = fontSizeStack.Peek();
                            bmfcSave.OutlineThickness = outlineThicknessStack.Peek();
                            bmfcSave.UseSmoothing = useFontSmoothingStack.Peek();
                            bmfcSave.IsItalic = isItalicStack.Peek();
                            bmfcSave.IsBold = isBoldStack.Peek();

                            if (bbFontFileForDisk != null)
                            {
                                bmfcSave.FontFile = ResolveFontFilePath(bbFontFileForDisk);
                                bmfcSave.FontName = System.IO.Path.GetFileNameWithoutExtension(bbFontFileForDisk);
                            }
                            else
                            {
                                bmfcSave.FontName = fontNameStack.Peek();
                            }
#if !FRB
                            // BBCode inline font creation: when BBCode tags like [FontSize=24] reference a font
                            // that doesn't exist, create it on demand. This parallels the font creation in
                            // UpdateToFontValues — both use FontService.CreateFontIfNecessary with the same pattern.
                            var gumProject = ObjectFinder.Self.GumProjectSave;
                            bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                            bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                            bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;
#endif

                            FontService.CreateFontIfNecessary(bmfcSave);
                        }
                        catch
                        {
                            // do nothing?
                        }
                    }
#endif

                    if (ToolsUtilities.FileManager.FileExists(fileName))
                    {
                        font = new BitmapFont(fileName);
                    }
                    else
                    {
                        // This can happen when closing tags are encountered at the end of a font. If no font exists, we can just go to the default
                        font = Text.DefaultBitmapFont;
                    }
                    // #3530: Replace so re-adding an already-occupied key heals it instead of throwing.
                    global::RenderingLibrary.Content.LoaderManager.Self.AddDisposable(fontFileName, font,
                        global::RenderingLibrary.Content.LoaderManager.ExistingContentBehavior.Replace);
                }
            }

            return font;
#else
            // Raylib can only produce a crisp font for the current stack state via a wired IRaylibFontCreator
            // (it cannot `new BitmapFont(file)` the way the XNA path does). The Raylib path always uses the
            // FontCache key/creator - it does not branch on useCustomFontStack (matching the pre-#3624
            // behavior, where custom fonts were not re-generated per inline run).
            if (InMemoryFontCreator == null || fontSizeStack.Peek() <= 0)
            {
                return null;
            }

            var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

            try
            {
                string fontName = fontNameStack.Peek();
                string? bbCodeFontFilePath = BmfcSave.IsFontFilePath(fontName) ? fontName : null;

                string fontCacheName = BmfcSave.GetFontCacheFileNameFor(
                    fontSizeStack.Peek(),
                    fontName,
                    outlineThicknessStack.Peek(),
                    useFontSmoothingStack.Peek(),
                    isItalicStack.Peek(),
                    isBoldStack.Peek(),
                    bbCodeFontFilePath);
                string fullFileName = ToolsUtilities.FileManager.Standardize(fontCacheName, preserveCase: true, makeAbsolute: true);

                // Reuse an already-generated font (its Raylib_cs.Font wraps an unmanaged GPU texture, so
                // regenerating every layout would leak VRAM). Mirrors the base-font cache check.
                if (loaderManager.GetDisposable(fullFileName) is ManagedFont cachedManagedFont
                    && cachedManagedFont.Font.BaseSize != 0)
                {
                    return cachedManagedFont.Font;
                }

                BmfcSave bmfcSave = new BmfcSave();
                bmfcSave.FontSize = fontSizeStack.Peek();
                bmfcSave.OutlineThickness = outlineThicknessStack.Peek();
                bmfcSave.UseSmoothing = useFontSmoothingStack.Peek();
                bmfcSave.IsItalic = isItalicStack.Peek();
                bmfcSave.IsBold = isBoldStack.Peek();

                if (bbCodeFontFilePath != null)
                {
                    bmfcSave.FontFile = ResolveFontFilePath(bbCodeFontFilePath);
                    bmfcSave.FontName = System.IO.Path.GetFileNameWithoutExtension(bbCodeFontFilePath);
                }
                else
                {
                    bmfcSave.FontName = fontName;
                }

                var gumProject = ObjectFinder.Self.GumProjectSave;
                bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;

                Raylib_cs.Font? createdFont = InMemoryFontCreator.TryCreateFont(bmfcSave);
                if (createdFont.HasValue && createdFont.Value.BaseSize != 0)
                {
                    // Dispose first so a poisoned empty slot (see the base path) can't make AddDisposable
                    // throw; then cache under the same FontCache key the base-font path uses so a base text
                    // and an inline run at the same size/style share one atlas.
                    loaderManager.Dispose(fullFileName);
                    loaderManager.AddDisposable(fullFileName, new ManagedFont(createdFont.Value));
                    return createdFont.Value;
                }
            }
            catch
            {
                // Fall through to null - the caller uses the base font / base-atlas scale fallback.
            }

            return null;
#endif
        }

#if !RAYLIB
        string GetFontFileName()
        {
            string fontFileNameName;
            if (useCustomFontStack.Peek())
            {
                fontFileNameName = fontNameStack.Peek() + ".fnt";
            }
            else
            {
                string? bbCodeFontFilePath = BmfcSave.IsFontFilePath(fontNameStack.Peek()) ? fontNameStack.Peek() : null;

                fontFileNameName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                    fontSizeStack.Peek(),
                    fontNameStack.Peek(),
                    outlineThicknessStack.Peek(),
                    useFontSmoothingStack.Peek(),
                    isItalicStack.Peek(),
                    isBoldStack.Peek(),
                    bbCodeFontFilePath);

            }

            var fullFileName = ToolsUtilities.FileManager.RemoveDotDotSlash(ToolsUtilities.FileManager.Standardize(fontFileNameName, false, true));
#if ANDROID || IOS
            fullFileName = fullFileName.ToLowerInvariant();
#endif
            return fullFileName;
        }
#endif
    }

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

#if !RAYLIB
        // Residual properties could exist on a Text instance, so we need to
        // tolerate a missing item and not crash.

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as Gum.GueDeriving.TextRuntime;

#endif
        if (text == null || textRuntime == null)
        {
            return;
        }

        BitmapFont font = null;

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        var contentLoader = loaderManager.ContentLoader;

        if (textRuntime.UseCustomFont)
        {

            if (!string.IsNullOrEmpty(textRuntime.CustomFontFile))
            {
                font = loaderManager.GetDisposable(textRuntime.CustomFontFile) as BitmapFont;
                if (font == null)
                {
#if KNI
                        try
                        {
                            // this could be running in browser where we don't have File.Exists, so JUST DO IT
                            font = new BitmapFont(textRuntime.CustomFontFile);
                            loaderManager.AddDisposable(textRuntime.CustomFontFile, font);
                        }
                        catch
                        {
                            // font doesn't exist, carry on...
                        }
#else
                    // so normally we would just let the content loader check if the file exists but since we're not going to
                    // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                    if (ToolsUtilities.FileManager.FileExists(textRuntime.CustomFontFile))
                    {
                        try
                        {
                            font = new BitmapFont(textRuntime.CustomFontFile);
                            loaderManager.AddDisposable(textRuntime.CustomFontFile, font);

                        }
                        catch(System.Text.DecoderFallbackException exception)
                        {
                            throw new Exception($"Error trying to load font from file {textRuntime.CustomFontFile}:\n" + exception, exception);
                        }
                    }
#endif
                }
                else if (font.Textures.Any(item => item?.IsDisposed == true))
                {
                    // The BitmapFont is cached by Gum, but the underlying Texture2D might be managed by something else (like FRB).
                    // This means that the Texture can be disposed without the BitmapFont being disposed. If this is the case we should
                    // ask the underlying system for a new .png, but we can keep the same BitmapFont since that should stay the same and
                    // .fnt parsing can be the slow part for large fonts.
                    font.ReAssignTextures();
                }
            }


        }
        else
        {
            if (textRuntime.FontSize > 0 && !string.IsNullOrEmpty(textRuntime.Font))
            {

                string? fontFilePath = BmfcSave.IsFontFilePath(textRuntime.Font) ? textRuntime.Font : null;

                string fontName = textRuntime.GetFontCacheFileName(fontFilePath);

                string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                font = loaderManager.GetDisposable(fullFileName) as BitmapFont;

                // Attempt to load from Embedded Resource

                if (fontName != null && font == null)
                {
                    font = GetFontDisposable(fontName);
                }

                // Try in-memory font creation first (no disk I/O)
                if (font == null && InMemoryFontCreator != null)
                {
                    try
                    {
                        BmfcSave bmfcSave = new BmfcSave();
                        textRuntime.CopyFontGenerationFieldsTo(
                            bmfcSave,
                            fontFilePath != null ? ResolveFontFilePath(fontFilePath) : null);

                        var gumProject = ObjectFinder.Self.GumProjectSave;
                        bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                        bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                        bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;

                        font = InMemoryFontCreator.TryCreateFont(bmfcSave);
                        if (font != null)
                        {
                            loaderManager.AddDisposable(fullFileName, font);
                        }
                    }
                    catch
                    {
                        // Fall through to disk-based path
                    }
                }

#if !FRB
                // Disk-based font creation: ask FontService to generate .fnt/.png files,
                // then load from disk. This is the fallback when no InMemoryFontCreator
                // is available or when in-memory creation fails.
                if (font == null && FontService != null)
                {
                    try
                    {
                        BmfcSave bmfcSave = new BmfcSave();
                        textRuntime.CopyFontGenerationFieldsTo(
                            bmfcSave,
                            fontFilePath != null ? ResolveFontFilePath(fontFilePath) : null);

                        var gumProject = ObjectFinder.Self.GumProjectSave;
                        bmfcSave.Ranges = gumProject?.FontRanges ?? BmfcSave.GetEffectiveDefaultRanges();
                        bmfcSave.SpacingHorizontal = gumProject?.FontSpacingHorizontal ?? 1;
                        bmfcSave.SpacingVertical = gumProject?.FontSpacingVertical ?? 1;

                        FontService.CreateFontIfNecessary(bmfcSave);
                    }
                    catch
                    {
                        // Font creation can fail for many reasons (invalid font name, missing bmfont.exe, etc.)
                        // Silently fall through to the disk load attempt or default font fallback.
                    }
                }
#endif

                if (font == null || font.Texture?.IsDisposed == true)
                {
#if KNI
                        try
                        {
                            // this could be running in browser where we don't have File.Exists, so JUST DO IT
                            font = new BitmapFont(fullFileName);

                            loaderManager.AddDisposable(fullFileName, font);
                        }
                        catch
                        {
                            // font doesn't exist, carry on...
                        }
#else
                    // so normally we would just let the content loader check if the file exists but since we're not going to
                    // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                    if (ToolsUtilities.FileManager.FileExists(fullFileName))
                    {
                        // kill the old font:
                        if(font?.Texture?.IsDisposed == true)
                        {
                            loaderManager.Dispose(fullFileName);
                        }

                        try
                        {
                            font = new BitmapFont(fullFileName);
                            loaderManager.AddDisposable(fullFileName, font);
                        }
                        catch (System.Text.DecoderFallbackException exception)
                        {
                            throw new Exception($"Error trying to load font from file {fullFileName}:\n" + exception, exception);
                        }
                    }
#endif
                }

                // FRB may dispose fonts, so let's check:

#if FULL_DIAGNOSTICS
                if (font?.Textures.Any(item => item?.IsDisposed == true) == true)
                {
                    throw new InvalidOperationException("The returned font has a disposed texture");
                }
#endif
            }
        }

        var fontToSet = font ?? Text.DefaultBitmapFont;

        if (asText.BitmapFont != fontToSet)
        {
            asText.BitmapFont = fontToSet;

            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // When this font load is the #2999 deferred-font flush performed from inside
                // UpdateLayout, the enclosing layout pass already sizes this element, so this
                // extra UpdateLayout would be redundant and re-entrant (no-arg UpdateLayout also
                // requests a parent update). Skip it in that case.
                if (!GraphicalUiElement.SuppressLayoutFromFontChange)
                {
                    graphicalUiElement.UpdateLayout();
                }
            }
        }
#else
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
#endif

        // Re-parse BBCode segments so they pick up the new base font values. Without this, only the
        // non-tagged text updates; segments after BBCode tags retain old font properties because their
        // InlineVariables still reference the previous resolved font.
        if (!string.IsNullOrEmpty(asText.StoredMarkupText))
        {
            asText.InlineVariables.Clear();
            SetBbCodeText(asText, graphicalUiElement, asText.StoredMarkupText);
        }
    }

#if RAYLIB
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
#endif

#if !RAYLIB
    private static BitmapFont? GetFontDisposable(string fontName)
    {
#if KNI
        string prefix = "KniGum";
#elif FNA
        string prefix = "FnaGum";
#else
        string prefix = "MonoGameGum.Content";
#endif

        string fontFilenameOnly = Path.GetFileName(fontName);
        string embeddedFontName = $"EmbeddedResource.{prefix}.{fontFilenameOnly}";
        return global::RenderingLibrary.Content.LoaderManager.Self.GetDisposable(embeddedFontName) as BitmapFont;
    }
#endif

#endregion

#if !RAYLIB
    private static bool TrySetPropertyOnLineRectangle(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }
        else if (propertyName == "Color")
        {
            //var valueAsColor = (Color)value;
            //((LineRectangle)mContainedObjectAsIpso).Color = valueAsColor;
            var lineRectangle = (LineRectangle) mContainedObjectAsIpso;
            //var valueAsColor = (Color)value;
            if (value is System.Drawing.Color drawingColor)
            {
                lineRectangle.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                lineRectangle.Color = xnaColor.ToSystemDrawing();

            }

            handled = true;
        }
        else if(propertyName == "IsRenderTarget")
        {
            ((LineRectangle)mContainedObjectAsIpso).IsRenderTarget = value as bool? ?? false;
            handled = true;
        }
        else if (propertyName == "SourceShaderFile")
        {
            // The Gum editor backs Containers with a LineRectangle, so the render-target
            // post-process shader resolves onto the LineRectangle's IRenderTargetRenderable slot
            // (the runtime's InvisibleRenderable path does the same in TrySetPropertyOnContainer).
            AssignSourceShaderFileOnContainer((LineRectangle)mContainedObjectAsIpso, graphicalUiElement, value as string);
            handled = true;
        }

        return handled;
    }
#endif

#if !RAYLIB
    private static bool TrySetPropertyOnLineCircle(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Color")
        {
            if(value is System.Drawing.Color drawingColor)
            {
                ((LineCircle)mContainedObjectAsIpso).Color = drawingColor;
            }
            else if(value is Microsoft.Xna.Framework.Color xnaColor)
            {
                ((LineCircle)mContainedObjectAsIpso).Color = xnaColor.ToSystemDrawing();
            }
            handled = true;
        }

        else if (propertyName == "Radius")
        {
            var valueAsFloat = (float)value;
            ((LineCircle)mContainedObjectAsIpso).Width = 2 * valueAsFloat;
            ((LineCircle)mContainedObjectAsIpso).Height = 2 * valueAsFloat;
            ((LineCircle)mContainedObjectAsIpso).Radius = valueAsFloat;
            graphicalUiElement.Width = 2 * valueAsFloat;
            graphicalUiElement.Height = 2 * valueAsFloat;
        }

        return handled;
    }
#endif

#if !RAYLIB
    private static bool TrySetPropertyOnLinePolygon(IRenderableIpso mContainedObjectAsIpso, string propertyName, object value)
    {
        bool handled = false;


        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Color")
        {
            var valueAsColor = (Color)value;
            ((LinePolygon)mContainedObjectAsIpso).Color = valueAsColor;
            handled = true;
        }


        else if (propertyName == "Points")
        {
            var points = (List<Vector2>)value;

            ((LinePolygon)mContainedObjectAsIpso).SetPoints(points);
            handled = true;
        }

        return handled;
    }
#endif

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
        else if (value.EndsWith(".achx"))
        {
            AnimationChainList? animationChainList = null;
            try
            {
                animationChainList = GetAnimationChainList(ref value, loaderManager);
                sprite.AnimationChains = animationChainList;
            }
            catch(Exception ex)
            {
                string message = $"Error setting SourceFile to on Sprite";

                if (graphicalUiElement.Tag != null)
                {
                    message += $" in {graphicalUiElement.Tag}";
                }
                message += $"\n{value}";
                message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
                message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
                if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                {
                    if (ObjectFinder.Self.GumProjectSave == null)
                    {
                        message += "\nNo Gum project has been loaded";
                    }

                    throw new System.IO.FileNotFoundException(message, ex);
                }
                sprite.AnimationChains = null;

                PropertyAssignmentError?.Invoke(message + "\n" + ex.ToString());
            }


            sprite.RefreshCurrentChainToDesiredName();

            sprite.UpdateToCurrentAnimationFrame();

            graphicalUiElement.UpdateTextureValuesFrom(sprite);
            handled = true;
        }
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;

                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

#if !RAYLIB
            // see if an atlas exists:
            var atlasedTexture = loaderManager.TryLoadContent<AtlasedTexture>(value);

            if (atlasedTexture != null)
            {
                graphicalUiElement.UpdateLayout();
            }
            else
#endif
            {
                // We used to check if the file exists. But internally something may
                // alias a file. Ultimately the content loader should make that decision,
                // not the GUE
                Texture2D? texture = null;
                Exception? loadException = null;
                try
                {
                    texture = loaderManager.LoadContent<Texture2D>(value);
                }
                catch (Exception ex)
                // Jan 1, 2025 - we used to only catch certain types of exceptions, but this list keeps growing as there
                // are a variety of types of crashes that can occur. NineSlice catches all exceptions, so let's just do that!
                //when (ex is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException or WebException or IOException)
                {
                    loadException = ex;
                }

                if (texture == null)
                {
                    // On desktop the loader returns null for a missing file instead of throwing, and a
                    // genuine load error is funneled here too (loadException). Report it the same way the
                    // catch used to.
                    string message = $"Error setting SourceFile on Sprite";

                    if (graphicalUiElement.Tag != null)
                    {
                        message += $" in {graphicalUiElement.Tag}";
                    }
                    message += $"\n{value}";
                    message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
                    message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
                    if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                    {
                        if (ObjectFinder.Self.GumProjectSave == null)
                        {
                            message += "\nNo Gum project has been loaded";
                        }

                        throw new System.IO.FileNotFoundException(message, loadException);
                    }
                    sprite.Texture = null;

                    PropertyAssignmentError?.Invoke(loadException != null ? message + "\n" + loadException.ToString() : message);
                }
                else
                {
                    sprite.Texture = texture;
                }
                graphicalUiElement.UpdateLayout();
            }
        }
        handled = true;
        return handled;
    }

    private static AnimationChainList? GetAnimationChainList(ref string value,
        // fully qualify to avoid Android namign conflicts
        global::RenderingLibrary.Content.LoaderManager loaderManager)
    {
        if (ToolsUtilities.FileManager.IsRelative(value))
        {
            value = ToolsUtilities.FileManager.RelativeDirectory + value;

            value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
        }

        AnimationChainList? animationChainList = null;

        if (loaderManager.CacheTextures)
        {
            animationChainList = loaderManager.GetDisposable(value) as AnimationChainList;
        }

        if (animationChainList == null)
        {
            var animationChainListSave = AnimationChainListSave.FromFile(value);
            animationChainList = animationChainListSave.ToAnimationChainList();
            if (loaderManager.CacheTextures)
            {
                loaderManager.AddDisposable(value, animationChainList);
            }
        }

        return animationChainList;
    }

    public static void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers, Layer layer)
    {
        var managers = iSystemManagers as SystemManagers;

#if !RAYLIB
        if (renderable is Sprite)
        {
            managers.SpriteManager.Add(renderable as Sprite, layer);
        }
        else if (renderable is NineSlice)
        {
            managers.SpriteManager.Add(renderable as NineSlice, layer);
        }
        else if (renderable is LineRectangle)
        {
            managers.ShapeManager.Add(renderable as LineRectangle, layer);
        }
        else if (renderable is SolidRectangle)
        {
            managers.ShapeManager.Add(renderable as SolidRectangle, layer);
        }
        else if (renderable is Text)
        {
            managers.TextManager.Add(renderable as Text, layer);
        }
        else if (renderable is LineCircle)
        {
            managers.ShapeManager.Add(renderable as LineCircle, layer);
        }
        else if (renderable is LinePolygon)
        {
            managers.ShapeManager.Add(renderable as LinePolygon, layer);
        }
        else if (renderable is InvisibleRenderable)
        {
            managers.SpriteManager.Add(renderable as InvisibleRenderable, layer);
        }
        else
#endif
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
    /// Detaches <paramref name="renderable"/> from the renderer so it stops drawing. On raylib,
    /// where the add path (<see cref="AddRenderableToManagers"/>) is purely layer-based (no
    /// Sprite/Shape/TextManager), removal just searches the renderer's layers for the renderable
    /// and removes it (issue #3048). On XNA-like backends, removal dispatches by renderable type
    /// to the matching manager, mirroring the type-dispatch add path.
    /// </summary>
    public static void RemoveRenderableFromManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers)
    {
#if RAYLIB
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
#else
        var managers = (SystemManagers)iSystemManagers;

        if (renderable is Sprite asSprite)
        {
            managers.SpriteManager.Remove(asSprite);
        }
        else if (renderable is NineSlice asNineSlice)
        {
            managers.SpriteManager.Remove(asNineSlice);
        }
        else if (renderable is global::RenderingLibrary.Math.Geometry.LineRectangle asLineRectangle)
        {
            managers.ShapeManager.Remove(asLineRectangle);
        }
        else if (renderable is global::RenderingLibrary.Math.Geometry.LinePolygon asLinePolygon)
        {
            managers.ShapeManager.Remove(asLinePolygon);
        }
        else if (renderable is global::RenderingLibrary.Graphics.SolidRectangle solidRectangle)
        {
            managers.ShapeManager.Remove(solidRectangle);
        }
        else if (renderable is Text asText)
        {
            managers.TextManager.Remove(asText);
        }
        else if (renderable is LineCircle asLineCircle)
        {
            managers.ShapeManager.Remove(asLineCircle);
        }
        else if (renderable is InvisibleRenderable asInvisibleRenderable)
        {
            managers.SpriteManager.Remove(asInvisibleRenderable);
        }
        else if (renderable != null)
        {
            // This could be a custom visual object, so don't do anything:
            //throw new NotImplementedException();
            managers.Renderer.RemoveRenderable(renderable);
        }
        if (renderable is IManagedObject asManagedObject)
        {
            asManagedObject.RemoveFromManagers();
        }
#endif
    }

    public static void ThrowExceptionsForMissingFiles(GraphicalUiElement graphicalUiElement)
    {
#if XNALIKE
        // We can't throw exceptions when assigning values on fonts because the font values get set one-by-one
        // and the end result of all values determines which file to load. For example, an object may set the following
        // variables one-by-one:
        // * FontSize
        // * Font
        // * OutlineThickness
        // Let's say the Font gets set to Arial. The FontSize may not have been set yet, so whatever value happens
        // to be there will be used to load the font (like 12). But the user may not have Arial12 in their project,
        // and if we threw an exception on-the-spot, the user would see a message about missing Arial12, even though
        // the project doesn't actually use Arial12.
        // We need to wait until the graphical UI element is fully created before we try to throw an exception, so
        // that's what we're going to do here:
        if (graphicalUiElement != null && graphicalUiElement.RenderableComponent is Text asText)
        {

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
            var textRuntime = graphicalUiElement as Gum.GueDeriving.TextRuntime;

#endif


            // check it
            if (asText.BitmapFont == null)
            {
                if (textRuntime.UseCustomFont)
                {
                    var fontName = ToolsUtilities.FileManager.Standardize(textRuntime.CustomFontFile, preserveCase: true, makeAbsolute: true);

                    throw new System.IO.FileNotFoundException($"Missing:{fontName}");
                }
                else
                {
                    if (textRuntime.FontSize > 0 && !string.IsNullOrEmpty(textRuntime.Font))
                    {
                        string fontName = textRuntime.GetFontCacheFileName(
                            BmfcSave.IsFontFilePath(textRuntime.Font) ? textRuntime.Font : null);

                        var standardized = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                        throw new System.IO.FileNotFoundException($"Missing:{standardized}");
                    }
                }
            }
            else
            {
                // we have a valid font file, so let's make sure the BitmapFont matches the expected font
                if (textRuntime.UseCustomFont)
                {
                    var expectedFont = textRuntime.CustomFontFile?.Replace("\\", "/");
                    var currentFont = asText.BitmapFont.FontFile?.Replace("\\", "/");

                    if (expectedFont != null && !expectedFont.Equals(currentFont, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new System.IO.FileNotFoundException($"Expected:{expectedFont} but currently using:{currentFont}");
                    }
                }
            }
        }
#endif

        foreach (var element in graphicalUiElement.ContainedElements)
        {
            ThrowExceptionsForMissingFiles(element);
        }
    }

    /// <summary>
    /// Resolves a font file path to an absolute path using the Gum project directory.
    /// Font generators (KernSmith and bmfont.exe) resolve paths relative to their own
    /// working directory, not the project directory, so relative paths must be made absolute.
    /// </summary>
    /// <param name="fontFilePath">The font file path, which may be relative to the project directory.</param>
    /// <returns>The absolute font file path, or the original value if already absolute or if no project is loaded.</returns>
    private static string ResolveFontFilePath(string fontFilePath)
    {
        if (System.IO.Path.IsPathRooted(fontFilePath))
        {
            return fontFilePath;
        }

        var gumProject = ObjectFinder.Self.GumProjectSave;
        if (gumProject != null)
        {
            string projectDir = ToolsUtilities.FileManager.GetDirectory(gumProject.FullFileName);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDir, fontFilePath));
        }

        return fontFilePath;
    }

#if !RAYLIB && !FRB
    // Issue #2925 — legacy variable routing for the two-slot CircleRuntime. The legacy
    // properties (Color/Alpha/Red/Green/Blue) on the runtime are intentionally obsolete and
    // already route to the stroke slot internally, preserving pre-two-slot behavior. Going
    // through them here means the helper does not need to know which slot is the contained
    // object or which backend produced the renderable.
    private static bool TrySetPropertyOnCircleRuntime(Gum.GueDeriving.CircleRuntime circleRuntime, string propertyName, object value)
    {
#pragma warning disable CS0618 // legacy obsolete properties are the public surface this dispatch was written against
        switch (propertyName)
        {
            case "Color":
                if (value is System.Drawing.Color drawingColor)
                {
                    circleRuntime.Color = new Microsoft.Xna.Framework.Color(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
                }
                else if (value is Microsoft.Xna.Framework.Color xnaColor)
                {
                    circleRuntime.Color = xnaColor;
                }
                else
                {
                    return false;
                }
                return true;
            case "Alpha":
                circleRuntime.Alpha = (int)value;
                return true;
            case "Red":
                circleRuntime.Red = (int)value;
                return true;
            case "Green":
                circleRuntime.Green = (int)value;
                return true;
            case "Blue":
                circleRuntime.Blue = (int)value;
                return true;
            case "Radius":
                circleRuntime.Radius = (float)value;
                return true;
        }
        return false;
#pragma warning restore CS0618
    }

    // Issue #2925 — same pattern for RectangleRuntime.
    private static bool TrySetPropertyOnRectangleRuntime(Gum.GueDeriving.RectangleRuntime rectangleRuntime, string propertyName, object value)
    {
#pragma warning disable CS0618
        switch (propertyName)
        {
            case "Color":
                if (value is System.Drawing.Color drawingColor)
                {
                    rectangleRuntime.Color = new Microsoft.Xna.Framework.Color(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
                }
                else if (value is Microsoft.Xna.Framework.Color xnaColor)
                {
                    rectangleRuntime.Color = xnaColor;
                }
                else
                {
                    return false;
                }
                return true;
            case "Alpha":
                rectangleRuntime.Alpha = (int)value;
                return true;
            case "Red":
                rectangleRuntime.Red = (int)value;
                return true;
            case "Green":
                rectangleRuntime.Green = (int)value;
                return true;
            case "Blue":
                rectangleRuntime.Blue = (int)value;
                return true;
            case "CornerRadius":
                rectangleRuntime.CornerRadius = (float)value;
                return true;
            case "CustomRadiusTopLeft":
                rectangleRuntime.CustomRadiusTopLeft = (float?)value;
                return true;
            case "CustomRadiusTopRight":
                rectangleRuntime.CustomRadiusTopRight = (float?)value;
                return true;
            case "CustomRadiusBottomLeft":
                rectangleRuntime.CustomRadiusBottomLeft = (float?)value;
                return true;
            case "CustomRadiusBottomRight":
                rectangleRuntime.CustomRadiusBottomRight = (float?)value;
                return true;
        }
        return false;
#pragma warning restore CS0618
    }
#endif
}

#if FRB
// FRB compiles Gum source without a TextRuntime, so the shared font-loading code above uses
// GraphicalUiElement directly and cannot see TextRuntime's font-cache helpers (which live behind
// #if !FRB). These extension methods provide the same helpers on GraphicalUiElement for the FRB
// build. FRB's GraphicalUiElement has no dropshadow font properties, so dropshadow is intentionally
// omitted here (matching the pre-#3413 inline behavior on FRB).
internal static class FrbGraphicalUiElementFontExtensions
{
    internal static string GetFontCacheFileName(this GraphicalUiElement textRuntime, string? fontFilePath)
    {
        return BmfcSave.GetFontCacheFileNameFor(
            textRuntime.FontSize,
            textRuntime.Font,
            textRuntime.OutlineThickness,
            textRuntime.UseFontSmoothing,
            textRuntime.IsItalic,
            textRuntime.IsBold,
            fontFilePath);
    }

    internal static void CopyFontGenerationFieldsTo(this GraphicalUiElement textRuntime,
        BmfcSave bmfcSave, string? resolvedFontFilePath)
    {
        bmfcSave.FontSize = textRuntime.FontSize;
        bmfcSave.OutlineThickness = textRuntime.OutlineThickness;
        bmfcSave.UseSmoothing = textRuntime.UseFontSmoothing;
        bmfcSave.IsItalic = textRuntime.IsItalic;
        bmfcSave.IsBold = textRuntime.IsBold;

        if (resolvedFontFilePath != null)
        {
            bmfcSave.FontFile = resolvedFontFilePath;
            bmfcSave.FontName = System.IO.Path.GetFileNameWithoutExtension(resolvedFontFilePath);
        }
        else
        {
            bmfcSave.FontName = textRuntime.Font;
            bmfcSave.FontFile = null;
        }
    }
}
#endif
