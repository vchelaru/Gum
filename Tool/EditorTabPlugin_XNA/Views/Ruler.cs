using System;
using System.Collections.Generic;
using System.Linq;
using RenderingLibrary;
using XnaAndWinforms;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using InputLibrary;
using WinCursor = System.Windows.Forms.Cursor;
using Sprite = RenderingLibrary.Graphics.Sprite;
using Camera = RenderingLibrary.Camera;
using RenderingLibrary.Math;
using Gum.ToolStates;
using Gum.Input;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views
{
    #region Enums

    public enum RulerSide
    {
        Left,
        Top
    }

    #endregion

    public class Ruler
    {
        #region Fields / Properties
        private ToolFontService _toolFontService;
        private ToolLayerService _toolLayerService;
        private readonly IHotkeyManager _hotkeyManager;
        GraphicsDeviceControl mControl;
        SystemManagers mManagers;
        Cursor mCursor;
        private readonly LayerService _layerService;
        Keyboard mKeyboard;

        SolidRectangle mRectangle;
        List<Line> mRulerLines = new List<Line>();
        List<Line> mGuides = new List<Line>();

        Line mGrabbedGuide;
        Text _grabbedGuideText;

        DistanceArrows DistanceArrow1;
        DistanceArrows DistanceArrow2;

        float mZoomValue = 1;

        Sprite mOffsetSprite;

        int nudgeYOffset;
        int nudgeXOffset;

        RulerSide mRulerSide;


        public RulerSide RulerSide
        {
            get { return mRulerSide; }
            set
            {
                mRulerSide = value;

                ReactToRulerSides();
            }
        }

        public float ZoomValue
        {
            set
            {
                float oldValue = mZoomValue;
                mZoomValue = value;

                //DestroyRulerLines();
                //CreateRulerLines();
                ReactToRulerSides();

                foreach (Line line in mGuides)
                {
                    if (RulerSide == RulerSide.Left)
                    {
                        line.Y *= mZoomValue / oldValue;
                    }
                    else
                    {
                        line.X *= mZoomValue / oldValue;
                    }
                }
            }
        }

        public IEnumerable<float> GuideValues
        {
            get
            {
                if (RulerSide == RulerSide.Left)
                {
                    foreach (Line line in mGuides)
                    {
                        yield return line.Y;
                    }
                }
                else
                {
                    foreach (Line line in mGuides)
                    {
                        yield return line.X;
                    }
                }
            }
            set
            {
                DestroyGuideLines();
                foreach (float position in value)
                {
                    AddGuide(position, GuideLineColor);
                }
            }
        }

        public void SetGuideColors(Color guidelineColor, Color guideTextColor) 
        { 
            GuideLineColor = guidelineColor;
            GuideValues = [.. GuideValues];
            GuideTextColor = guideTextColor;
        }

        Renderer Renderer
        {
            get
            {
                if (mManagers == null)
                {
                    return Renderer.Self;
                }
                else
                {
                    return mManagers.Renderer;
                }
            }
        }

        ShapeManager ShapeManager
        {
            get
            {
                if (mManagers == null)
                {
                    return ShapeManager.Self;
                }
                else
                {
                    return mManagers.ShapeManager;
                }
            }
        }

        TextManager TextManager
        {
            get
            {
                if (mManagers == null)
                {
                    return TextManager.Self;
                }
                else
                {
                    return mManagers.TextManager;
                }
            }
        }

        public bool IsCursorOver
        {
            get;
            private set;
        }

        Color GuideLineColor { get; set; } = Color.FromArgb(127, 255, 255, 255);
        Color GuideTextColor { get; set; } = Color.FromArgb(255, 255, 255, 255);

        bool visible = true;
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                mRectangle.Visible = value;
                foreach (var line in mRulerLines)
                {
                    line.Visible = value;
                }
                foreach (var line in mGuides)
                {
                    line.Visible = value;
                }
                if (_grabbedGuideText != null)
                {
                    _grabbedGuideText.Visible = value;
                }
            }
        }

        #endregion



        public Ruler(GraphicsDeviceControl control, 
            SystemManagers managers, 
            Cursor cursor, 
            ToolFontService toolFontService, 
            ToolLayerService toolLayerService, 
            LayerService layerService,
            IHotkeyManager hotkeyManager)
        {
            _toolFontService = toolFontService;
            _toolLayerService = toolLayerService;
            _hotkeyManager = hotkeyManager;

            mControl = control;
            mManagers = managers;
            mCursor = cursor;

            _layerService = layerService;

            CreateArrows(managers);

            CreateVisualRepresentation();

            // Create the text after the Layer
            CreateGuideText();

            RulerSide = RulerSide.Top;

        }

        private void CreateArrows(SystemManagers managers)
        {
            DistanceArrow1 = new DistanceArrows(managers ?? SystemManagers.Default, _toolFontService, _toolLayerService);
            DistanceArrow1.AddToManagers();
            DistanceArrow2 = new DistanceArrows(managers ?? SystemManagers.Default, _toolFontService, _toolLayerService);
            DistanceArrow2.AddToManagers();

            DistanceArrow1.IsStartArrowTipVisible = false;
            DistanceArrow1.TextHorizontalAlignment = HorizontalAlignment.Center;
            DistanceArrow2.IsStartArrowTipVisible = false;
            DistanceArrow2.TextHorizontalAlignment = HorizontalAlignment.Center;

        }

        public bool HandleXnaUpdate(bool isCursorInWindow)
        {
            IsCursorOver = false;
            UpdateOffsetSpritePosition();

            bool isOver = PerformGuidesActivity(isCursorInWindow);

            if (isCursorInWindow)
            {
                isOver |= HandleAddingGuides();
            }
            IsCursorOver = isOver;
            return isOver;
        }


        private void CreateGuideText()
        {
            _grabbedGuideText = new Text(mManagers, "");
            _grabbedGuideText.RenderBoundary = false;
            _grabbedGuideText.Parent = mOffsetSprite;
            _grabbedGuideText.BitmapFont = _toolFontService.ToolFont;
            TextManager.Add(_grabbedGuideText, _layerService.RulerLayer);
        }

        private void CreateVisualRepresentation()
        {
            mOffsetSprite = new Sprite(null);
            mOffsetSprite.Name = "Ruler offset sprite";

            mRectangle = new SolidRectangle();
            mRectangle.Color = Color.Empty;
            ShapeManager.Add(mRectangle, _layerService.RulerLayer);

            ReactToRulerSides();

            CreateRulerLines();
        }

        public void DestroyRulerLines()
        {
            foreach (var line in mRulerLines)
            {
                ShapeManager.Remove(line);
            }
            // Do we want to remove this?
        }

        public void DestroyGuideLines()
        {
            foreach (var line in mGuides)
            {
                ShapeManager.Remove(line);
            }
            mGuides.Clear();
        }


        private void CreateRulerLines()
        {
            CreateRulerLine(0, 10, Color.LightGray);

            for (int i = 1; i < 100; i++)
            {
                float y = i * 10 * mZoomValue;
                bool isLong = i % 5 == 0;
                float length;
                if (isLong)
                {
                    length = 8;
                }
                else
                {
                    length = 5;
                }

                CreateRulerLine(y, length, Color.LightGray);
            }
            for (int i = 1; i < 100; i++)
            {
                float y = -i * 10 * mZoomValue;
                bool isLong = i % 5 == 0;
                float length;
                if (isLong)
                {
                    length = 8;
                }
                else
                {
                    length = 5;
                }

                CreateRulerLine(y, length, Color.LightGray);
            }
        }

        private void CreateRulerLine(float y, float length, Color color)
        {
            Line line = new Line(mManagers);
            line.X = 10 - length;
            line.Y = MathFunctions.RoundToInt(y) + .5f;


            line.RelativePoint = new Vector2(length, 0);

            line.Color = color;
            line.Z = 1;

            line.Parent = mOffsetSprite;
            mRulerLines.Add(line);
            ShapeManager.Add(line, _layerService.RulerLayer);
        }

        private bool PerformGuidesActivity(bool isCursorInWindow)
        {

            float guideSpacePosition;
            if (RulerSide == RulerSide.Left)
            {
                guideSpacePosition = mCursor.Y - mOffsetSprite.Y + nudgeYOffset;
            }
            else
            {
                guideSpacePosition = mCursor.X - mOffsetSprite.X + nudgeXOffset;
            }


            //guideSpaceY; ;

            Line guideOver = null;
            if (mGrabbedGuide == null && isCursorInWindow)
            {
                foreach (Line line in mGuides)
                {
                    if (RulerSide == RulerSide.Left && Math.Abs(line.Y - guideSpacePosition) < 3 ||
                        RulerSide == RulerSide.Top && Math.Abs(line.X - guideSpacePosition) < 3)
                    {
                        guideOver = line;
                        break;
                    }

                }
            }

            if (guideOver != null || mGrabbedGuide != null)
            {
                WinCursor cursorToSet;
                if (RulerSide == RulerSide.Left)
                {
                    cursorToSet = System.Windows.Forms.Cursors.SizeNS;
                }
                else // top
                {
                    cursorToSet = System.Windows.Forms.Cursors.SizeWE;
                }

                mCursor.SetWinformsCursor(cursorToSet);
            }

            // Remove the guide if it is right-clicked
            if (mCursor.IsInWindow && mCursor.SecondaryPush && guideOver != null)
            {
                mGuides.Remove(guideOver);
                ShapeManager.Remove(guideOver);
            }

            if (mCursor.IsInWindow && mCursor.PrimaryPush)
            {
                mGrabbedGuide = guideOver;
                nudgeXOffset = 0;
                nudgeYOffset = 0;

            }
            if (mCursor.PrimaryDown && mGrabbedGuide != null)
            {

                if (RulerSide == RulerSide.Left)
                {
                    if (_hotkeyManager.NudgeUp.IsPressedInControl())
                    {
                        nudgeYOffset--;
                    }
                    else if (_hotkeyManager.NudgeDown.IsPressedInControl())
                    {
                        nudgeYOffset++;
                    }
                    mGrabbedGuide.Y = guideSpacePosition;
                }
                else
                {
                    if (_hotkeyManager.NudgeLeft.IsPressedInControl())
                    {
                        nudgeXOffset--;
                    }
                    else if (_hotkeyManager.NudgeRight.IsPressedInControl())
                    {
                        nudgeXOffset++;
                    }
                    mGrabbedGuide.X = guideSpacePosition;
                }
            }

            UpdateGrabbedGuideText(GuideTextColor);

            //mScreenSpaceLayer.LayerCameraSettings.Zoom = mManagers.Renderer.Camera.Zoom;

            UpdateDistanceArrows(GuideLineColor, GuideTextColor);

            if (!mCursor.PrimaryDown)
            {
                if (mGrabbedGuide != null && !isCursorInWindow)
                {
                    mGuides.Remove(mGrabbedGuide);
                    ShapeManager.Remove(mGrabbedGuide);
                }
                nudgeXOffset = 0;
                nudgeYOffset = 0;
                mGrabbedGuide = null;
            }

            return guideOver != null || mGrabbedGuide != null;
        }

        private void UpdateDistanceArrows(Color guideLineColor, Color guideTextColor)
        {
            DistanceArrow1.Visible = false;
            DistanceArrow2.Visible = false;
            DistanceArrow1.Zoom = mZoomValue;
            DistanceArrow2.Zoom = mZoomValue;


            if (mCursor.PrimaryDown && mGrabbedGuide != null)
            {
                DistanceArrow1.TextColor = guideTextColor;
                DistanceArrow1.ArrowColor = guideLineColor;

                DistanceArrow2.TextColor = guideTextColor;
                DistanceArrow2.ArrowColor = guideLineColor;

                if (RulerSide == RulerSide.Left)
                {
                    var guideAbove = mGuides.OrderBy(item => item.Y).Where(item => item.RelativePoint.Y == 0 && item.Y < mGrabbedGuide.Y).LastOrDefault();
                    var guideBelow = mGuides.OrderBy(item => item.Y).Where(item => item.RelativePoint.Y == 0 && item.Y > mGrabbedGuide.Y).FirstOrDefault();

                    var yAbove = guideAbove?.Y / mZoomValue;
                    if (yAbove == null && mGrabbedGuide.Y / mZoomValue > GraphicalUiElement.CanvasHeight)
                    {
                        yAbove = GraphicalUiElement.CanvasHeight;
                    }
                    if (yAbove == null && mGrabbedGuide.Y > 0)
                    {
                        yAbove = 0;
                    }

                    var yBelow = guideBelow?.Y / mZoomValue;
                    if (yBelow == null && mGrabbedGuide.Y < 0)
                    {
                        yBelow = 0;
                    }
                    if (yBelow == null && mGrabbedGuide.Y / mZoomValue < GraphicalUiElement.CanvasHeight)
                    {
                        yBelow = GraphicalUiElement.CanvasHeight;
                    }

                    var x = mCursor.GetWorldX();

                    if (yAbove != null)
                    {
                        DistanceArrow1.Visible = true;
                        DistanceArrow1.SetFrom(new Vector2(x, mGrabbedGuide.Y / mZoomValue), new Vector2(x, yAbove.Value));
                    }

                    if (yBelow != null)
                    {
                        DistanceArrow2.Visible = true;
                        DistanceArrow2.SetFrom(new Vector2(x, mGrabbedGuide.Y / mZoomValue), new Vector2(x, yBelow.Value));
                    }
                }
                else // ruler is the top ruller, so do this on the X
                {
                    var guideLeft = mGuides.OrderBy(item => item.X).Where(item => item.RelativePoint.X == 0 && item.X < mGrabbedGuide.X).LastOrDefault();
                    var guideRight = mGuides.OrderBy(item => item.X).Where(item => item.RelativePoint.X == 0 && item.X > mGrabbedGuide.X).FirstOrDefault();

                    var xLeft = guideLeft?.X / mZoomValue;
                    if (xLeft == null && mGrabbedGuide.X / mZoomValue > GraphicalUiElement.CanvasWidth)
                    {
                        xLeft = GraphicalUiElement.CanvasWidth;
                    }
                    if (xLeft == null && mGrabbedGuide.X / mZoomValue > 0)
                    {
                        xLeft = 0;
                    }

                    var xRight = guideRight?.X / mZoomValue;
                    if (xRight == null && mGrabbedGuide.X / mZoomValue < 0)
                    {
                        xRight = 0;
                    }
                    if (xRight == null && mGrabbedGuide.X / mZoomValue < GraphicalUiElement.CanvasWidth)
                    {
                        xRight = GraphicalUiElement.CanvasWidth;
                    }

                    var y = mCursor.GetWorldY();

                    if (xLeft != null)
                    {
                        DistanceArrow1.Visible = true;
                        DistanceArrow1.SetFrom(new Vector2(mGrabbedGuide.X / mZoomValue, y), new Vector2(xLeft.Value, y));
                    }
                    if (xRight != null)
                    {
                        DistanceArrow2.Visible = true;
                        DistanceArrow2.SetFrom(new Vector2(mGrabbedGuide.X / mZoomValue, y), new Vector2(xRight.Value, y));
                    }
                }
            }


        }

        public float ConvertToPixelBasedCoordinate(float value)
        {
            value *= mZoomValue;
            value = MathFunctions.RoundToInt(value);
            value /= mZoomValue;
            return value;
        }

        private void UpdateGrabbedGuideText(Color guideTextColor)
        {
            // need to make it bigger to support scrollbars
            //const float distanceFromEdge = 10;
            const float distanceFromEdge = 30;
            _grabbedGuideText.Visible = false;
            if (mCursor.PrimaryDown && mGrabbedGuide != null)
            {
                _grabbedGuideText.Visible = true;
                _grabbedGuideText.Color = guideTextColor;
                if (RulerSide == RulerSide.Left)
                {
                    _grabbedGuideText.Y = mGrabbedGuide.Y - 21;
                    _grabbedGuideText.X = Renderer.Camera.ClientWidth - distanceFromEdge - _grabbedGuideText.EffectiveWidth;
                    _grabbedGuideText.RawText = (mGrabbedGuide.Y / mZoomValue).ToString();
                    _grabbedGuideText.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    _grabbedGuideText.Y = Renderer.Camera.ClientHeight - distanceFromEdge - 22;
                    _grabbedGuideText.X = mGrabbedGuide.X + 4;
                    _grabbedGuideText.RawText = (mGrabbedGuide.X / mZoomValue).ToString();
                    _grabbedGuideText.HorizontalAlignment = HorizontalAlignment.Left;

                }
            }
        }

        private bool HandleAddingGuides()
        {
            bool toReturn = false;
            float x = mCursor.X;
            float y = mCursor.Y;

            if (mCursor.PrimaryClick)
            {
                if (x > mRectangle.X && x < mRectangle.X + mRectangle.Width &&
                    y > mRectangle.Y && y < mRectangle.Y + mRectangle.Height)
                {
                    AddGuide(x, y, GuideLineColor);
                    toReturn = true;
                }
            }

            return toReturn;
        }

        private void UpdateOffsetSpritePosition()
        {
            //float whereCameraShouldBe = mManagers.Renderer.Camera.ClientHeight / (2.0f );
            //float whereCameraIs = mManagers.Renderer.Camera.Y;

            //float difference = whereCameraIs - whereCameraShouldBe;


            //mOffsetSprite.Y = -difference ;

            Camera camera = Renderer.Camera;

            if (RulerSide == RulerSide.Left)
            {
                float halfResolutionHeight = camera.ClientHeight / 2.0f;
                if (camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft)
                {
                    halfResolutionHeight = 0;
                }
                mOffsetSprite.X = 0;
                mOffsetSprite.Y = MathFunctions.RoundToInt(
                    -camera.Y * mZoomValue + halfResolutionHeight);
            }
            else // top
            {
                float halfResolutionWidth = camera.ClientWidth / 2.0f;
                if (camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft)
                {
                    halfResolutionWidth = 0;
                }
                mOffsetSprite.Y = 0;
                mOffsetSprite.X = MathFunctions.RoundToInt(
                    -camera.X * mZoomValue + halfResolutionWidth);
            }
        }

        private void AddGuide(float x, float y, Color guideColor)
        {
            float relevantValue;
            if (RulerSide == RulerSide.Left)
            {
                relevantValue = y - mOffsetSprite.Y;
            }
            else// if (this.RulerSide == Wireframe.RulerSide.Top)
            {
                relevantValue = x - mOffsetSprite.X;
            }
            AddGuide(relevantValue, guideColor);
        }

        private void AddGuide(float relevantValue, Color guideColor)
        {

            Line line = new Line(mManagers);

            if (RulerSide == RulerSide.Left)
            {
                line.X = 10;
                line.Y = relevantValue;
                line.RelativePoint = new Vector2(6000, 0);
            }
            else if (RulerSide == RulerSide.Top)
            {
                line.Y = 10;
                line.X = relevantValue;
                line.RelativePoint = new Vector2(0, 6000);
            }
            line.Color = guideColor;
            line.Z = 2;

            line.Parent = mOffsetSprite;
            mGuides.Add(line);
            ShapeManager.Add(line, _layerService.RulerLayer);
        }

        private void ReactToRulerSides()
        {
            int countOnEachSide = (mRulerLines.Count - 1) / 2;

            for (int i = 0; i < mRulerLines.Count; i++)
            {
                if (i < countOnEachSide)
                {
                    mRulerLines[i].Color = Color.LightGray;
                }
                else if (i == countOnEachSide)
                {
                    mRulerLines[i].Color = Color.LightGray;
                }
                else
                {
                    mRulerLines[i].Color = Color.LightGray;
                }
            }


            if (RulerSide == RulerSide.Left)
            {
                mRectangle.Width = 10;
                mRectangle.Height = 4000;

                for (int i = 0; i < mRulerLines.Count; i++)
                {
                    float y = (countOnEachSide - i) * mZoomValue * 10;
                    mRulerLines[i].Y = y;
                    float length = GetMethodForIndex(i);

                    mRulerLines[i].X = 10 - length;

                    mRulerLines[i].RelativePoint = new Vector2(length, 0);
                }
            }
            else if (RulerSide == RulerSide.Top)
            {
                mRectangle.Width = 4000;
                mRectangle.Height = 10;

                for (int i = 0; i < mRulerLines.Count; i++)
                {
                    float x = (countOnEachSide - i) * mZoomValue * 10;
                    mRulerLines[i].X = x;
                    float length = GetMethodForIndex(i);

                    mRulerLines[i].Y = 10 - length;

                    mRulerLines[i].RelativePoint = new Vector2(0, length);
                }
            }
        }

        private static float GetMethodForIndex(int i)
        {
            float length;
            if (i % 2 == 1)
            {
                length = 8;
            }
            else
            {
                length = 4;
            }
            return length;
        }
    }
}
