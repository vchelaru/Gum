using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers;
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
        StateSave CurrentState
        {
            get
            {
                if(SelectedState.Self.SelectedStateSave != null)
                {
                    return SelectedState.Self.SelectedStateSave;
                }
                else
                {
                    return SelectedState.Self.SelectedElement?.DefaultState;
                }
            }
        }

        public AnchorControl()
        {
            InitializeComponent();
        }

        private void TopLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);
                
            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void TopRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void MiddleLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void MiddleMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void MiddleRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void BottomLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void BottomMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void BottomRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            SetAndCallReact("X", 0.0f, "float");
            SetAndCallReact("X Origin",
                alignment, "HorizontalAlignment");
            SetAndCallReact("X Units",
               xUnits, typeof(Gum.Managers.PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                SetAndCallReact("HorizontalAlignment", alignment, "HorizontalAlignment");
            }
        }


        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = CurrentState;

            SetAndCallReact("Y", 0.0f, "float");
            SetAndCallReact("Y Origin",
                alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            SetAndCallReact("Y Units",
                yUnits, typeof(PositionUnitType).Name);

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
