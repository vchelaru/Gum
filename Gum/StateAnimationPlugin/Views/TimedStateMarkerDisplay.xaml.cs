using SkiaSharp;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for TimedStateMarkerDisplay.xaml
    /// </summary>
    public partial class TimedStateMarkerDisplay : UserControl
    {
        #region Fields

        List<Rectangle> mRectangles = new List<Rectangle>();


        #endregion

        #region Properties


        public double RangeMaximum
        {
            get { return (double)GetValue(RangeMaximumProperty); }
            set { SetValue(RangeMaximumProperty, value); RefreshRectangles(); }
        }

        // Using a DependencyProperty as the backing store for RangeMaximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RangeMaximumProperty =
            DependencyProperty.Register("RangeMaximum", typeof(double), typeof(TimedStateMarkerDisplay), 
            new PropertyMetadata(1.0, RangePropertyChangeCallback));



        public IEnumerable<AnimatedKeyframeViewModel> MarkerItemSource
        {
            get { return (IEnumerable<AnimatedKeyframeViewModel>)GetValue(MarkerItemSourceProperty); }
            set { SetValue(MarkerItemSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MarkerItemSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkerItemSourceProperty =
            DependencyProperty.Register("MarkerItemSource", typeof(IEnumerable<AnimatedKeyframeViewModel>), 
            typeof(TimedStateMarkerDisplay), new PropertyMetadata(null, MarkerItemSourceChangeCallback));



        public AnimatedKeyframeViewModel SelectedKeyframe
        {
            get { return (AnimatedKeyframeViewModel)GetValue(SelectedKeyframeProperty); }
            set { SetValue(SelectedKeyframeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedKeyframe.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedKeyframeProperty =
            DependencyProperty.Register(
                "SelectedKeyframe", 
                typeof(AnimatedKeyframeViewModel), 
                typeof(TimedStateMarkerDisplay), 
                new PropertyMetadata(null, SelectedKeyframeChangedCallback));



        #endregion

        #region Methods

        public TimedStateMarkerDisplay()
        {
            InitializeComponent();
            
            //RangeMinimum = 0;
            RangeMaximum = 7;

            this.Canvas.SizeChanged += HandleCanvasSizeChanged;

        }

        private void HandleCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            RefreshRectangles();
        }


        private static void SelectedKeyframeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (TimedStateMarkerDisplay)d;

            //var oldKeyframeViewModel = e.OldValue as AnimatedKeyframeViewModel;

            //if(oldKeyframeViewModel != null)
            //{
            //    oldKeyframeViewModel.PropertyChanged -= instance.SelectedKeyframeValueChanged;
            //}

            //if(instance.SelectedKeyframe != null)
            //{
            //    instance.SelectedKeyframe.PropertyChanged += instance.SelectedKeyframeValueChanged;
            //}

            // make sure rectangles exist:
            instance.AddAndDestroyRectanglesToMatchSourceCount();


            instance.UpdateRectangleProperties();
        }

        private static void RangePropertyChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimedStateMarkerDisplay display = (TimedStateMarkerDisplay)d;
            display.RefreshRectangles();
        }

        private void SelectedKeyframeValueChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(AnimatedKeyframeViewModel.Time):
                    UpdateRectangleProperties();
                    break;
            }
        }


        private static void MarkerItemSourceChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimedStateMarkerDisplay instance = (TimedStateMarkerDisplay)d;
            var oldValues = e.OldValue as IEnumerable<AnimatedKeyframeViewModel>;

            if(oldValues != null)
            {
                foreach(var item in oldValues)
                {
                    item.PropertyChanged -= instance.HandleItemPropertyChanged;
                }
            }

            var newValues = e.NewValue as IEnumerable<AnimatedKeyframeViewModel>;
            if(newValues != null)
            {
                foreach(var item in newValues)
                {
                    item.PropertyChanged += instance.HandleItemPropertyChanged;
                }
            }
            instance.RefreshRectangles();
        }

        private void HandleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshRectangles();
        }

        private void RefreshRectangles()
        {
            AddAndDestroyRectanglesToMatchSourceCount();

            UpdateRectangleProperties();
        }

        public void UpdateRectangleProperties()
        {
            int subAnimationIndex = 0;
            int index = 0;

            const float Border = 5;

            double requiredHeight = 10;

            if (MarkerItemSource != null)
            {
                foreach (var keyframe in MarkerItemSource)
                {
                    bool isState = !string.IsNullOrEmpty(keyframe.StateName);

                    double percentage = 0;

                    if (RangeMaximum != 0)
                    {
                        percentage = keyframe.Time / RangeMaximum;
                    }

                    // Why multiply the border by 3?
                    // Not sure, but 2 didn't work.
                    double canvasWidth = (Canvas.ActualWidth - Border * 3);

                    var offset = Border + percentage * canvasWidth;

                    if(index < mRectangles.Count)
                    {
                        var rectangle = mRectangles[index];
                        rectangle.SetValue(Canvas.LeftProperty, offset);
                        double topValue = 0;

                        if (isState)
                        {
                            rectangle.Fill = Brushes.Red;
                            rectangle.Width = 10;
                        }
                        else
                        {
                            rectangle.Fill = Brushes.Blue;
                            if (RangeMaximum != 0)
                            {
                                double widthPercentage = (keyframe.Length / RangeMaximum);
                                rectangle.Width = Border + widthPercentage * canvasWidth;
                            }
                            else
                            {
                                rectangle.Width = 10;
                            }
                            // see below, keep all at 0
                            //topValue = 10.0 + subAnimationIndex * (rectangle.Height + 1);
                            subAnimationIndex++;
                        }

                        if(keyframe == SelectedKeyframe)
                        {
                            rectangle.Fill = Brushes.Yellow;
                            rectangle.Stroke = Brushes.Black;
                        }
                        else
                        {
                            rectangle.Stroke = null;
                        }

                        rectangle.SetValue(Canvas.TopProperty, topValue);

                        // We used to make this control taller if there are multiple rectangles, but it causes problems
                        // We are going to have the position of rectangles to be all on the same row.
                        //requiredHeight = System.Math.Max(requiredHeight, topValue + rectangle.Height);


                    }

                    index++;
                }
            }

            this.Height = requiredHeight;
        }

        private void AddAndDestroyRectanglesToMatchSourceCount()
        {
            var timesCount = 0;

            if (MarkerItemSource != null)
            {
                timesCount = MarkerItemSource.Count();
            }

            while (mRectangles.Count < timesCount)
            {
                Rectangle rectangle = new Rectangle();
                rectangle.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                rectangle.Height = 10;
                rectangle.Width = 10;

                rectangle.SetValue(Canvas.LeftProperty, (double)mRectangles.Count);

                mRectangles.Add(rectangle);
                this.Canvas.Children.Add(rectangle);
            }
            while (mRectangles.Count > timesCount)
            {
                this.Canvas.Children.Remove(mRectangles.Last());
                mRectangles.RemoveAt(mRectangles.Count - 1);
            }
        }

        private void SKElement_PaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = SKColors.Pink;

            canvas.DrawCircle(30, 30, 30, paint);
        }
        #endregion

    }
}
