using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;


namespace MonoGameGum.Forms.DefaultVisuals
{
    public abstract class TextBoxBaseVisual : InteractiveGue
    {
        public NineSliceRuntime Background { get; private set; }
        public NineSliceRuntime SelectionInstance { get; private set; }
        public TextRuntime TextInstance { get; private set; }
        public TextRuntime PlaceholderTextInstance { get; private set; }
        public NineSliceRuntime FocusedIndicator { get; private set; }
        public SpriteRuntime CaretInstance { get; private set; }

        protected abstract string CategoryName { get; }

        public class TextBoxCategoryStates
        {
            public StateSave Enabled { get; set; }
            public StateSave Disabled { get; set; }
            public StateSave Highlighted { get; set; }
            public StateSave Focused { get; set; }
            public StateSave SingleLineMode { get; set; }
            public StateSave MultiLineMode { get; set; }
            public StateSave MultiLineModeNoWrap { get; set; }
        }

        public TextBoxCategoryStates States;

        public TextBoxBaseVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.States = new TextBoxCategoryStates();
                this.Width = 100;
                this.Height = 24;
                this.ClipsChildren = true;

                var uiSpriteSheetTexture = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{RenderingLibrary.SystemManagers.AssemblyPrefix}.UISpriteSheet.png");

                Background = new NineSliceRuntime();
                Background.Name = "Background";
                Background.X = 0;
                Background.Y = 0;
                Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                Background.XOrigin = HorizontalAlignment.Center;
                Background.YOrigin = VerticalAlignment.Center;
                Background.Width = 0;
                Background.Height = 0;
                Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                Background.Color = Styling.Colors.DarkGray;
                Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
                Background.Texture = uiSpriteSheetTexture;
                Background.ApplyState(NineSliceStyles.Bordered);

                SelectionInstance = new NineSliceRuntime();
                SelectionInstance.Name = "SelectionInstance";
                SelectionInstance.Color = Styling.Colors.Accent;
                SelectionInstance.Height = -4f;
                SelectionInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                SelectionInstance.Width = 7f;
                SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                SelectionInstance.X = 15f;
                SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                SelectionInstance.Y = 0f;
                SelectionInstance.TextureAddress = Gum.Managers.TextureAddress.Custom;
                SelectionInstance.Texture = uiSpriteSheetTexture;
                SelectionInstance.ApplyState(NineSliceStyles.Solid);

                TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";
                TextInstance.X = 4f;
                TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                TextInstance.Y = 0f;
                TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                TextInstance.Width = 0f;
                TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                TextInstance.Height = -4f;
                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                TextInstance.VerticalAlignment = VerticalAlignment.Center;
                TextInstance.Color = Styling.Colors.White;
                TextInstance.ApplyState(TextStyles.Normal);
                TextInstance.Text = "";
                
                PlaceholderTextInstance = new TextRuntime();
                PlaceholderTextInstance.Name = "PlaceholderTextInstance";
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
                
                // TODO: Fix this, this.ClipsChildren makes this invisible because it's outside the parent.
                FocusedIndicator = new NineSliceRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                FocusedIndicator.Color = Styling.Colors.Warning;
                FocusedIndicator.X = 0;
                FocusedIndicator.Y = 2;
                FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
                FocusedIndicator.XOrigin = HorizontalAlignment.Center;
                FocusedIndicator.YOrigin = VerticalAlignment.Top;
                FocusedIndicator.Width = 0;
                FocusedIndicator.Height = 2;
                FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                FocusedIndicator.TextureAddress = Gum.Managers.TextureAddress.Custom;
                FocusedIndicator.Texture = uiSpriteSheetTexture;
                FocusedIndicator.ApplyState(NineSliceStyles.Solid);
                FocusedIndicator.Visible = false;
                
                CaretInstance = new SpriteRuntime();
                CaretInstance.Name = "CaretInstance";
                CaretInstance.Color = Styling.Colors.Primary;
                CaretInstance.Height = 18f;
                CaretInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                CaretInstance.Texture = uiSpriteSheetTexture;
                CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
                CaretInstance.ApplyState(NineSliceStyles.Solid);
                CaretInstance.Width = 1f;
                CaretInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                CaretInstance.X = 4f;
                CaretInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                CaretInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                CaretInstance.Y = 0f;
                CaretInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                CaretInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                this.Children.Add(Background);
                this.Children.Add(SelectionInstance);
                this.Children.Add(TextInstance);
                this.Children.Add(PlaceholderTextInstance);
                this.Children.Add(FocusedIndicator);
                this.Children.Add(CaretInstance);

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
                AddVariable("TextInstance.Color", Styling.Colors.White);
                AddVariable("Background.Color", Styling.Colors.DarkGray);
                AddVariable("FocusedIndicator.Visible", false);
                States.Enabled = currentState;

                AddState(FrameworkElement.DisabledStateName);
                AddVariable("TextInstance.Color", Styling.Colors.Gray);
                AddVariable("Background.Color", Styling.Colors.DarkGray);
                AddVariable("FocusedIndicator.Visible", false);
                States.Disabled = currentState;

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.White);
                AddVariable("Background.Color", Styling.Colors.Gray);
                AddVariable("FocusedIndicator.Visible", false);
                States.Highlighted = currentState;

                AddState(FrameworkElement.FocusedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.White);
                AddVariable("Background.Color", Styling.Colors.DarkGray);
                AddVariable("FocusedIndicator.Visible", true);
                States.Focused = currentState;

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
                this.States.SingleLineMode = singleLineState;

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
                this.States.MultiLineMode = multiLineState;

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
                this.States.MultiLineModeNoWrap = multiLineNoWrapState;
            }
        }
    }
}
