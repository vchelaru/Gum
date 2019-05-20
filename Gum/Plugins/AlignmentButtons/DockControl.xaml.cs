using Gum.DataTypes;
using Gum.Managers;
using Gum.PropertyGridHelpers;
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

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop);

            SetAndCallReact("Width", 0.0f, "float");
            SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


            RefreshAndSave();
        }

        private void LeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            SetAndCallReact("Height", 0.0f, "float");
            SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void FillButton_Click(object sender, System.Windows.RoutedEventArgs e)
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

        private void RightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            SetAndCallReact("Height", 0.0f, "float");
            SetAndCallReact("Height Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void BottomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = SelectedState.Self.SelectedStateSave;

            SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom);

            SetAndCallReact("Width", 0.0f, "float");
            SetAndCallReact("Width Units", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

            RefreshAndSave();
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            SetAndCallReact("X", 0.0f, "float");
            SetAndCallReact("X Origin", alignment, "HorizontalAlignment");
            SetAndCallReact("X Units", xUnits, typeof(Gum.Managers.PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                SetAndCallReact("HorizontalAlignment", alignment, "HorizontalAlignment");
            }

        }


        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;

            SetAndCallReact("Y", 0.0f, "float");
            SetAndCallReact("Y Origin", alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            SetAndCallReact("Y Units", yUnits, typeof(PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                SetAndCallReact("VerticalAlignment", alignment, "VerticalAlignment");
            }

        }



        private void SetAndCallReact(string unqualified, object value, string typeName)
        {
            var instance = SelectedState.Self.SelectedInstance;
            string GetVariablePrefix()
            {
                string prefixInternal = "";
                if (instance != null)
                {
                    prefixInternal = instance.Name + ".";
                }
                return prefixInternal;
            }
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();


            var oldValue = state.GetValue(prefix + unqualified);
            state.SetValue(prefix + unqualified, value, typeName);
            SetVariableLogic.Self.ReactToPropertyValueChanged(unqualified, oldValue, SelectedState.Self.SelectedElement, instance, refresh: false);
        }

        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
