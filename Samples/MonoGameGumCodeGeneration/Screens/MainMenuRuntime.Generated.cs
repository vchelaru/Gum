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
        public PopupRuntime PopupInstance { get; protected set; }
        public ComponentWithStatesRuntime ComponentWithStatesInstance { get; protected set; }
        public TextRuntime TextWithLotsOfPropertiesSet { get; protected set; }
        public PolygonRuntime PolygonInstance { get; protected set; }
        public CircleRuntime CircleInstance { get; protected set; }
        public PolygonRuntime PolygonInstance1 { get; protected set; }
        public RectangleRuntime RectangleInstance { get; protected set; }

        public MainMenuRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {

                 

                InitializeInstances();

                if(fullInstantiation)
                {
                    ApplyDefaultVariables();
                }
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
            CircleInstance = new CircleRuntime();
            CircleInstance.Name = "CircleInstance";
            PolygonInstance1 = new PolygonRuntime();
            PolygonInstance1.Name = "PolygonInstance1";
            RectangleInstance = new RectangleRuntime();
            RectangleInstance.Name = "RectangleInstance";
        }
        protected virtual void AssignParents()
        {
            this.WhatThisContains.Add(PopupInstance);
            this.WhatThisContains.Add(ComponentWithStatesInstance);
            this.WhatThisContains.Add(TextWithLotsOfPropertiesSet);
            this.WhatThisContains.Add(PolygonInstance);
            this.WhatThisContains.Add(CircleInstance);
            this.WhatThisContains.Add(PolygonInstance1);
            this.WhatThisContains.Add(RectangleInstance);
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

            this.CircleInstance.Alpha = 255;
            this.CircleInstance.Blue = 255;
            this.CircleInstance.Green = 0;
            this.CircleInstance.Radius = 24f;
            this.CircleInstance.Red = 128;
            this.CircleInstance.Rotation = 0f;
            this.CircleInstance.Visible = true;
            this.CircleInstance.X = 664f;
            this.CircleInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.CircleInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.CircleInstance.Y = 220f;
            this.CircleInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.CircleInstance.YUnits = GeneralUnitType.PixelsFromSmall;

            this.PolygonInstance1.Blue = 0;
            this.PolygonInstance1.Green = 255;
            this.PolygonInstance1.Red = 255;
            this.PolygonInstance1.Rotation = 0f;
            this.PolygonInstance1.X = 623f;
            this.PolygonInstance1.XUnits = GeneralUnitType.PixelsFromSmall;
            this.PolygonInstance1.Y = 317f;

            this.RectangleInstance.Blue = 219;
            this.RectangleInstance.Green = 168;
            this.RectangleInstance.Height = 94f;
            this.RectangleInstance.Red = 74;
            this.RectangleInstance.Rotation = 0f;
            this.RectangleInstance.Width = 70f;
            this.RectangleInstance.X = 677f;
            this.RectangleInstance.Y = 408f;

        }
        partial void CustomInitialize();
    }
}
