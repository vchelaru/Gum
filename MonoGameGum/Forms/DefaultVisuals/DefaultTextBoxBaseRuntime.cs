using Gum.Converters;
using Gum.Wireframe;
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
    public abstract class DefaultTextBoxBaseRuntime : InteractiveGue
    {
        TextRuntime TextInstance;
        protected abstract string CategoryName { get; }

        public DefaultTextBoxBaseRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.ClipsChildren = true;

                var Background = new ColoredRectangleRuntime();
                Background.Name = "Background";
                var SelectionInstance = new ColoredRectangleRuntime();
                SelectionInstance.Name = "SelectionInstance";
                
                TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                var PlaceholderTextInstance = new TextRuntime();
                PlaceholderTextInstance.Name = "PlaceholderTextInstance";
                var FocusedIndicator = new ColoredRectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                var CaretInstance = new ColoredRectangleRuntime();
                CaretInstance.Name = "CaretInstance";

                this.Children.Add(Background);
                this.Children.Add(SelectionInstance);
                this.Children.Add(TextInstance);
                this.Children.Add(PlaceholderTextInstance);
                this.Children.Add(FocusedIndicator);
                this.Children.Add(CaretInstance);

                Background.Color = new Microsoft.Xna.Framework.Color(70, 70, 70);
                Background.Width = 0;
                Background.Height = 0;
                Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;

                SelectionInstance.Color = new Microsoft.Xna.Framework.Color(140, 48, 138);
                SelectionInstance.Height = -4f;
                SelectionInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                SelectionInstance.Width = 7f;
                SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                SelectionInstance.X = 15f;
                SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                SelectionInstance.Y = 0f;

                TextInstance.Height = -4f;
                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                TextInstance.Text = "";
                TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                TextInstance.Width = 0f;
                TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                TextInstance.X = 4f;
                TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                TextInstance.Y = 0f;
                TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                PlaceholderTextInstance.Red = 128;
                PlaceholderTextInstance.Blue = 128;
                PlaceholderTextInstance.Green = 128;
                PlaceholderTextInstance.Height = -4f;
                PlaceholderTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                PlaceholderTextInstance.Text = "Text Placeholder";
                PlaceholderTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                PlaceholderTextInstance.Width = -8f;
                PlaceholderTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                PlaceholderTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                PlaceholderTextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                PlaceholderTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                PlaceholderTextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                FocusedIndicator.Height = 2f;
                FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                FocusedIndicator.Visible = false;
                FocusedIndicator.Y = 2f;
                FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

                CaretInstance.Color = new Microsoft.Xna.Framework.Color(6, 159, 177);
                CaretInstance.Height = 18f;
                CaretInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
                CaretInstance.TextureHeight = 24;
                CaretInstance.TextureLeft = 0;
                CaretInstance.TextureTop = 48;
                CaretInstance.TextureWidth = 24;
                CaretInstance.Width = 1f;
                CaretInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                CaretInstance.X = 4f;
                CaretInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                CaretInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                CaretInstance.Y = 0f;
                CaretInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                CaretInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                var textboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                textboxCategory.Name = CategoryName;
                this.AddCategory(textboxCategory);
                textboxCategory.States.Add(new()
                {
                    Name = "Enabled",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "TextInstance.Color",
                            Value = new Microsoft.Xna.Framework.Color(255,255,255),
                        },
                        new ()
                        {
                            Name = "Background.Color",
                            Value = new Microsoft.Xna.Framework.Color(70,70,70),
                        }
                    }
                });

                textboxCategory.States.Add(new()
                {
                    Name = "Disabled",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "TextInstance.Color",
                            Value = new Microsoft.Xna.Framework.Color(128,128,128),
                        },
                        new ()
                        {
                            Name = "Background.Color",
                            Value = new Microsoft.Xna.Framework.Color(70,70,70),
                        }
                    }
                });

                textboxCategory.States.Add(new()
                {
                    Name = "Highlighted",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "TextInstance.Color",
                            Value = new Microsoft.Xna.Framework.Color(255,255,255),
                        },
                        new ()
                        {
                            Name = "Background.Color",
                            Value = new Microsoft.Xna.Framework.Color(130,130,130),
                        }
                    }
                });

                textboxCategory.States.Add(new()
                {
                    Name = "Selected",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "TextInstance.Color",
                            Value = new Microsoft.Xna.Framework.Color(255,255,255),
                        },
                        new ()
                        {
                            Name = "Background.Color",
                            Value = new Microsoft.Xna.Framework.Color(130,130,130),
                        }
                    }
                });


                var lineModeCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                lineModeCategory.Name = "LineModeCategory";
                this.AddCategory(lineModeCategory);
                lineModeCategory.States.Add(new()
                {
                    Name = "Single",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "SelectionInstance.Height",
                            Value = -4f
                        },
                        new ()
                        {
                            Name = "SelectionInstance.HeightUnits",
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer
                        },
                        new()
                        {
                            Name = "TextInstance.Width",
                            Value = 0f
                        },
                        new()
                        {
                            Name = "TextInstance.WidthUnits",
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren
                        }
                    }
                });

                lineModeCategory.States.Add(new()
                {
                    Name = "Multi",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "SelectionInstance.Height",
                            Value = 20f
                        },
                        new ()
                        {
                            Name = "SelectionInstance.HeightUnits",
                            Value = global::Gum.DataTypes.DimensionUnitType.Absolute
                        },
                        new()
                        {
                            Name = "TextInstance.Width",
                            Value = -8f
                        },
                        new()
                        {
                            Name = "TextInstance.WidthUnits",
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer
                        }
                    }
                });
            }


        }

    }
}
