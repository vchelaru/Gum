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
    public class SplitterVisual : InteractiveGue
    {
        public NineSliceRuntime Background { get; private set; }

        public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            Width = 8;
            Height = 8;

            var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.Dock(Gum.Wireframe.Dock.Fill);
            Background.Color = Styling.Colors.DarkGray;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Bordered);
            this.AddChild(Background);

            if(tryCreateFormsObject)
            {
                FormsControlAsObject = new Splitter(this);
            }
        }

        public Splitter FormsControl => FormsControlAsObject as Splitter;
    }
}
