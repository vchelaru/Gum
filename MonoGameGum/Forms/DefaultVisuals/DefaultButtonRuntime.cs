using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultButtonRuntime : InteractiveGue
    {
        public TextRuntime TextInstance { get; private set; }

        public RectangleRuntime FocusedIndicator { get; private set; }

        internal static Color EnabledbuttonColor = new Color(0, 0, 128);
        internal static Color HighlightedButtonColor = new Color(0, 0, 160);
        internal static Color PushedButtonColor = new Color(0, 0, 96);
        internal static Color DisabledButtonColor = new Color(48, 48, 64);

        public DefaultButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if(fullInstantiation)
            {
                this.Width = 128;
                this.Height = 32;

                var background = new ColoredRectangleRuntime();
                background.Width = 0;
                background.Height = 0;
                background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.Name = "ButtonBackground";
                this.Children.Add(background);

                TextInstance = new TextRuntime();
                TextInstance.X = 0;
                TextInstance.Y = 0;
                TextInstance.Width = 0;
                TextInstance.Height = 0;
                TextInstance.Name = "TextInstance";
                TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
                this.Children.Add(TextInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.X = 0;
                FocusedIndicator.Y = 0;
                FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.XOrigin = HorizontalAlignment.Center;
                FocusedIndicator.YOrigin = VerticalAlignment.Center;
                FocusedIndicator.Width = -4;
                FocusedIndicator.Height = -4;
                FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.Color = Color.White;
                FocusedIndicator.Visible = false;
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                buttonCategory.Name = "ButtonCategory";

                buttonCategory.States.Add(new ()
                {
                    Name = FrameworkElement.EnabledState,
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = EnabledbuttonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = false,
                        },
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.FocusedState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = EnabledbuttonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = true,
                        },
                    }
                });

                buttonCategory.States.Add(new ()
                {
                    Name = FrameworkElement.HighlightedState,
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = HighlightedButtonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = false,
                        },
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.HighlightedFocusedState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = HighlightedButtonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = true,
                        },
                    }
                });

                buttonCategory.States.Add(new ()
                {
                    Name = FrameworkElement.PushedState,
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = PushedButtonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = false,
                        },
                    }
                });

                buttonCategory.States.Add(new ()
                {
                    Name = FrameworkElement.DisabledState,
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = DisabledButtonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = false,
                        },
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.DisabledFocusedState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = DisabledButtonColor,
                        },
                        new ()
                        {
                            Name = "FocusedIndicator.Visible",
                            Value = true,
                        },
                    }
                });

                this.AddCategory(buttonCategory);
            }

            if(tryCreateFormsObject)
            {
                FormsControlAsObject = new Button(this);
            }

        }

        public Button FormsControl => FormsControlAsObject as Button;
    }
}
