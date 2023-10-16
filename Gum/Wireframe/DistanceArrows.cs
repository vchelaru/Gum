using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe
{
    internal class DistanceArrows
    {
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

        Text distanceText;

        Layer layer;

        bool visible;



        public float Zoom { get; set; } = 1;

        public bool Visible
        {
            get => visible;
            set
            {
                this.visible = value;
                Arrow1.Visible = value;
                Arrow2.Visible = value;
                distanceText.Visible = value;
            }
        }

        public DistanceArrows(SystemManagers systemManagers, Layer layer)
        {
            Arrow1 = new Arrow();
            Arrow2 = new Arrow();

            distanceText = new Text(systemManagers);
            distanceText.RenderBoundary = false;
            distanceText.HorizontalAlignment = HorizontalAlignment.Center;
            distanceText.VerticalAlignment = VerticalAlignment.Center;
            this.layer = layer;
        }

        public void SetFrom(Vector2 startAbsolute, Vector2 endAbsolute)
        {
            var midpoint = (startAbsolute + endAbsolute) / 2.0f;


            distanceText.RawText =
                (endAbsolute - startAbsolute).Length().ToString("0.00");

            distanceText.FontScale = 1 / Zoom;
            //distanceText.Position.ToString();
            distanceText.Position = midpoint;

            switch (TextHorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    distanceText.X -= distanceText.EffectiveWidth / 2.0f;
                    break;
                case HorizontalAlignment.Right:
                    distanceText.X -= distanceText.EffectiveWidth;
                    break;

                case HorizontalAlignment.Left:
                    // do nothing
                    break;
            }

            distanceText.Y -= distanceText.EffectiveHeight / 2.0f;
            distanceText.Width = 0;
            distanceText.Height = 0;

            var xAbsolute = Math.Abs(startAbsolute.X - endAbsolute.X);
            var yAbsolute = Math.Abs(startAbsolute.Y - endAbsolute.Y);

            float textGap = 18;
            if(yAbsolute > xAbsolute)
            {
                textGap = distanceText.EffectiveHeight ;
            }
            else
            {
                textGap = distanceText.EffectiveWidth;
            }

            textGap += 4 / Zoom;



            if ((startAbsolute - endAbsolute).Length() < textGap)
            {
                Arrow1.Visible = false;
                Arrow2.Visible = false;
            }
            else
            {
                Arrow1.Visible = visible;
                Arrow2.Visible = visible;
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
            Arrow1.AddToManagers(layer);
            Arrow2.AddToManagers(layer);

            layer.Add(distanceText);
        }

        public void RemoveFromManagers()
        {
            Arrow1.RemoveFromManagers(layer);
            Arrow2.RemoveFromManagers(layer);

            layer.Remove(distanceText);
        }
    }

    class Arrow
    {
        Line endLine1;
        Line endLine2;
        Line body;

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
