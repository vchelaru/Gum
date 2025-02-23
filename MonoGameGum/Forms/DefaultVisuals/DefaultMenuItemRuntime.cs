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

namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultMenuItemRuntime : InteractiveGue
{
    public DefaultMenuItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 6;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Height = 3;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            var Background = new ColoredRectangleRuntime();
            Background.Name = "Background";


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


            var TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            TextInstance.Text = "Label";
            TextInstance.X = 6;
            TextInstance.Y = 3;
            TextInstance.Height = 0;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.Width = 0;
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            this.Children.Add(TextInstance);

            var menuItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            menuItemCategory.Name = "MenuItemCategory";
            this.AddCategory(menuItemCategory);

            menuItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = "Enabled",
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                {
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = false,
                        Name = "Background.Visible"
                    },
                    //new Gum.DataTypes.Variables.VariableSave()
                    //{
                    //    Value = false,
                    //    Name = "FocusedIndicator.Visible"
                    //}
                }
            });

            menuItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
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
                        Value = new Microsoft.Xna.Framework.Color(205, 142, 44),
                        Name = "Background.Color"
                    },
                    //new Gum.DataTypes.Variables.VariableSave()
                    //{
                    //    Value = false,
                    //    Name = "FocusedIndicator.Visible"
                    //}
                }
            });

            menuItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
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
                        Value = new Microsoft.Xna.Framework.Color(143, 68, 121),
                        Name = "Background.Color"
                    },
                    //new Gum.DataTypes.Variables.VariableSave()
                    //{
                    //    Value = false,
                    //    Name = "FocusedIndicator.Visible"
                    //}
                }
            });
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new MenuItem();
        }

    }

    public override object FormsControlAsObject 
    { 
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if(value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = DefaultScrollVisualTemplate;
            }
        }
    }

    VisualTemplate DefaultScrollVisualTemplate => new VisualTemplate(() =>
                new DefaultScrollViewerRuntimeSizedToChildren(fullInstantiation: true, tryCreateFormsObject: false));

    public MenuItem FormsControl => FormsControlAsObject as MenuItem;
}
