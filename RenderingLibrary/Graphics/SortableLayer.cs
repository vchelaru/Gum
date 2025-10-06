#if SKIA
using System;
#endif
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics
{
    public class SortableLayer : Layer, IRenderable
    {
        public bool Wrap
        {
            get { return false; }
        }

        public BlendState BlendState => BlendState.NonPremultiplied; 

        public void Render(ISystemManagers managers)
        {
            // We can't have statics in interfaces in .NET 4.7 so need to crash
            //if (managers == null)
            //{
            //    managers = ISystemManagers.Default;
            //}
            managers.Renderer.RenderLayer(managers, this);
        }

        void IRenderable.PreRender() { }

        public float Z
        {
            get;
            set;
        }



#if SKIA
        public void Render(SkiaSharp.SKCanvas canvas)
        {
            throw new NotImplementedException();
        }
#endif

#if !NET8_0_OR_GREATER
    public string BatchKey => string.Empty;
    public void StartBatch(ISystemManagers systemManagers) { }
    public void EndBatch(ISystemManagers systemManagers) { }
#endif

    }
}
