using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Styling = Gum.Forms.DefaultVisuals.Styling;

namespace MonoGameGum.Forms.DefaultVisuals;
public class DefaultSplitterRuntime : InteractiveGue
{
    public DefaultSplitterRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;

        if (fullInstantiation)
        {
            this.Width = 8;
            this.Height = 8;

            var background = new ColoredRectangleRuntime();
            background.Name = "Background";
            background.Dock(Gum.Wireframe.Dock.Fill);
            background.Color = Styling.ActiveStyle.Colors.Gray;
            this.AddChild(background);

        }

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Splitter(this);
        }
    }

    public Splitter FormsControl => FormsControlAsObject as Splitter;
}
