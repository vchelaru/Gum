using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public abstract class SkiaShapeRuntime : BindableGraphicalUiElement
    {
        protected abstract RenderableBase ContainedRenderable { get; }

        #region Solid colors

        public int Alpha
        {
            get => ContainedRenderable.Alpha;
            set => ContainedRenderable.Alpha = value;
        }

        public int Blue
        {
            get => ContainedRenderable.Blue;
            set => ContainedRenderable.Blue = value;
        }

        public int Green
        {
            get => ContainedRenderable.Green;
            set => ContainedRenderable.Green = value;
        }

        public int Red
        {
            get => ContainedRenderable.Red;
            set => ContainedRenderable.Red = value;
        }

        public SKColor Color
        {
            get => ContainedRenderable.Color;
            set => ContainedRenderable.Color = value;
        }
        #endregion


        public bool IsFilled
        {
            get => ContainedRenderable.IsFilled;
            set => ContainedRenderable.IsFilled = value;
        }

        public float StrokeWidth
        {
            get => ContainedRenderable.StrokeWidth;
            set => ContainedRenderable.StrokeWidth = value;
        }
    }
}
