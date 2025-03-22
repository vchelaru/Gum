using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using RenderingLibrary;
#endif

namespace MonoGameGum.GueDeriving
{
    public class ContainerRuntime : InteractiveGue
    {
        public float Alpha
        {
            get => (RenderableComponent as InvisibleRenderable)?.Alpha ?? 255;
            set
            {
                if (RenderableComponent is InvisibleRenderable invisibleRenderable)
                {
                    invisibleRenderable.Alpha = value;
                }
            }
        }

        public bool IsRenderTarget
        {
            get => (RenderableComponent as InvisibleRenderable)?.IsRenderTarget ?? false;
            set
            {
                if (RenderableComponent is InvisibleRenderable invisibleRenderable)
                {
                    invisibleRenderable.IsRenderTarget = value;
                }
            }
        }


        public Microsoft.Xna.Framework.Graphics.BlendState BlendState
        {
            get => RenderableComponent.BlendState.ToXNA();
            set
            {
                if (RenderableComponent is InvisibleRenderable invisibleRenderable)
                {
                    invisibleRenderable.BlendState = value.ToGum();
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(Blend));
                }
            }
        }

        public Gum.RenderingLibrary.Blend Blend
        {
            get
            {
                return Gum.RenderingLibrary.BlendExtensions.ToBlend(RenderableComponent.BlendState);
            }
            set
            {
                BlendState = value.ToBlendState().ToXNA();

                // NotifyPropertyChanged handled by BlendState:
            }
        }

        public ContainerRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new InvisibleRenderable());
                Width = 150;
                Height = 150;
                Visible = true;
            }
        }



        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    }
}
