using RenderingLibrary.Graphics;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace SkiaGum.Renderables
{
    /// <summary>
    /// Adapts a <see cref="RenderableShapeBase"/> to the <see cref="ISkiaSurfaceDrawable"/>
    /// interface so it can be wrapped by <see cref="SkiaTexturedRenderable"/> for use in the
    /// Gum tool's MonoGame rendering pipeline.
    /// </summary>
    public class RenderableShapeAdapter : ISkiaSurfaceDrawable
    {
        readonly RenderableShapeBase _shape;

        public RenderableShapeAdapter(RenderableShapeBase shape)
        {
            _shape = shape;
        }

        /// <summary>
        /// The underlying shape. Use this target for property access via reflection
        /// (e.g. in <c>RegisterSkiaPropertyRedirect</c>).
        /// </summary>
        public RenderableShapeBase Shape => _shape;

        #region ISkiaSurfaceDrawable

        public float Width
        {
            get => _shape.Width;
            set => _shape.Width = value;
        }

        public float Height
        {
            get => _shape.Height;
            set => _shape.Height = value;
        }

        public bool NeedsUpdate
        {
            get => _shape.NeedsUpdate;
            set => _shape.NeedsUpdate = value;
        }

        public Color Color => Color.FromArgb(
            _shape.Color.Alpha, _shape.Color.Red, _shape.Color.Green, _shape.Color.Blue);

        public bool ShouldApplyColorOnSpriteRender => _shape.ShouldApplyColorOnSpriteRender;

        public float XSizeSpillover => _shape.XSizeSpillover;

        public float YSizeSpillover => _shape.YSizeSpillover;

        public ColorOperation ColorOperation
        {
            get => _shape.ColorOperation;
            set => _shape.ColorOperation = value;
        }

        public void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var xSpillover = _shape.XSizeSpillover;
            var ySpillover = _shape.YSizeSpillover;

            var boundingRect = new SKRect(
                xSpillover,
                ySpillover,
                xSpillover + _shape.Width,
                ySpillover + _shape.Height);

            if (_shape.IsFilled == false && _shape.IsOffsetAppliedForStroke)
            {
                boundingRect.Left += _shape.StrokeWidth / 2.0f;
                boundingRect.Top += _shape.StrokeWidth / 2.0f;
                boundingRect.Right -= _shape.StrokeWidth / 2.0f;
                boundingRect.Bottom -= _shape.StrokeWidth / 2.0f;
            }

            _shape.DrawBound(boundingRect, surface.Canvas, 0f);
        }

        public void PreRender() => _shape.PreRender();

        #endregion
    }
}
