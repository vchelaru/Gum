using Gum.Converters;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
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
    public TextRuntime TextInstance { get; private set; }

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
            Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Visible = false;
            Background.Width = 0f;
            Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.X = 0f;
            Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Y = 0f;
            Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            this.Children.Add(Background);


            var innerContainer = new ContainerRuntime();
            this.Children.Add(innerContainer);
            innerContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            innerContainer.Dock(Gum.Wireframe.Dock.SizeToChildren);

            TextInstance = new TextRuntime();
            innerContainer.Children.Add(TextInstance);
            TextInstance.Name = "TextInstance";
            TextInstance.Text = "Label";
            TextInstance.X = 6;
            TextInstance.Y = 3;
            TextInstance.Height = 0;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.Width = 0;
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            var SubmenuIndicatorInstance = new TextRuntime();
            innerContainer.Children.Add(SubmenuIndicatorInstance);
            SubmenuIndicatorInstance.Name = "SubmenuIndicatorInstance";
            SubmenuIndicatorInstance.Text = ">";
            SubmenuIndicatorInstance.X = 12;

            var menuItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            menuItemCategory.Name = "MenuItemCategory";
            this.AddCategory(menuItemCategory);

            menuItemCategory.States.Add(new Gum.DataTypes.Variables.StateSave()
            {
                Name = FrameworkElement.EnabledStateName,
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                {
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = false,
                        Name = "Background.Visible"
                    },
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = Color.White,
                        Name = "TextInstance.Color"
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
                Name = FrameworkElement.DisabledStateName,
                Variables = new List<Gum.DataTypes.Variables.VariableSave>()
                {
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = false,
                        Name = "Background.Visible"
                    },
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = Color.Gray,
                        Name = "TextInstance.Color"
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
                Name = FrameworkElement.HighlightedStateName,
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
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = Color.White,
                        Name = "TextInstance.Color"
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
                Name = FrameworkElement.SelectedStateName,
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
                    new Gum.DataTypes.Variables.VariableSave()
                    {
                        Value = Color.White,
                        Name = "TextInstance.Color"
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
            FormsControlAsObject = new MenuItem(this);
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

    Gum.Forms.VisualTemplate DefaultScrollVisualTemplate => new (() =>
                new DefaultScrollViewerRuntimeSizedToChildren(fullInstantiation: true, tryCreateFormsObject: false));

    public MenuItem FormsControl => FormsControlAsObject as MenuItem;
}
