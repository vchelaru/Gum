using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop);

            state.SetValue(prefix + "Width", 0.0f, "float");
            state.SetValue(prefix + "Width Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


            RefreshAndSave();
        }


        private void LeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            state.SetValue(prefix + "Height", 0.0f, "float");
            state.SetValue(prefix + "Height Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void FillButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            state.SetValue(prefix + "Width", 0.0f, "float");
            state.SetValue(prefix + "Width Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            state.SetValue(prefix + "Height", 0.0f, "float");
            state.SetValue(prefix + "Height Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void RightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            state.SetValue(prefix + "Height", 0.0f, "float");
            state.SetValue(prefix + "Height Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void BottomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom);

            state.SetValue(prefix + "Width", 0.0f, "float");
            state.SetValue(prefix + "Width Units",
                DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            state.SetValue(prefix + "X", 0.0f, "float");
            state.SetValue(prefix + "X Origin",
                alignment, "HorizontalAlignment");
            state.SetValue(prefix + "X Units",
               xUnits, typeof(Gum.Managers.PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                state.SetValue(prefix + "HorizontalAlignment", alignment, "HorizontalAlignment");
            }

        }


        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();

            state.SetValue(prefix + "Y", 0.0f, "float");
            state.SetValue(prefix + "Y Origin",
                alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            state.SetValue(prefix + "Y Units",
                yUnits, typeof(PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                state.SetValue(prefix + "VerticalAlignment", alignment, "VerticalAlignment");
            }

        }

        private static string GetVariablePrefix()
        {
            string prefix = "";
            var instance = SelectedState.Self.SelectedInstance;
            if (instance != null)
            {
                prefix = instance.Name + ".";
            }
            return prefix;
        }

        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
