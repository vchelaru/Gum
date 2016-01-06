using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
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

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AnchorControl.xaml
    /// </summary>
    public partial class AnchorControl : UserControl
    {
        public AnchorControl()
        {
            InitializeComponent();
        }

        private void TopLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    PositionUnitType.PixelsFromLeft);
                
                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop);

                RefreshAndSave();
            }
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                    PositionUnitType.PixelsFromCenterX);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop);

                RefreshAndSave();
            }
        }

        private void TopRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                    PositionUnitType.PixelsFromRight);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop);

                RefreshAndSave();
            }
        }

        private void MiddleLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    PositionUnitType.PixelsFromLeft);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                    PositionUnitType.PixelsFromCenterY);

                RefreshAndSave();
            }
        }

        private void MiddleMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                    PositionUnitType.PixelsFromCenterX);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                    PositionUnitType.PixelsFromCenterY);

                RefreshAndSave();
            }
        }

        private void MiddleRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                    PositionUnitType.PixelsFromRight);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                    PositionUnitType.PixelsFromCenterY);

                RefreshAndSave();
            }
        }

        private void BottomLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    PositionUnitType.PixelsFromLeft);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom);

                RefreshAndSave();
            }
        }

        private void BottomMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                    PositionUnitType.PixelsFromCenterX);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom);

                RefreshAndSave();
            }
        }

        private void BottomRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                    PositionUnitType.PixelsFromRight);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom);

                RefreshAndSave();
            }
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var instance = SelectedState.Self.SelectedInstance;
            string instancePrefix = instance.Name + ".";

            state.SetValue(instancePrefix + "X", 0.0f, "float");
            state.SetValue(instancePrefix + "X Origin",
                alignment, "HorizontalAlignment");
            state.SetValue(instancePrefix + "X Units",
               xUnits, typeof(Gum.Managers.PositionUnitType).Name);
        }

        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var instance = SelectedState.Self.SelectedInstance;
            string instancePrefix = instance.Name + ".";

            state.SetValue(instancePrefix + "Y", 0.0f, "float");
            state.SetValue(instancePrefix + "Y Origin",
                alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            state.SetValue(instancePrefix + "Y Units",
                yUnits, typeof(PositionUnitType).Name);
        }



        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid();
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
