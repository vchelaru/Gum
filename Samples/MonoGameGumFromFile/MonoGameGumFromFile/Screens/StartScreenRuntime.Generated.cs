//Code for StartScreen
using GumRuntime;
using MonoGameGumFromFile.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using MonoGameGumFromFile.Components;
namespace MonoGameGumFromFile.Screens
{
    public partial class StartScreenRuntime
    {
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime TextInstance1 { get; protected set; }
        public TextRuntime TextInstance2 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
        public CircleRuntime CircleInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public TextRuntime TextInstance3 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance1 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance2 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance3 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance4 { get; protected set; }
        public TextRuntime TextInstance7 { get; protected set; }
        public TextRuntime TextInstance11 { get; protected set; }
        public SpriteRuntime SpriteInstance1 { get; protected set; }
        public SpriteRuntime SpriteInstance2 { get; protected set; }
        public SpriteRuntime AnimatedSprite { get; protected set; }
        public TextRuntime TextInstance10 { get; protected set; }
        public TextRuntime TextInstance8 { get; protected set; }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public TextRuntime TextInstance4 { get; protected set; }
        public PolygonRuntime PolygonInstance { get; protected set; }
        public TextRuntime TextInstance5 { get; protected set; }
        public RectangleRuntime RectangleInstance { get; protected set; }
        public TextRuntime TextInstance6 { get; protected set; }
        public SpriteRuntime SpriteInstance { get; protected set; }
        public ContainerRuntime ContainerInstance1 { get; protected set; }
        public TextRuntime TextInstance9 { get; protected set; }
        public ComponentWithExposedVariableRuntime ComponentWithExposedVariableInstance { get; protected set; }

        public StartScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            TextInstance1 = new TextRuntime();
            TextInstance1.Name = "TextInstance1";
            TextInstance2 = new TextRuntime();
            TextInstance2.Name = "TextInstance2";
            ColoredRectangleInstance = new ColoredRectangleRuntime();
            ColoredRectangleInstance.Name = "ColoredRectangleInstance";
            CircleInstance = new CircleRuntime();
            CircleInstance.Name = "CircleInstance";
            ContainerInstance = new ContainerRuntime();
            ContainerInstance.Name = "ContainerInstance";
            TextInstance3 = new TextRuntime();
            TextInstance3.Name = "TextInstance3";
            ColoredRectangleInstance1 = new ColoredRectangleRuntime();
            ColoredRectangleInstance1.Name = "ColoredRectangleInstance1";
            ColoredRectangleInstance2 = new ColoredRectangleRuntime();
            ColoredRectangleInstance2.Name = "ColoredRectangleInstance2";
            ColoredRectangleInstance3 = new ColoredRectangleRuntime();
            ColoredRectangleInstance3.Name = "ColoredRectangleInstance3";
            ColoredRectangleInstance4 = new ColoredRectangleRuntime();
            ColoredRectangleInstance4.Name = "ColoredRectangleInstance4";
            TextInstance7 = new TextRuntime();
            TextInstance7.Name = "TextInstance7";
            TextInstance11 = new TextRuntime();
            TextInstance11.Name = "TextInstance11";
            SpriteInstance1 = new SpriteRuntime();
            SpriteInstance1.Name = "SpriteInstance1";
            SpriteInstance2 = new SpriteRuntime();
            SpriteInstance2.Name = "SpriteInstance2";
            AnimatedSprite = new SpriteRuntime();
            AnimatedSprite.Name = "AnimatedSprite";
            TextInstance10 = new TextRuntime();
            TextInstance10.Name = "TextInstance10";
            TextInstance8 = new TextRuntime();
            TextInstance8.Name = "TextInstance8";
            NineSliceInstance = new NineSliceRuntime();
            NineSliceInstance.Name = "NineSliceInstance";
            TextInstance4 = new TextRuntime();
            TextInstance4.Name = "TextInstance4";
            PolygonInstance = new PolygonRuntime();
            PolygonInstance.Name = "PolygonInstance";
            TextInstance5 = new TextRuntime();
            TextInstance5.Name = "TextInstance5";
            RectangleInstance = new RectangleRuntime();
            RectangleInstance.Name = "RectangleInstance";
            TextInstance6 = new TextRuntime();
            TextInstance6.Name = "TextInstance6";
            SpriteInstance = new SpriteRuntime();
            SpriteInstance.Name = "SpriteInstance";
            ContainerInstance1 = new ContainerRuntime();
            ContainerInstance1.Name = "ContainerInstance1";
            TextInstance9 = new TextRuntime();
            TextInstance9.Name = "TextInstance9";
            ComponentWithExposedVariableInstance = new ComponentWithExposedVariableRuntime();
            ComponentWithExposedVariableInstance.Name = "ComponentWithExposedVariableInstance";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(TextInstance);
            else this.WhatThisContains.Add(TextInstance);
            ColoredRectangleInstance.Children.Add(TextInstance1);
            if(this.Children != null) this.Children.Add(TextInstance2);
            else this.WhatThisContains.Add(TextInstance2);
            if(this.Children != null) this.Children.Add(ColoredRectangleInstance);
            else this.WhatThisContains.Add(ColoredRectangleInstance);
            if(this.Children != null) this.Children.Add(CircleInstance);
            else this.WhatThisContains.Add(CircleInstance);
            if(this.Children != null) this.Children.Add(ContainerInstance);
            else this.WhatThisContains.Add(ContainerInstance);
            ContainerInstance.Children.Add(TextInstance3);
            ContainerInstance.Children.Add(ColoredRectangleInstance1);
            ContainerInstance.Children.Add(ColoredRectangleInstance2);
            ContainerInstance.Children.Add(ColoredRectangleInstance3);
            ContainerInstance.Children.Add(ColoredRectangleInstance4);
            if(this.Children != null) this.Children.Add(TextInstance7);
            else this.WhatThisContains.Add(TextInstance7);
            if(this.Children != null) this.Children.Add(TextInstance11);
            else this.WhatThisContains.Add(TextInstance11);
            if(this.Children != null) this.Children.Add(SpriteInstance1);
            else this.WhatThisContains.Add(SpriteInstance1);
            if(this.Children != null) this.Children.Add(SpriteInstance2);
            else this.WhatThisContains.Add(SpriteInstance2);
            if(this.Children != null) this.Children.Add(AnimatedSprite);
            else this.WhatThisContains.Add(AnimatedSprite);
            if(this.Children != null) this.Children.Add(TextInstance10);
            else this.WhatThisContains.Add(TextInstance10);
            if(this.Children != null) this.Children.Add(TextInstance8);
            else this.WhatThisContains.Add(TextInstance8);
            if(this.Children != null) this.Children.Add(NineSliceInstance);
            else this.WhatThisContains.Add(NineSliceInstance);
            NineSliceInstance.Children.Add(TextInstance4);
            if(this.Children != null) this.Children.Add(PolygonInstance);
            else this.WhatThisContains.Add(PolygonInstance);
            PolygonInstance.Children.Add(TextInstance5);
            if(this.Children != null) this.Children.Add(RectangleInstance);
            else this.WhatThisContains.Add(RectangleInstance);
            RectangleInstance.Children.Add(TextInstance6);
            if(this.Children != null) this.Children.Add(SpriteInstance);
            else this.WhatThisContains.Add(SpriteInstance);
            if(this.Children != null) this.Children.Add(ContainerInstance1);
            else this.WhatThisContains.Add(ContainerInstance1);
            ContainerInstance1.Children.Add(TextInstance9);
            if(this.Children != null) this.Children.Add(ComponentWithExposedVariableInstance);
            else this.WhatThisContains.Add(ComponentWithExposedVariableInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.TextInstance.Height = 0f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.Text = @"This is some regular text";
            this.TextInstance.Width = 0f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            this.TextInstance1.Height = 0f;
            this.TextInstance1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance1.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance1.Text = @"This rectangle has a Text child, and the text child wraps to stay inside the rectangle.";
            this.TextInstance1.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance1.Width = 0f;
            this.TextInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance1.X = 0f;
            this.TextInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance1.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance1.Y = 0f;
            this.TextInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance1.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.TextInstance2.Height = 0f;
            this.TextInstance2.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance2.Text = @"This is a regular (line) circle:";
            this.TextInstance2.Width = 0f;
            this.TextInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance2.X = 3f;
            this.TextInstance2.Y = 177f;

            this.ColoredRectangleInstance.Blue = 139;
            this.ColoredRectangleInstance.Green = 20;
            this.ColoredRectangleInstance.Height = 106f;
            this.ColoredRectangleInstance.Red = 0;
            this.ColoredRectangleInstance.Width = 254f;
            this.ColoredRectangleInstance.X = 25f;
            this.ColoredRectangleInstance.Y = 39f;

            this.CircleInstance.Height = 104f;
            this.CircleInstance.Radius = 52f;
            this.CircleInstance.Width = 55f;
            this.CircleInstance.X = 23f;
            this.CircleInstance.Y = 205f;

            this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ContainerInstance.Height = 245f;
            this.ContainerInstance.StackSpacing = 4f;
            this.ContainerInstance.Width = 97f;
            this.ContainerInstance.X = 294f;
            this.ContainerInstance.Y = 13f;

            this.TextInstance3.Height = 0f;
            this.TextInstance3.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance3.Text = @"Containers can stack children:";
            this.TextInstance3.Width = 0f;
            this.TextInstance3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            this.ColoredRectangleInstance1.Blue = 13;
            this.ColoredRectangleInstance1.Green = 0;
            this.ColoredRectangleInstance1.Red = 255;

            this.ColoredRectangleInstance2.Blue = 255;
            this.ColoredRectangleInstance2.Green = 40;
            this.ColoredRectangleInstance2.Red = 0;

            this.ColoredRectangleInstance3.Blue = 20;
            this.ColoredRectangleInstance3.Green = 255;
            this.ColoredRectangleInstance3.Red = 0;

            this.ColoredRectangleInstance4.Blue = 0;
            this.ColoredRectangleInstance4.Green = 255;
            this.ColoredRectangleInstance4.Red = 246;

            this.TextInstance7.Height = 0f;
            this.TextInstance7.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance7.Text = @"Draw a sprite using its entire texture or part of it:";
            this.TextInstance7.Width = 0f;
            this.TextInstance7.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance7.X = 9f;
            this.TextInstance7.Y = 411f;

            this.TextInstance11.Height = 24f;
            this.TextInstance11.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance11.Text = @"Use [IsBold=true]BBCode[/IsBold] to perform [Color=Red]inline styling[/Color] of your Text. [FontScale=2]How cool![/FontScale]";
            this.TextInstance11.Width = 89f;
            this.TextInstance11.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance11.X = 22f;
            this.TextInstance11.Y = 645f;

            this.SpriteInstance1.SourceFileName = @"bear.png";
            this.SpriteInstance1.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.SpriteInstance1.TextureHeight = 17;
            this.SpriteInstance1.TextureLeft = 12;
            this.SpriteInstance1.TextureTop = 4;
            this.SpriteInstance1.TextureWidth = 20;
            this.SpriteInstance1.X = 68f;
            this.SpriteInstance1.Y = 449f;

            this.SpriteInstance2.Height = 300f;
            this.SpriteInstance2.SourceFileName = @"bear.png";
            this.SpriteInstance2.Width = 300f;
            this.SpriteInstance2.X = 96f;
            this.SpriteInstance2.Y = 462f;

            this.AnimatedSprite.Animate = true;
            this.AnimatedSprite.CurrentChainName = @"IdleRight";
            this.AnimatedSprite.Height = 300f;
            this.AnimatedSprite.SourceFileName = @"CharacterAnimations.achx";
            this.AnimatedSprite.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.AnimatedSprite.Width = 300f;
            this.AnimatedSprite.X = 229f;
            this.AnimatedSprite.Y = 483f;

            this.TextInstance10.Height = 0f;
            this.TextInstance10.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance10.Text = @"Animate it with .achx files:";
            this.TextInstance10.Width = 80f;
            this.TextInstance10.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TextInstance10.X = 231f;
            this.TextInstance10.Y = 446f;

            this.TextInstance8.Height = 0f;
            this.TextInstance8.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance8.Text = @"Scale it up to see it larger:";
            this.TextInstance8.Width = 80f;
            this.TextInstance8.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TextInstance8.X = 10f;
            this.TextInstance8.Y = 490f;

            this.NineSliceInstance.Height = 210f;
            this.NineSliceInstance.SourceFileName = @"metalpanel_blue.png";
            this.NineSliceInstance.Width = 186f;
            this.NineSliceInstance.X = 603f;
            this.NineSliceInstance.Y = 10f;

            this.TextInstance4.Blue = 0;
            this.TextInstance4.Green = 0;
            this.TextInstance4.Height = -72f;
            this.TextInstance4.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance4.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance4.Red = 0;
            this.TextInstance4.Text = @"NineSlices are great for UI since they stretch the edges and middle, but keep corners drawn at their normal size.";
            this.TextInstance4.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance4.Width = -28f;
            this.TextInstance4.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance4.X = 0f;
            this.TextInstance4.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance4.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance4.Y = 0f;
            this.TextInstance4.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance4.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.PolygonInstance.X = 671f;
            this.PolygonInstance.Y = 316f;
            this.PolygonInstance.SetPoints(new System.Numerics.Vector2[]{
                new System.Numerics.Vector2(-71f, -69f),
                new System.Numerics.Vector2(73f, -82f),
                new System.Numerics.Vector2(137.5f, -11.5f),
                new System.Numerics.Vector2(91f, 72f),
                new System.Numerics.Vector2(-73f, 78f),
                new System.Numerics.Vector2(-71f, -69f),
            });

            this.TextInstance5.Height = 111f;
            this.TextInstance5.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance5.Text = @"This is a Polygon";
            this.TextInstance5.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance5.Width = 137f;
            this.TextInstance5.X = -49f;
            this.TextInstance5.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance5.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance5.Y = -52f;
            this.TextInstance5.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance5.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.RectangleInstance.Height = 102f;
            this.RectangleInstance.Width = 184f;
            this.RectangleInstance.X = 405f;
            this.RectangleInstance.Y = 112f;

            this.TextInstance6.Height = 0f;
            this.TextInstance6.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance6.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance6.Text = @"Line rectangles can draw one-pixel boundaries around objects.";
            this.TextInstance6.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance6.Width = 0f;
            this.TextInstance6.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance6.X = 0f;
            this.TextInstance6.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance6.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance6.Y = 0f;
            this.TextInstance6.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance6.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.SpriteInstance.SourceFileName = @"bear.png";
            this.SpriteInstance.X = 10f;
            this.SpriteInstance.Y = 439f;

            this.ContainerInstance1.ClipsChildren = true;
            this.ContainerInstance1.Height = 38f;
            this.ContainerInstance1.Width = 116f;
            this.ContainerInstance1.X = 425f;
            this.ContainerInstance1.Y = 61f;

            this.TextInstance9.Height = 0f;
            this.TextInstance9.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance9.Text = @"This text is clipped";
            this.TextInstance9.Width = 0f;
            this.TextInstance9.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            this.ComponentWithExposedVariableInstance.Height = 41f;
            this.ComponentWithExposedVariableInstance.Text = @"Set me in code";
            this.ComponentWithExposedVariableInstance.Width = 162f;
            this.ComponentWithExposedVariableInstance.X = 170f;
            this.ComponentWithExposedVariableInstance.Y = 272f;

        }
        partial void CustomInitialize();
    }
}
