using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gum.Plugins.AlignmentButtons.CommonControlLogic;

namespace Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels
{
    public class AlignmentViewModel : ViewModel
    {
        public float DockMargin
        {
            get => Get<float>();
            set => Set(value);
        }

        #region Anchor Actions

        public void TopLeftButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft, DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop, DockMargin);

                RefreshAndSave();
            }
        }

        public void TopButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop, DockMargin);

                RefreshAndSave();
            }
        }

        public void TopRightButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight, -DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                    PositionUnitType.PixelsFromTop, DockMargin);

                RefreshAndSave();
            }
        }

        public void MiddleLeftButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    PositionUnitType.PixelsFromLeft, DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                    PositionUnitType.PixelsFromCenterY);

                RefreshAndSave();
            }
        }

        public void MiddleMiddleButton_Click()
        {
            using (UndoManager.Self.RequestLock())
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

        public void MiddleRightButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                    PositionUnitType.PixelsFromRight, -DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                    PositionUnitType.PixelsFromCenterY);

                RefreshAndSave();
            }
        }

        public void BottomLeftButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    PositionUnitType.PixelsFromLeft, DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom, -DockMargin);

                RefreshAndSave();
            }
        }

        public void BottomMiddleButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                    PositionUnitType.PixelsFromCenterX);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom, -DockMargin);

                RefreshAndSave();
            }
        }

        public void BottomRightButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                SetXValues(
                    global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                    PositionUnitType.PixelsFromRight, -DockMargin);

                SetYValues(
                    global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                    PositionUnitType.PixelsFromBottom, -DockMargin);

                RefreshAndSave();
            }
        }

        #endregion

        #region Dock Actions

        public void DockTopButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop, DockMargin);

                SetAndCallReact("Width", -DockMargin * 2, "float");
                SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                RefreshAndSave();
            }
        }

        public void DockLeftButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft, DockMargin);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", -DockMargin * 2, "float");
                SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        public void DockFillButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Width", -DockMargin * 2, "float");
                SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                SetAndCallReact("Height", -DockMargin * 2, "float");
                SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        public void DockRightButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight, -DockMargin);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", -DockMargin * 2, "float");
                SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        public void DockBottomButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {
                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom, -DockMargin);

                SetAndCallReact("Width", -DockMargin * 2, "float");
                SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        public void DockFillVerticallyButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                SetAndCallReact("Height", -DockMargin * 2, "float");
                SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        public void DockFillHorizontallyButton_Click()
        {
            using (UndoManager.Self.RequestLock())
            {

                var state = SelectedState.Self.SelectedStateSave;

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);

                SetAndCallReact("Width", -DockMargin * 2, "float");
                SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                RefreshAndSave();
            }
        }

        #endregion
    }
}
