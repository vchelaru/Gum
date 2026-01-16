using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using SkiaGum;
using SkiaGum.Content;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaPlugin.Managers;
using System;

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
                asText.BoldWeight = textRuntime.IsBold ? 1.5f : 1.0f;
                asText.FontSize = textRuntime.FontSize;
            }
        }

        private void RegisterComponentRuntimeInstantiations()
        {
            ElementSaveExtensions.RegisterGueInstantiation(
                "Arc",
                () => new ArcRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Circle",
                () => new CircleRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "ColoredCircle",
                () => new ColoredCircleRuntime());


            ElementSaveExtensions.RegisterGueInstantiation(
                "Container",
                () => new ContainerRuntime());

            //ElementSaveExtensions.RegisterGueInstantiation(
            //    "NineSlice",
            //    () => new NineSliceRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Polygon",
                () => new PolygonRuntime());

            //ElementSaveExtensions.RegisterGueInstantiation(
            //    "Rectangle",
            //    () => new RectangleRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Sprite",
                () => new SpriteRuntime());

            ElementSaveExtensions.RegisterGueInstantiation(
                "Text",
                () => new TextRuntime());
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

        public void InvalidateSurface()
        {

        }

        public void Draw()
        {
            Renderer.Draw(this);
        }
    }
}
