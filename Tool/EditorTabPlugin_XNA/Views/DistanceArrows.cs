using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using Microsoft.Xna.Framework;

namespace Gum.Wireframe
{
    public class DistanceArrows
    {
        private ToolLayerService _toolLayerService;
        Arrow Arrow1;
        Arrow Arrow2;

        public bool IsStartArrowTipVisible
        {
            get => Arrow1.IsArrowTipVisible;
            set => Arrow1.IsArrowTipVisible = value;
        }

        public bool IsEndArrowTipVisible
        {
            get => Arrow2.IsArrowTipVisible;
            set => Arrow2.IsArrowTipVisible = value;
        }

        public HorizontalAlignment TextHorizontalAlignment { get; set; }

        Text _distanceText;

        bool _visible;

        public System.Drawing.Color TextColor
        {
            get => _distanceText.Color;
            set => _distanceText.Color = value;
        }

        public System.Drawing.Color ArrowColor
        {
            get => Arrow1.Color;
            set
            {
                Arrow1.Color = value;
                Arrow2.Color = value;
            }
        }

        public float Zoom { get; set; } = 1;

        public bool Visible
        {
            get => _visible;
            set
            {
                this._visible = value;
                Arrow1.Visible = value;
                Arrow2.Visible = value;
                _distanceText.Visible = value;
            }
        }

        public DistanceArrows(SystemManagers systemManagers, ToolFontService toolFontService, ToolLayerService toolLayerService)
        {
            _toolLayerService = toolLayerService;
            Arrow1 = new Arrow();
            Arrow2 = new Arrow();

            if(_toolLayerService.TopLayer == null)
            {
                throw new InvalidOperationException("_toolLayerService.TopLayer should not be null at this point");
            }

            _distanceText = new Text(systemManagers);
            _distanceText.RenderBoundary = false;
            _distanceText.HorizontalAlignment = HorizontalAlignment.Center;
            _distanceText.VerticalAlignment = VerticalAlignment.Center;
            _distanceText.BitmapFont = toolFontService.ToolFont;
        }

        public void SetFrom(Vector2 startAbsolute, Vector2 endAbsolute)
        {
            var midpoint = (startAbsolute + endAbsolute) / 2.0f;


            _distanceText.RawText =
                (endAbsolute - startAbsolute).Length().ToString("0.00");

            _distanceText.FontScale = 1 / Zoom;
            //distanceText.Position.ToString();
            _distanceText.Position = midpoint;

            switch (TextHorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    _distanceText.X -= _distanceText.EffectiveWidth / 2.0f;
                    break;
                case HorizontalAlignment.Right:
                    _distanceText.X -= _distanceText.EffectiveWidth;
                    break;

                case HorizontalAlignment.Left:
                    // do nothing
                    break;
            }

            _distanceText.Y -= _distanceText.EffectiveHeight / 2.0f;
            _distanceText.Width = null;
            _distanceText.Height = 0;

            var xAbsolute = Math.Abs(startAbsolute.X - endAbsolute.X);
            var yAbsolute = Math.Abs(startAbsolute.Y - endAbsolute.Y);

            float textGap = 18;
            if(yAbsolute > xAbsolute)
            {
                textGap = _distanceText.EffectiveHeight ;
            }
            else
            {
                textGap = _distanceText.EffectiveWidth;
            }

            textGap += 4 / Zoom;



            if ((startAbsolute - endAbsolute).Length() < textGap)
            {
                Arrow1.Visible = false;
                Arrow2.Visible = false;
            }
            else
            {
                Arrow1.Visible = _visible;
                Arrow2.Visible = _visible;
            }

            var arrow1Start = midpoint + (startAbsolute - midpoint).AtLength(textGap / 2);
            var arrow2Start = midpoint + (endAbsolute - midpoint).AtLength(textGap / 2);

            // make sure we can at least draw the line a few pixels
            Arrow1.Visible = (arrow1Start - startAbsolute).Length() > 6/Zoom;
            Arrow2.Visible = (arrow2Start - endAbsolute).Length() > 6 / Zoom;

            Arrow1.SetFrom(arrow1Start, startAbsolute, Zoom);
            Arrow2.SetFrom(arrow2Start, endAbsolute, Zoom);
        }

        public void AddToManagers()
        {
            Arrow1.AddToManagers(_toolLayerService.TopLayer);
            Arrow2.AddToManagers(_toolLayerService.TopLayer);

            _toolLayerService.TopLayer.Add(_distanceText);
        }

        public void RemoveFromManagers()
        {
            Arrow1.RemoveFromManagers(_toolLayerService.TopLayer);
            Arrow2.RemoveFromManagers(_toolLayerService.TopLayer);

            _toolLayerService.TopLayer.Remove(_distanceText);
        }
    }

    class Arrow
    {
        Line endLine1;
        Line endLine2;
        Line body;

        public System.Drawing.Color Color
        {
            get => body.Color;
            set
            {
                body.Color = value;
                endLine1.Color = value;
                endLine2.Color = value;
            }
        }

        bool visible;
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                body.Visible = value;
                UpdateEndlineVisibility();
            }
        }
        bool isArrowTipVisible = true;
        public bool IsArrowTipVisible
        {
            get => isArrowTipVisible;
            set
            {
                isArrowTipVisible = value;
                UpdateEndlineVisibility();
            }
        }



        private void UpdateEndlineVisibility()
        {
            endLine1.Visible = Visible && IsArrowTipVisible;
            endLine2.Visible = Visible && IsArrowTipVisible;
        }


        public Arrow()
        {
            this.endLine1 = new Line();
            this.endLine2 = new Line();
            this.body = new Line();
        }

        public void AddToManagers(Layer layer)
        {
            layer.Add(endLine1);
            layer.Add(endLine2);
            layer.Add(body);
        }

        public void RemoveFromManagers(Layer layer) 
        {
            layer.Remove(endLine1);
            layer.Remove(endLine2);
            layer.Remove(body);
        }

        public void SetFrom(Vector2 startAbsolute, Vector2 endAbsolute, float zoom = 1)
        {
            if (startAbsolute == endAbsolute) return;

            body.X = startAbsolute.X;
            body.Y = startAbsolute.Y;

            body.RelativePoint = new Vector2(
                endAbsolute.X - startAbsolute.X,
                endAbsolute.Y - startAbsolute.Y);

            endLine1.SetPosition(endAbsolute);
            endLine2.SetPosition(endAbsolute);
            var normalizedBack = Vector2.Normalize(startAbsolute - endAbsolute);

            float arrowPointLineLength = 8 / zoom;

            var angle = Vector2Methods.Angle(normalizedBack).Value;
            angle += MathHelper.PiOver4;
            endLine1.RelativePoint = Vector2Methods.AtAngle(new Vector2(arrowPointLineLength, 0), angle);

            angle -= MathHelper.PiOver4;
            angle -= MathHelper.PiOver4;
            endLine2.RelativePoint = Vector2Methods.AtAngle(new Vector2(arrowPointLineLength, 0), angle);
        }

    }

    static class Vector2Methods
    {
        public static Vector2 RotatedBy(this Vector2 vector2, float radiansToRotateBy)
        {
            if (vector2.X == 0 && vector2.Y == 0)
            {
                return vector2;
            }
            else
            {
                var existingAngle = vector2.Angle().Value;
                var newAngle = existingAngle + radiansToRotateBy;
                return FromAngle(newAngle) * vector2.Length();
            }
        }

        public static Vector2 AtAngle(this Vector2 vector2, float angleRadians)
        {
            return FromAngle(angleRadians) * vector2.Length();
        }

        public static Vector2 FromAngle(float angle)
        {
            return new Vector2((float)Math.Cos(angle),
                (float)Math.Sin(angle));
        }

        public static float? Angle(this Vector2 vector)
        {
            if (vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return (float)System.Math.Atan2(vector.Y, vector.X);
            }
        }

        public static Vector2 AtLength(this Vector2 vector2, float length)
        {
            return vector2.NormalizedOrZero() * length;
        }

        public static Vector2 NormalizedOrZero(this Vector2 vector)
        {
            if (vector.X != 0 || vector.Y != 0)
            {
                return Vector2.Normalize(vector);
            }
            else
            {
                return Vector2.Zero;
            }
        }
    }
}
