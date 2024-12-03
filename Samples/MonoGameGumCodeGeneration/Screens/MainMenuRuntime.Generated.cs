//Code for MainMenu
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using MonoGameGumCodeGeneration.Components;
using System.Linq;
namespace MonoGameGumCodeGeneration.Screens
{
    public partial class MainMenuRuntime
    {
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("MainMenu", typeof(MainMenuRuntime));
        }
        public PopupRuntime PopupInstance { get; protected set; }
        public ComponentWithStatesRuntime ComponentWithStatesInstance { get; protected set; }
        public TextRuntime TextWithLotsOfPropertiesSet { get; protected set; }
        public PolygonRuntime PolygonInstance { get; protected set; }

        public MainMenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {

                 

                InitializeInstances();

                ApplyDefaultVariables();
                AssignParents();
                CustomInitialize();
            }
        }
        protected virtual void InitializeInstances()
        {
            PopupInstance = new PopupRuntime();
            PopupInstance.Name = "PopupInstance";
            ComponentWithStatesInstance = new ComponentWithStatesRuntime();
            ComponentWithStatesInstance.Name = "ComponentWithStatesInstance";
            TextWithLotsOfPropertiesSet = new TextRuntime();
            TextWithLotsOfPropertiesSet.Name = "TextWithLotsOfPropertiesSet";
            PolygonInstance = new PolygonRuntime();
            PolygonInstance.Name = "PolygonInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(PopupInstance);
            this.Children.Add(ComponentWithStatesInstance);
            this.Children.Add(TextWithLotsOfPropertiesSet);
            this.Children.Add(PolygonInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.PopupInstance.X = 2f;
            this.PopupInstance.Y = -10f;

            this.ComponentWithStatesInstance.X = 85f;
            this.ComponentWithStatesInstance.Y = 65f;

            this.TextWithLotsOfPropertiesSet.Blue = 100;
            this.TextWithLotsOfPropertiesSet.FontSize = 24;
            this.TextWithLotsOfPropertiesSet.Green = 150;
            this.TextWithLotsOfPropertiesSet.Height = 10f;
            this.TextWithLotsOfPropertiesSet.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextWithLotsOfPropertiesSet.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextWithLotsOfPropertiesSet.LineHeightMultiplier = 1.2f;
            this.TextWithLotsOfPropertiesSet.Red = 200;
            this.TextWithLotsOfPropertiesSet.Text = "I am a Text that has lots of properties set. I am testing all of the different properties that might be assigned on a Text object.";
            this.TextWithLotsOfPropertiesSet.TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.TruncateWord;
            this.TextWithLotsOfPropertiesSet.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.SpillOver;
            this.TextWithLotsOfPropertiesSet.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextWithLotsOfPropertiesSet.Visible = true;
            this.TextWithLotsOfPropertiesSet.Width = 196f;
            this.TextWithLotsOfPropertiesSet.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TextWithLotsOfPropertiesSet.X = 32f;
            this.TextWithLotsOfPropertiesSet.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextWithLotsOfPropertiesSet.XUnits = GeneralUnitType.PixelsFromSmall;
            this.TextWithLotsOfPropertiesSet.Y = 240f;
            this.TextWithLotsOfPropertiesSet.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextWithLotsOfPropertiesSet.YUnits = GeneralUnitType.PixelsFromSmall;

            this.PolygonInstance.X = 545f;
            this.PolygonInstance.Y = 55f;
            this.PolygonInstance.SetPoints(new System.Numerics.Vector2[]{
                new System.Numerics.Vector2(-32f, -32f),
                new System.Numerics.Vector2(32f, -32f),
                new System.Numerics.Vector2(108f, 71f),
                new System.Numerics.Vector2(-88f, 86f),
                new System.Numerics.Vector2(-83f, 13f),
                new System.Numerics.Vector2(-32f, -32f),
            });

        }
        partial void CustomInitialize();
    }
}
