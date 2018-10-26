using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextureCoordinateSelectionPlugin.Logic
{
    public class ControlLogic : Singleton<ControlLogic>
    {
        public ImageRegionSelectionControl CreateControl()
        {
            var control = new ImageRegionSelectionControl();
            control.AvailableZoomLevels = new int[]
            {
                3200,
                1600,
                1200,
                800,
                500,
                300,
                200,
                150,
                100,
                75,
                50,
                33,
                25,
                10,
            };
            control.StartRegionChanged += HandleStartRegionChanged;
            control.RegionChanged += HandleRegionChanged;
            control.EndRegionChanged += HandleEndRegionChanged;
            GumCommands.Self.GuiCommands.AddWinformsControl(control, "Texture Coordinates", TabLocation.Right);

            return control;
        }

        private void HandleStartRegionChanged(object sender, EventArgs e)
        {
            UndoManager.Self.RecordUndo();
        }

        private void HandleRegionChanged(object sender, EventArgs e)
        {
            var control = sender as ImageRegionSelectionControl;

            var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;

            if (graphicalUiElement != null)
            {
                var selector = control.RectangleSelector;

                graphicalUiElement.TextureLeft = MathFunctions.RoundToInt(selector.Left);
                graphicalUiElement.TextureTop = MathFunctions.RoundToInt(selector.Top);

                graphicalUiElement.TextureWidth = MathFunctions.RoundToInt(selector.Width);
                graphicalUiElement.TextureHeight = MathFunctions.RoundToInt(selector.Height);

                var state = SelectedState.Self.SelectedStateSave;
                var instancePrefix = SelectedState.Self.SelectedInstance?.Name;

                if (!string.IsNullOrEmpty(instancePrefix))
                {
                    instancePrefix += ".";
                }

                state.SetValue($"{instancePrefix}Texture Left", graphicalUiElement.TextureLeft, "int");
                state.SetValue($"{instancePrefix}Texture Top", graphicalUiElement.TextureTop, "int");
                state.SetValue($"{instancePrefix}Texture Width", graphicalUiElement.TextureWidth, "int");
                state.SetValue($"{instancePrefix}Texture Height", graphicalUiElement.TextureHeight, "int");

                GumCommands.Self.GuiCommands.RefreshPropertyGridValues();
            }
        }

        private void HandleEndRegionChanged(object sender, EventArgs e)
        {
            UndoManager.Self.RecordUndo();

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        public void RefreshOutline(ImageRegionSelectionControl control, ref LineRectangle textureOutlineRectangle)
        {
            var shouldShowOutline = control.CurrentTexture != null;
            if (shouldShowOutline)
            {
                if (textureOutlineRectangle == null)
                {
                    textureOutlineRectangle = new LineRectangle(control.SystemManagers);
                    textureOutlineRectangle.IsDotted = false;
                    textureOutlineRectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255, 128);
                    control.SystemManagers.ShapeManager.Add(textureOutlineRectangle);
                }
                textureOutlineRectangle.Width = control.CurrentTexture.Width;
                textureOutlineRectangle.Height = control.CurrentTexture.Height;
                textureOutlineRectangle.Visible = true;
            }
            else
            {
                if (textureOutlineRectangle != null)
                {
                    textureOutlineRectangle.Visible = false;
                }
            }
        }

        public void RefreshSelector(ImageRegionSelectionControl control)
        {
            // early out
            if (control.RectangleSelector != null &&
                control.RectangleSelector.SideGrabbed != FlatRedBall.SpecializedXnaControls.RegionSelection.ResizeSide.None)
            {
                return;
            }

            var shouldClearOut = true;
            if (SelectedState.Self.SelectedStateSave != null)
            {

                var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;
                var rfv = new RecursiveVariableFinder(SelectedState.Self.SelectedStateSave);
                var instancePrefix = SelectedState.Self.SelectedInstance?.Name;

                if (!string.IsNullOrEmpty(instancePrefix))
                {
                    instancePrefix += ".";
                }

                var textureAddress = rfv.GetValue<Gum.Managers.TextureAddress>($"{instancePrefix}Texture Address");
                if (textureAddress == Gum.Managers.TextureAddress.Custom)
                {
                    shouldClearOut = false;
                    control.DesiredSelectorCount = 1;

                    var selector = control.RectangleSelector;


                    selector.Left = rfv.GetValue<int>($"{instancePrefix}Texture Left");
                    selector.Width = rfv.GetValue<int>($"{instancePrefix}Texture Width");

                    selector.Top = rfv.GetValue<int>($"{instancePrefix}Texture Top");
                    selector.Height = rfv.GetValue<int>($"{instancePrefix}Texture Height");

                    selector.Visible = true;
                    selector.ShowHandles = true;
                    selector.ShowMoveCursorWhenOver = true;

                    control.SystemManagers.Renderer.Camera.X =
                        selector.Left + selector.Width / 2.0f;
                    control.SystemManagers.Renderer.Camera.Y =
                        selector.Top + selector.Height / 2.0f;

                }
            }

            if (shouldClearOut)
            {
                control.DesiredSelectorCount = 0;
            }
        }
    }
}
