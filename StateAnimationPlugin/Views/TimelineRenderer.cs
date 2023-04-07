using Gum.Mvvm;
using SkiaSharp;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace StateAnimationPlugin.Views
{
    internal static class TimelineRenderer
    {
        static int skiaDrawRow = 0;
        static ElementAnimationsViewModel ViewModel;
        static double CanvasWidth;
        static double CanvasHeight;
        static SKCanvas Canvas;

        static SKPaint separatorPaint;
        static TimelineRenderer()
        {
            separatorPaint = new SKPaint();
            separatorPaint.Color = new SKColor(190, 190, 190);
            separatorPaint.StrokeWidth = 1f;
        }

        public static void DrawSurface(ElementAnimationsViewModel viewModel, SKSurface surface, SKImageInfo info)
        {
            CanvasWidth = info.Width;
            CanvasHeight = info.Height;
            Canvas = surface.Canvas;
            surface.Canvas.Clear();
            skiaDrawRow = 0;
            ViewModel = viewModel;
            /////////////////early out///////////////////////

            if (ViewModel?.SelectedAnimation == null)
            {
                return;
            }

            //////////////end early out/////////////////////

            // Ironically can't use SkiaGum here because of the confusion in libraries.
            var animation = ViewModel.SelectedAnimation;

            var rows = 0;
            foreach(var frame in animation.Keyframes)
            {
                var isSubanimation = !string.IsNullOrEmpty(frame.AnimationName);
                if(isSubanimation)
                {
                    rows++;
                }
            }

            DrawBackground(rows);

            DrawHorizontalSeparators(rows);
            DrawVertialSeparators(rows);
            foreach (var frame in animation.Keyframes)
            {
                DrawKeyframe(frame);
            }


            DrawCurrentTimeLine();

        }

        private static void DrawBackground(int rows)
        {
            var paint = new SKPaint();
            var backgroundColor = new SKColor(240, 240, 240);
            paint.Color = backgroundColor;
            var end = TimeToX(ViewModel.SelectedAnimation.Length);
            var start = TimeToX(0);
            Canvas.DrawRect(start, 0, (float)end - start, (float)(rows+1)*spacingPerRow, paint);

        }


        private static void DrawHorizontalSeparators(int rows)
        {

            var startX = TimeToX(0);
            var endX = TimeToX(ViewModel.SelectedAnimation.Length);
            for(int i = 0; i < rows + 2; i++)
            {
                var y = i * spacingPerRow;
                Canvas.DrawLine(startX, y, endX, y, separatorPaint);
            }
        }

        private static void DrawVertialSeparators(int rows)
        {
            var bottom = (rows + 1) * spacingPerRow;
            for(int i = 0; i < ViewModel.SelectedAnimation.Length + 1; i++)
            {
                var x = TimeToX(i);

                Canvas.DrawLine(x, 0, x, bottom, separatorPaint);
            }
        }

        private static void DrawCurrentTimeLine()
        {
            var x = TimeToX(ViewModel.DisplayedAnimationTime);

            var linePaint = new SKPaint();
            linePaint.Color = SKColors.LightBlue;
            linePaint.StrokeWidth = 1;
            Canvas.DrawLine(x, 0, x, (float)CanvasHeight, linePaint);
        }

        enum ShapeType
        {
            Circle,
            Rectangle
        }

        const int buffer = 5;
        const float spacingPerRow = 22;
        static float TimeToX(double time)
        {
            var percentX = time / ViewModel.SelectedAnimation.Length;
            var usableWidth = CanvasWidth - (buffer * 2);
            return (float)(buffer + usableWidth * percentX);
        }

        public static double XToTime(float X, float animationLength)
        {
            var usableWidth = CanvasWidth - (buffer * 2);
            var offset = X - buffer;

            var ratio = offset / usableWidth;
            ratio = Math.Max(0, ratio);
            ratio = Math.Min(100, ratio);

            return animationLength * ratio;
        }

        private static void DrawKeyframe(AnimatedKeyframeViewModel frame)
        {
            AnimationViewModel animation = ViewModel.SelectedAnimation;
            var usableWidth = CanvasWidth - (buffer * 2);

            float x = TimeToX(frame.Time);

            var paint = new SKPaint();

            var isState = !string.IsNullOrEmpty(frame.StateName);
            var isSubanimation = !string.IsNullOrEmpty(frame.AnimationName);
            var isEvent = !string.IsNullOrEmpty(frame.EventName);

            var isSelected = frame == ViewModel.SelectedAnimation.SelectedKeyframe;

            float width = 10f;
            float height = 10f;
            float top = 0;
            var drawBorder = true;
            var shapeType = ShapeType.Circle;

            var deselectedColor = new SKColor(255, 100, 100);

            if (isState)
            {
                if (isSelected)
                {
                    drawBorder = true;
                    paint.Color = SKColors.Yellow;
                }
                else
                {
                    paint.Color = deselectedColor;
                }
            }
            else if (isSubanimation)
            {
                shapeType = ShapeType.Rectangle;
                var percentWidth = frame.Length / animation.Length;
                width = (float)(percentWidth * usableWidth);
                if (isSelected)
                {
                    drawBorder = true;
                    paint.Color = SKColors.Yellow;
                }
                else
                {
                    paint.Color = deselectedColor;
                }
                top = (1 + skiaDrawRow) * spacingPerRow;
                skiaDrawRow++;
            }
            else if (isEvent)
            {
                if (isSelected)
                {
                    drawBorder = true;
                    paint.Color = SKColors.Yellow;
                }
                else
                {
                    paint.Color = SKColors.Green;
                }
            }

            if (shapeType == ShapeType.Circle)
            {
                var y = top + spacingPerRow / 2f;
                Canvas.DrawCircle(x, y, width / 2, paint);
                if (drawBorder)
                {
                    var borderPaint = new SKPaint();
                    borderPaint.Color = SKColors.Black;
                    borderPaint.StrokeWidth = 1;
                    borderPaint.IsStroke = true;
                    Canvas.DrawCircle(x, y, width / 2, borderPaint);
                }
            }
            else
            {
                var margin = (spacingPerRow - height) / 2.0f;
                Canvas.DrawRect(x, top + margin, width, height, paint);

                if (drawBorder)
                {
                    var borderPaint = new SKPaint();
                    borderPaint.Color = SKColors.Black;
                    borderPaint.StrokeWidth = 1;
                    borderPaint.IsStroke = true;
                    Canvas.DrawRect(x, top + margin, width, height, borderPaint);
                }
            }
        }
    }
}
