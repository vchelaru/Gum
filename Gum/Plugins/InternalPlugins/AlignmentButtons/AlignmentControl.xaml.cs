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
    public partial class AlignmentControl : UserControl
    {
        public AlignmentControl()
        {
            InitializeComponent();
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                state.SetValue(instancePrefix + "X", 0.0f, "float");
                state.SetValue(instancePrefix + "X Origin",
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center, "HorizontalAlignment");
                state.SetValue(instancePrefix + "X Units",
                   PositionUnitType.PixelsFromCenterX, typeof(Gum.Managers.PositionUnitType).Name);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                state.SetValue(instancePrefix + "Y", 0.0f, "float");
                state.SetValue(instancePrefix + "Y Origin",
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
                state.SetValue(instancePrefix + "Y Units",
                    PositionUnitType.PixelsFromTop, typeof(PositionUnitType).Name);

                RefreshAndSave();
            }
        }


        private void LeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                if (SelectedState.Self.SelectedInstance != null)
                {
                    var instance = SelectedState.Self.SelectedInstance;

                    var state = SelectedState.Self.SelectedStateSave;

                    string instancePrefix = instance.Name + ".";

                    state.SetValue(instancePrefix + "X", 0.0f, "float");
                    state.SetValue(instancePrefix + "X Origin",
                        global::RenderingLibrary.Graphics.HorizontalAlignment.Left, "HorizontalAlignment");
                    state.SetValue(instancePrefix + "X Units",
                       PositionUnitType.PixelsFromLeft, typeof(Gum.Managers.PositionUnitType).Name);

                    state.SetValue(instancePrefix + "Height", 0.0f, "float");
                    state.SetValue(instancePrefix + "Height Units",
                        DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                    state.SetValue(instancePrefix + "Y", 0.0f, "float");
                    state.SetValue(instancePrefix + "Y Origin",
                        global::RenderingLibrary.Graphics.VerticalAlignment.Center, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
                    state.SetValue(instancePrefix + "Y Units",
                        PositionUnitType.PixelsFromCenterY, typeof(PositionUnitType).Name);

                    RefreshAndSave();
                }
            }
        }

        private void FillButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                state.SetValue(instancePrefix + "X", 0.0f, "float");
                state.SetValue(instancePrefix + "X Origin",
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center, "HorizontalAlignment");
                state.SetValue(instancePrefix + "X Units",
                   PositionUnitType.PixelsFromCenterX, typeof(Gum.Managers.PositionUnitType).Name);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                state.SetValue(instancePrefix + "Height", 0.0f, "float");
                state.SetValue(instancePrefix + "Height Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                state.SetValue(instancePrefix + "Y", 0.0f, "float");
                state.SetValue(instancePrefix + "Y Origin",
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
                state.SetValue(instancePrefix + "Y Units",
                    PositionUnitType.PixelsFromCenterY, typeof(PositionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void RightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                state.SetValue(instancePrefix + "X", 0.0f, "float");
                state.SetValue(instancePrefix + "X Origin",
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right, "HorizontalAlignment");
                state.SetValue(instancePrefix + "X Units",
                   PositionUnitType.PixelsFromRight, typeof(Gum.Managers.PositionUnitType).Name);

                state.SetValue(instancePrefix + "Height", 0.0f, "float");
                state.SetValue(instancePrefix + "Height Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                state.SetValue(instancePrefix + "Y", 0.0f, "float");
                state.SetValue(instancePrefix + "Y Origin",
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
                state.SetValue(instancePrefix + "Y Units",
                    PositionUnitType.PixelsFromCenterY, typeof(PositionUnitType).Name);

                RefreshAndSave();
            }
        }

        private void BottomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                state.SetValue(instancePrefix + "X", 0.0f, "float");
                state.SetValue(instancePrefix + "X Origin",
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center, "HorizontalAlignment");
                state.SetValue(instancePrefix + "X Units",
                   PositionUnitType.PixelsFromCenterX, typeof(Gum.Managers.PositionUnitType).Name);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                state.SetValue(instancePrefix + "Y", 0.0f, "float");
                state.SetValue(instancePrefix + "Y Origin",
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
                state.SetValue(instancePrefix + "Y Units",
                    PositionUnitType.PixelsFromBottom, typeof(PositionUnitType).Name);

                RefreshAndSave();
            }
        }


        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid();
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
