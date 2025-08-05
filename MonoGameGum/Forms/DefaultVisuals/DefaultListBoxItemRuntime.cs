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


namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultListBoxItemRuntime : InteractiveGue
{
    public ColoredRectangleRuntime Background { get; private set; }
    public TextRuntime TextInstance { get; private set; }

    public RectangleRuntime FocusedIndicator { get; private set; }

    public DefaultListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Height = 6f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

            Background = new ColoredRectangleRuntime();
            Background.Name = "Background";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            FocusedIndicator = new RectangleRuntime();
            FocusedIndicator.Name = "FocusedIndicator";

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

            TextInstance.Height = 0f;
            TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            TextInstance.Text = "ListBox Item";
            TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.Width = -8f;
            TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            this.Children.Add(TextInstance);

            FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.Visible = false;
            FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            FocusedIndicator.YUnits = GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.Color = new Microsoft.Xna.Framework.Color(205, 142, 44);
            FocusedIndicator.Width = 0;
            FocusedIndicator.Height = 0;
            FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.Children.Add(FocusedIndicator);

            var listBoxItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            listBoxItemCategory.Name = "ListBoxItemCategory";
            this.AddCategory(listBoxItemCategory);


            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                listBoxItemCategory.States.Add(state);
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
            AddVariable("Background.Visible", false);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.ActiveStyle.Colors.Primary);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.SelectedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.ActiveStyle.Colors.Accent);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("Background.Visible", false);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.DisabledStateName);
            AddVariable("Background.Visible", false);
            AddVariable("FocusedIndicator.Visible", true);
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem(this);
        }
    }

    public ListBoxItem FormsControl => FormsControlAsObject as ListBoxItem;
}
