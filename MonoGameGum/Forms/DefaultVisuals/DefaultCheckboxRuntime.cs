using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultCheckboxRuntime : InteractiveGue
    {
        public DefaultCheckboxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {


                this.Height = 32;
                this.Width = 128;

                var checkboxBackground = new ColoredRectangleRuntime();
                checkboxBackground.Width = 24;
                checkboxBackground.Height = 24;
                checkboxBackground.Color = new Color(41, 55, 52);
                checkboxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                checkboxBackground.YOrigin = VerticalAlignment.Center;
                checkboxBackground.Name = "CheckBoxBackground";
                this.Children.Add(checkboxBackground);

                var innerCheck = new ColoredRectangleRuntime();
                innerCheck.Width = 12;
                innerCheck.Height = 12;
                innerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                innerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                innerCheck.XOrigin = HorizontalAlignment.Center;
                innerCheck.YOrigin = VerticalAlignment.Center;
                innerCheck.Name = "InnerCheck";
                innerCheck.Color = new Color(128, 255, 0);

                checkboxBackground.Children.Add(innerCheck);

                var text = new TextRuntime();
                text.X = 28;
                text.Y = 0;
                text.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
                text.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                text.Width = -36;
                text.Height = 0;
                text.Name = "TextInstance";
                text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                this.Children.Add(text);


                var checkboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                checkboxCategory.Name = "CheckBoxCategory";
                checkboxCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
                {
                    Name = "EnabledOn",
                    Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Name = "InnerCheck.Visible",
                            Value = true,
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Name = "CheckBoxBackground.Color",
                            Value = new Color(41, 55, 52),
                        }
                    }
                });
                checkboxCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
                {
                    Name = "EnabledOff",
                    Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Name = "InnerCheck.Visible",
                            Value = false,
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Name = "CheckBoxBackground.Color",
                            Value = new Color(41, 55, 52),
                        }
                    }
                });

                checkboxCategory.States.Add(new ()
                {
                    Name = "HighlightedOn",
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "InnerCheck.Visible",
                            Value = true,
                        },
                        new ()
                        {
                            Name = "CheckBoxBackground.Color",
                            Value = new Color(51, 65, 62),
                        }
                    }
                });
                checkboxCategory.States.Add(new ()
                {
                    Name = "HighlightedOff",
                    Variables = new ()
                    {
                        new ()
                        {
                            Name = "InnerCheck.Visible",
                            Value = false,
                        },
                        new ()
                        {
                            Name = "CheckBoxBackground.Color",
                            Value = new Color(61, 65, 62),
                        }
                    }
                });

                this.AddCategory(checkboxCategory);
            }

            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new CheckBox(this);
            }
        }

        public CheckBox FormsControl => FormsControlAsObject as CheckBox;
    }
}
