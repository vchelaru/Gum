using System;
using System.Collections.Generic;
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
        float mRangeMinimum;


        public double RangeMaximum
        {
            get { return (double)GetValue(RangeMaximumProperty); }
            set { SetValue(RangeMaximumProperty, value); RefreshRectangles(); }
        }

        // Using a DependencyProperty as the backing store for RangeMaximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RangeMaximumProperty =
            DependencyProperty.Register("RangeMaximum", typeof(double), typeof(TimedStateMarkerDisplay), 
            new PropertyMetadata(1.0, RangePropertyChangeCallback));



        public IEnumerable<double> MarkerItemSource
        {
            get { return (IEnumerable<double>)GetValue(MarkerItemSourceProperty); }
            set { SetValue(MarkerItemSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MarkerItemSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkerItemSourceProperty =
            DependencyProperty.Register("MarkerItemSource", typeof(IEnumerable<double>), 
            typeof(TimedStateMarkerDisplay), new PropertyMetadata(null, MarkerItemSourceChangeCallback));

        

        List<Rectangle> mRectangles = new List<Rectangle>();

        public TimedStateMarkerDisplay()
        {
            InitializeComponent();

            //MarkerItemSource = new List<double> { 0, 0.5, 1.5, 3, 5 };

            //RangeMinimum = 0;
            RangeMaximum = 7;

            this.Canvas.SizeChanged += HandleCanvasSizeChanged;

        }

        private void HandleCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshRectangles();
        }


        private static void RangePropertyChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimedStateMarkerDisplay display = d as TimedStateMarkerDisplay;
            display.RefreshRectangles();
        }


        private static void MarkerItemSourceChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimedStateMarkerDisplay display = d as TimedStateMarkerDisplay;
            display.RefreshRectangles();
        }


        private void RefreshRectangles()
        {
            var timesCount = 0;
            
            if(MarkerItemSource != null)
            {
                timesCount = MarkerItemSource.Count();
            }

            while(mRectangles.Count < timesCount)
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

            int index = 0;

            const float Border = 5;

            if (MarkerItemSource != null)
            {
                foreach (var time in MarkerItemSource)
                {

                    double percentage = 0;

                    if (RangeMaximum != 0)
                    {
                        percentage = time / RangeMaximum;
                    }

                    double width = (Canvas.ActualWidth - Border * 2);

                    var offset = Border + percentage * width;

                    mRectangles[index].SetValue(Canvas.LeftProperty, offset);

                    index++;
                }
            }
        }

    }
}
