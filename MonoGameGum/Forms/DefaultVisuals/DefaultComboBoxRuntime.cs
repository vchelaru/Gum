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
    public class DefaultComboBoxRuntime : InteractiveGue
    {
        public DefaultListBoxRuntime ListBoxInstance;

        public DefaultComboBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new ColoredRectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new DefaultListBoxRuntime(tryCreateFormsObject:false);
                ListBoxInstance.Name = "ListBoxInstance";


                // I dont' think we need an icon or focus indicator for the basic implementation.

                this.Height = 24f;
                this.Width = 256f;

                background.Color = new Microsoft.Xna.Framework.Color(32, 32, 32, 255);
                background.Height = 0f;
                background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.Width = 0f;
                background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.X = 0f;
                background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                background.XUnits = GeneralUnitType.PixelsFromMiddle;
                background.Y = 0f;
                background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                background.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Children.Add(background);

                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.Text = "Selected Item";
                TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.Width = -8f;
                TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Children.Add(TextInstance);

                ListBoxInstance.Height = 128f;
                ListBoxInstance.Width = 0f;
                ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ListBoxInstance.Y = 28f;
                this.Children.Add(ListBoxInstance);
                ListBoxInstance.Visible = false;
            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}
