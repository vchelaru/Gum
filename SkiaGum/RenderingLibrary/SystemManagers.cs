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

            StandardElementsManager.Self.Initialize();
            StandardElementsManager.Self.CustomGetDefaultState += HandleCustomGetDefaultState;
            ElementSaveExtensions.CustomCreateGraphicalComponentFunc = HandleCreateGraphicalComponent;
            GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            GraphicalUiElement.UpdateFontFromProperties = UpdateFonts;

            GraphicalUiElement.AddRenderableToManagers = AddRenderableToManagers;
            SkiaResourceManager.Initialize(null);
            LoaderManager.Self.ContentLoader = new EmbeddedResourceContentLoader();
            RegisterComponentRuntimeInstantiations();

        }

        private void UpdateFonts(IText text, GraphicalUiElement element)
        {
            if(text is Text asText && element is TextRuntime textRuntime)
            {
                asText.FontName = textRuntime.Font ?? "Arial";
                asText.IsItalic = textRuntime.IsItalic;
                asText.BoldWeight = textRuntime.IsBold ? 700 : 400;
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

        private IRenderable HandleCreateGraphicalComponent(string type, ISystemManagers managers)
        {
            switch (type)
            {
                case "Arc": return new Arc();
                case "Canvas": return new CanvasRenderable();
                case "Circle": return new Circle();
                case "ColoredCircle": return new Circle();
                case "ColoredRectangle": return new SolidRectangle();
                case "LottieAnimation": return new LottieAnimation();
                case "NineSlice": return new NineSlice();
                case "Polygon": return new Polygon();
                case "RoundedRectangle": return new RoundedRectangle();
                case "Svg": return new VectorSprite();
                case "Sprite": return new Sprite();
                case "Text": return new Text();
            }

            return null;
        }

        private StateSave HandleCustomGetDefaultState(string arg)
        {
            switch(arg)
            {
                case "Arc":
                    return StandardElementsManager.GetArcState();
                case "Canvas":
                    return DefaultStateManager.GetCanvasState();
                case "ColoredCircle":
                    return StandardElementsManager.GetColoredCircleState();
                case "LottieAnimation":
                    return DefaultStateManager.GetLottieAnimationState();
                case "RoundedRectangle":
                    return StandardElementsManager.GetRoundedRectangleState();
                case "Svg":
                    return DefaultStateManager.GetSvgState();
            }
            return null;
        }

        private void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers managers, Layer layer)
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

        public void InvalidateSurface()
        {

        }

        public void Draw()
        {
            Renderer.Draw(this);
        }
    }
}
