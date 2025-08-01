using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Styling = Gum.Forms.DefaultVisuals.Styling;


namespace MonoGameGum.Forms.DefaultVisuals
{
    public abstract class DefaultTextBoxBaseRuntime : InteractiveGue
    {
        public TextRuntime TextInstance { get; private set; }

        public ColoredRectangleRuntime CaretInstance { get; private set; } 

        protected abstract string CategoryName { get; }

        public DefaultTextBoxBaseRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.Width = 100;
                this.Height = 24;
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
                CaretInstance = new ColoredRectangleRuntime();
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
                Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

                SelectionInstance.Color = new Microsoft.Xna.Framework.Color(140, 48, 138);
                SelectionInstance.Height = -4f;
                SelectionInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                SelectionInstance.Width = 7f;
                SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                SelectionInstance.X = 15f;
                SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                SelectionInstance.Y = 0f;

                TextInstance.Height = -4f;
                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
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
                TextInstance.VerticalAlignment = VerticalAlignment.Center;


                PlaceholderTextInstance.Red = 128;
                PlaceholderTextInstance.Blue = 128;
                PlaceholderTextInstance.Green = 128;
                PlaceholderTextInstance.Height = -4f;
                PlaceholderTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                PlaceholderTextInstance.Text = "Text Placeholder";
                PlaceholderTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                PlaceholderTextInstance.Width = -8f;
                PlaceholderTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                PlaceholderTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                PlaceholderTextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                PlaceholderTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                PlaceholderTextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                PlaceholderTextInstance.VerticalAlignment = VerticalAlignment.Center;

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


                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    textboxCategory.States.Add(state);
                    currentState = state;
                }

                void AddVariable(string name, object value)
                {
                    currentState.Variables.Add(new VariableSave
                    {
                        Name = name,
                        Value = value
                    });
                }

                AddState(FrameworkElement.EnabledStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("Background.Color", Styling.ActiveStyle.Colors.DarkGray);

                AddState(FrameworkElement.DisabledStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.Gray);
                AddVariable("Background.Color", Styling.ActiveStyle.Colors.DarkGray);

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("Background.Color", Styling.ActiveStyle.Colors.Gray);

                // todo - this is using the wrong state name. It should be Focused,
                // but the TextBoxBase expects Selected. This will change in future 
                // versions of Gum...
                AddState(FrameworkElement.FocusedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("Background.Color", Styling.ActiveStyle.Colors.DarkGray);


                var lineModeCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                lineModeCategory.Name = "LineModeCategory";
                this.AddCategory(lineModeCategory);
                var singleLineState = new StateSave()
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
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToParent
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
                        },
                        new ()
                        {
                            Name = "PlaceholderTextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Center
                        },
                        new ()
                        {
                            Name = "TextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Center
                        }

                    }
                };

                lineModeCategory.States.Add(singleLineState);

                var multiLineState = new StateSave()
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
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToParent
                        },
                        new ()
                        {
                            Name = "PlaceholderTextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Top
                        },
                        new ()
                        {
                            Name = "TextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Top
                        }
                    }
                };
                lineModeCategory.States.Add(multiLineState);

                var multiLineNoWrapState = new StateSave()
                {
                    Name = "MultiNoWrap",
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
                            Value = 0f
                        },
                        new()
                        {
                            Name = "TextInstance.WidthUnits",
                            Value = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren
                        },
                        new ()
                        {
                            Name = "PlaceholderTextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Top
                        },
                        new ()
                        {
                            Name = "TextInstance.VerticalAlignment",
                            Value = VerticalAlignment.Top
                        }
                    }
                };
                lineModeCategory.States.Add(multiLineNoWrapState);
            }



        }

    }
}
