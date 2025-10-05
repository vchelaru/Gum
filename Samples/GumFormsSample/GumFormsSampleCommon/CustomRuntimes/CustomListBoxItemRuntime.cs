using Gum.Converters;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomRuntimes;

internal class CustomListBoxItemRuntime : InteractiveGue
{
    public CustomListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Height = 30f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            var Background = new ColoredRectangleRuntime();
            Background.Name = "Background";
            var TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            var FocusedIndicator = new RectangleRuntime();
            FocusedIndicator.Name = "FocusedIndicator";

            Background.Height = 0f;
            Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Background.Visible = false;
            Background.Width = 0f;
            Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Background.X = 0f;
            Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Y = 0f;
            Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            this.Children.Add(Background);

            TextInstance.Height = 0f;
            TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            TextInstance.Text = "ListBox Item";
            TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.Width = -8f;
            TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            this.Children.Add(TextInstance);

            FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            FocusedIndicator.Visible = false;
            FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            FocusedIndicator.YUnits = GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.Color = new Microsoft.Xna.Framework.Color(205, 142, 44);
            this.Children.Add(FocusedIndicator);

            var listBoxItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            listBoxItemCategory.Name = "ListBoxItemCategory";

            listBoxItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = "Enabled",
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = false,
                            Name = "Background.Visible"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = false,
                            Name = "FocusedIndicator.Visible"
                        }
                    }
            });

            listBoxItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = "Highlighted",
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = true,
                            Name = "Background.Visible"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = Microsoft.Xna.Framework.Color.Green,
                            Name = "Background.Color"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = false,
                            Name = "FocusedIndicator.Visible"
                        }
                    }
            });

            listBoxItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = "Selected",
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = true,
                            Name = "Background.Visible"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = Microsoft.Xna.Framework.Color.Yellow,
                            Name = "Background.Color"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = false,
                            Name = "FocusedIndicator.Visible"
                        }
                    }
            });

            listBoxItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = "Focused",
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                    {
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = false,
                            Name = "Background.Visible"
                        },
                        new Gum.DataTypes.Variables.VariableSave()
                        {
                            Value = true,
                            Name = "FocusedIndicator.Visible"
                        }
                    }
            });

            this.AddCategory(listBoxItemCategory);

        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem();
        }
    }

    public ListBoxItem FormsControl => FormsControlAsObject as ListBoxItem;
}



