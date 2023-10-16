using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using System.Windows.Controls;
using static Gum.Plugins.AlignmentButtons.CommonControlLogic;

namespace Gum.Plugins.AlignmentButtons
{
    
    /// <summary>
    /// Interaction logic for AlignmentControl.xaml
    /// </summary>
    public partial class DockControl : UserControl
    {
        public DockControl()
        {
            InitializeComponent();
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using(UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop);

                SetAndCallReact("Width", 0.0f, "float");
                SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                RefreshAndSave();
            }
        }

        private void LeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", 0.0f, "float");
                SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void FillButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Width", 0.0f, "float");
                SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                SetAndCallReact("Height", 0.0f, "float");
                SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void RightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", 0.0f, "float");
                SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void BottomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom);

                SetAndCallReact("Width", 0.0f, "float");
                SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void FillVerticallyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", 0.0f, "float");
                SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void FillHorizontallyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);

                SetAndCallReact("Width", 0.0f, "float");
                SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }
    }
}
