using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;
using SkiaMonoGameRendering;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaInGum
{
    public class SkiaSpriteCanvas : FlatRedBall.Sprite
        , ISkiaRenderable

    {
        bool needsUpdate = true;

        public void InvalidateSurface() => needsUpdate = true;

        int ISkiaRenderable.TargetWidth => (int)Width;

        int ISkiaRenderable.TargetHeight => (int)Height;

        SKColorType ISkiaRenderable.TargetColorFormat { get => SKColorType.Rgba8888; }

        bool ISkiaRenderable.ShouldRender => needsUpdate && Width > 0 && Height > 0 && AbsoluteVisible;


        public event Action<SKSurface> PaintSurface;

        public SkiaSpriteCanvas()
        {
            Width = 100;
            Height = 100;
            TextureScale = 0;
        }

        public void AddToManagers()
        {
            SpriteManager.AddSprite(this);
            SkiaRenderer.AddRenderable(this);
        }

        public void RemoveFromManagers()
        {
            SpriteManager.RemoveSprite(this);
            SkiaRenderer.RemoveRenderable(this);
        }

        void ISkiaRenderable.DrawToSurface(SKSurface surface)
        {
            PaintSurface?.Invoke(surface);
        }

        void ISkiaRenderable.NotifyDrawnTexture(Texture2D texture)
        {
            this.Texture = texture;
            needsUpdate = false;
        }
    }
}
