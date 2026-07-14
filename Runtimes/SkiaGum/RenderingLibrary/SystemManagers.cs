using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using SkiaGum;
using SkiaGum.Content;
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaPlugin.Managers;
using System;
using Texture2D = SkiaSharp.SKBitmap;

namespace RenderingLibrary
{
    public partial class SystemManagers : ISystemManagers
    {
        #region Fields/Properties

        /// <summary>
        /// The font scale value. This can be used to scale all fonts globally,
        /// generally in response to a font scaling value like the Android font scale setting.
        /// </summary>
        public static float GlobalFontScale { get; set; } = 1.0f;

        // Mirrors the MonoGame/Raylib AssemblyPrefix pattern (issue #3561) so
        // LoadEmbeddedTexture2d below and Gum.Forms.DefaultVisuals.V3.Styling's embedded
        // sprite-sheet lookup resolve to this assembly's embedded resources.
        public static string AssemblyPrefix => "SkiaGum.Content";

        static SystemManagers _default;
        public static SystemManagers Default
        {
            get => _default;
            set
            {
                _default = value;
                ISystemManagers.Default = value;
            }
        }

        public SkiaSharp.SKCanvas Canvas { get; set; }


        public RenderingLibrary.Graphics.Renderer Renderer { get; private set; }
        public bool EnableTouchEvents { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        IRenderer ISystemManagers.Renderer => Renderer;

        #endregion

        static bool HasInitializedGlobal = false;

        public void Initialize()
        {
            //mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            Renderer = new Renderer();
            Renderer.Initialize(this);

            //SpriteManager = new SpriteManager();

            //ShapeManager = new ShapeManager();

            //TextManager = new TextManager();

            //SpriteManager.Managers = this;
            //ShapeManager.Managers = this;
            //Tex

            if(!HasInitializedGlobal)
            {
                // If we don't do this, then multiple windows may initialize the same thing, causing events to stack,
                // and wiping customization of standard elements:
                HasInitializedGlobal = true;
                StandardElementsManager.Self.Initialize();
                StandardElementsManager.Self.CustomGetDefaultState = HandleCustomGetDefaultState;
                ElementSaveExtensions.CustomCreateGraphicalComponentFunc = HandleCreateGraphicalComponent;
                GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
                GraphicalUiElement.UpdateFontFromProperties = UpdateFonts;

                GraphicalUiElement.AddRenderableToManagers = AddRenderableToManagers;
                GraphicalUiElement.RemoveRenderableFromManagers = RemoveRenderableFromManagers;
                SkiaResourceManager.Initialize(null);
                LoaderManager.Self.ContentLoader = LoaderManager.Self.ContentLoader ?? new EmbeddedResourceContentLoader();
                RegisterComponentRuntimeInstantiations();
            }


        }

        private void UpdateFonts(IText text, GraphicalUiElement element)
        {
            if(text is Text asText && element is TextRuntime textRuntime)
            {
                asText.FontName = textRuntime.Font ?? "Arial";
                asText.IsItalic = textRuntime.IsItalic;
                // BoldWeight is an embolden multiplier (1.0 = normal). Do NOT set CSS weights (400/700) here.
                asText.BoldWeight = textRuntime.IsBold ? 1.5f : 1.0f;
                asText.FontSize = textRuntime.FontSize;
                // Push OutlineThickness here too: this delegate is the code-property path
                // (GraphicalUiElement.OutlineThickness setter -> UpdateToFontValues -> this). #3675
                // only wired the string/SetProperty path (CustomSetPropertyOnRenderable.UpdateToFontValues),
                // so setting OutlineThickness in code silently rendered no halo (bug #3684).
                asText.OutlineThickness = textRuntime.OutlineThickness;

                // SkiaGum can't bake a KernSmith shadow atlas, so map the cross-backend baked-shadow API
                // (TextRuntime.HasDropshadow + params) onto the SkiaGum.Text renderable's standalone
                // ImageFilter drop shadow (#3674): the same user-facing API renders on Skia via a live
                // canvas effect instead of a baked atlas. DropshadowBlur is a single scalar on the
                // runtime; the renderable takes separate X/Y blur, so drive both from it. (The blur
                // magnitudes won't match the baked backends exactly — different blur conventions — but
                // the shadow renders and honors color/offset/blur.)
                asText.HasDropshadow = textRuntime.HasDropshadow;
                asText.DropshadowColor = textRuntime.DropshadowColor;
                asText.DropshadowOffsetX = textRuntime.DropshadowOffsetX;
                asText.DropshadowOffsetY = textRuntime.DropshadowOffsetY;
                asText.DropshadowBlurX = textRuntime.DropshadowBlur;
                asText.DropshadowBlurY = textRuntime.DropshadowBlur;
            }
        }

        private void RegisterComponentRuntimeInstantiations()
        {
            // Construct the SkiaGum.GueDeriving shim subclasses (not the new Gum.GueDeriving base
            // types). This file has `using Gum.GueDeriving;`, so an unqualified `new ContainerRuntime()`
            // resolves to the base type — but already-generated user code casts the loaded instance to
            // the deprecated shim namespace (`... as SkiaGum.GueDeriving.ContainerRuntime`), and that
            // cast only succeeds when the instance is the most-derived shim. Instantiating the base
            // yields null and a NullReferenceException downstream (issue #3380). Mirrors the same fix
            // in RenderingLibrary.SystemManagers (MonoGame) and AposShapeRuntime. Drop the
            // qualification once the SkiaGum.GueDeriving shims are removed.
#pragma warning disable CS0618 // Type or member is obsolete
            ElementSaveExtensions.RegisterGueInstantiation(
                "Arc",
                () => new global::SkiaGum.GueDeriving.ArcRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Circle",
                () => new global::SkiaGum.GueDeriving.CircleRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "ColoredCircle",
                () => new global::SkiaGum.GueDeriving.ColoredCircleRuntime());


            ElementSaveExtensions.RegisterGueInstantiation(
                "Container",
                () => new global::SkiaGum.GueDeriving.ContainerRuntime());

            // Issue #3324 — without this registration a "Line" standard type created no
            // renderable, so a Line was silently dropped from SVG export (the same #3259-class
            // gap that hit Rectangle). The XNALIKE/Apos backend registers the same runtime in
            // AposShapeRuntime. Pairs with the "Line" arm added to HandleCustomGetDefaultState.
            ElementSaveExtensions.RegisterGueInstantiation(
                "Line",
                () => new global::SkiaGum.GueDeriving.LineRuntime());

            //ElementSaveExtensions.RegisterGueInstantiation(
            //    "NineSlice",
            //    () => new NineSliceRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Polygon",
                () => new global::SkiaGum.GueDeriving.PolygonRuntime());

            // Issue #3259 — the v3 "Rectangle" standard type (filled/rounded/stroked) renders
            // through RectangleRuntime, whose SKIA build wraps a RoundedRectangle fill+stroke
            // pair (#2814/#2818). Without this registration SkiaGum created no renderable for a
            // "Rectangle" base type, so the shape was silently dropped from SVG export (gumcli
            // svg / tool File ▸ Export) and any SkiaGum-hosted render — only Text/Container/etc.
            // drew. The XNALIKE/raylib backends register the same runtime in their SystemManagers.
            // RectangleRuntime has no SkiaGum.GueDeriving shim, so it stays the canonical base type.
            ElementSaveExtensions.RegisterGueInstantiation(
                "Rectangle",
                () => new RectangleRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Sprite",
                () => new global::SkiaGum.GueDeriving.SpriteRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Text",
                () => new global::SkiaGum.GueDeriving.TextRuntime());
#pragma warning restore CS0618
        }

        private IRenderable? HandleCreateGraphicalComponent(string type, ISystemManagers managers)
        {
            return type switch
            {
                "Arc" => new Arc(),
                "Canvas" => new CanvasRenderable(),
                "Circle" => new Circle(),
                "ColoredCircle" => new Circle(),
                "ColoredRectangle" => new SolidRectangle(),
                "LottieAnimation" => new LottieAnimation(),
                "NineSlice" => new NineSlice(),
                "Polygon" => new Polygon(),
                "RoundedRectangle" => new RoundedRectangle(),
                "Svg" => new VectorSprite(),
                "Sprite" => new Sprite(),
                "Text" => new Text(),
                _ => null,
            };
        }

        private StateSave? HandleCustomGetDefaultState(string arg)
        {
            return arg switch
            {
                "Arc" => StandardElementsManager.GetArcState(),
                "Canvas" => DefaultStateManager.GetCanvasState(),
                "ColoredCircle" => StandardElementsManager.GetColoredCircleState(),
                // Issue #3324 — Line was the one extended shape type missing here, so headless
                // SVG export (gumcli svg / File ▸ Export) threw "Could not get the default state
                // for type Line" during GumProjectSave.Initialize for any project containing one.
                "Line" => StandardElementsManager.GetLineState(),
                "LottieAnimation" => DefaultStateManager.GetLottieAnimationState(),
                "RoundedRectangle" => StandardElementsManager.GetRoundedRectangleState(),
                "Svg" => DefaultStateManager.GetSvgState(),
                _ => null,
            };
        }

        private void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers managers, Layer layer)
        {
            (layer ?? managers.Renderer.Layers[0]).Add(renderable);
        }

        private void RemoveRenderableFromManagers(IRenderableIpso renderable, ISystemManagers managers)
        {
            if (renderable == null) return;
            foreach (var layer in managers.Renderer.Layers)
            {
                if (layer.Renderables.Contains(renderable))
                {
                    layer.Remove(renderable);
                    return;
                }
            }
        }

        public void InvalidateSurface()
        {

        }

        public void Draw()
        {
            Renderer.Draw(this);
        }

        /// <summary>
        /// Loads a texture into the Disposable cache from the Embedded Resource within the application.
        /// Mirrors MonoGame's/Raylib's LoadEmbeddedTexture2d (issue #3561) so
        /// Gum.Forms.FormsUtilities.InitializeDefaults can load the shared UISpriteSheet.png on Skia.
        /// </summary>
        /// <param name="embeddedTexture2dName"></param>
        /// <returns></returns>
        public Texture2D? LoadEmbeddedTexture2d(string embeddedTexture2dName)
        {
            var assembly = typeof(SystemManagers).Assembly;
            using var stream = ToolsUtilities.FileManager.GetStreamFromEmbeddedResource(assembly,
                $"{AssemblyPrefix}.{embeddedTexture2dName}");

            Texture2D texture = Texture2D.Decode(stream);

            var resourceName = $"{AssemblyPrefix}.{embeddedTexture2dName}";
            Content.LoaderManager.Self.AddDisposable($"EmbeddedResource.{resourceName}", texture,
                // Mirrors MonoGame's LoadEmbeddedTexture2d: allows unit tests, and can avoid confusing errors
                Content.LoaderManager.ExistingContentBehavior.Replace);

            return texture;
        }
    }
}
