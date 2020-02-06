using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using XnaAndWinforms;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using InputLibrary;

using Cursors = System.Windows.Forms.Cursors;
using WinCursor = System.Windows.Forms.Cursor;
using Sprite = RenderingLibrary.Graphics.Sprite;
using Camera = RenderingLibrary.Camera;
using RenderingLibrary.Math;

namespace Gum.Wireframe
{

    public enum RulerSide
    {
        Left,
        Top
    }

    public class Ruler
    {
        #region Fields

        XnaAndWinforms.GraphicsDeviceControl mControl;
        SystemManagers mManagers;
        Layer mLayer;
        Cursor mCursor;
        Keyboard mKeyboard;

        SolidRectangle mRectangle;
        List<Line> mRulerLines = new List<Line>();
        List<Line> mGuides = new List<Line>();

        Line mGrabbedGuide;
        Text mGrabbedGuideText;
        float mZoomValue = 1;

        Sprite mOffsetSprite;

        int nudgeYOffset;
        int nudgeXOffset;

        RulerSide mRulerSide;

        #endregion

        #region Properties

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
                    if (this.RulerSide == Wireframe.RulerSide.Left)
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
                if (RulerSide == Wireframe.RulerSide.Left)
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
                this.DestroyGuideLines();
                foreach (float position in value)
                {
                    AddGuide(position);
                }
            }
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

        #endregion


        public Ruler(GraphicsDeviceControl control, SystemManagers managers, Cursor cursor, InputLibrary.Keyboard keyboard )
        {
            try
            {
                mControl = control;
                mKeyboard = keyboard;
                mManagers = managers;
                mCursor = cursor;

                CreateLayer();

                CreateVisualRepresentation();

                // Create the text after the Layer
                CreateGuideText();

                RulerSide = Wireframe.RulerSide.Top;
            }
            catch(Exception e)
            {
                throw e;
            }
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
            mGrabbedGuideText = new Text(mManagers, "");
            mGrabbedGuideText.RenderBoundary = false;
            mGrabbedGuideText.Parent = mOffsetSprite;
            TextManager.Add(mGrabbedGuideText, mLayer);
        }

        private void CreateVisualRepresentation()
        {
            mOffsetSprite = new Sprite(null);
            mOffsetSprite.Name = "Ruler offset sprite";

            mRectangle = new SolidRectangle();
            mRectangle.Color = Color.Yellow;
            ShapeManager.Add(mRectangle, mLayer);

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
            CreateRulerLine(0, 10, Color.Black);

            for (int i = 1; i < 100; i++)
            {
                float y = i * 10 * mZoomValue;
                bool isLong = (i % 5) == 0;
                float length;
                if (isLong)
                {
                    length = 8;
                }
                else
                {
                    length = 5;
                }

                CreateRulerLine(y, length, Color.DarkRed);
            }
            for (int i = 1; i < 100; i++)
            {
                float y = -i * 10 * mZoomValue;
                bool isLong = (i % 5) == 0;
                float length;
                if (isLong)
                {
                    length = 8;
                }
                else
                {
                    length = 5;
                }

                CreateRulerLine(y, length, Color.DarkGreen);
            }
        }

        private void CreateRulerLine(float y, float length, Color color)
        {
            Line line = new Line(mManagers);
                line.X = 10 - length;
                line.Y = MathFunctions.RoundToInt(y) + .5f;

            
            line.RelativePoint = new Microsoft.Xna.Framework.Vector2(length, 0);

            line.Color = color;
            line.Z = 1;

            line.Parent = mOffsetSprite;
            mRulerLines.Add(line);
            ShapeManager.Add(line, mLayer);
        }


        private void CreateLayer()
        {
            mLayer = Renderer.AddLayer();
            mLayer.LayerCameraSettings = new LayerCameraSettings();
            mLayer.LayerCameraSettings.IsInScreenSpace = true;
            mLayer.Name = "Ruler Layer";
        }

        private bool PerformGuidesActivity(bool isCursorInWindow)
        {

            float guideSpacePosition;
            if (this.RulerSide == Wireframe.RulerSide.Left)
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
                    if ((this.RulerSide == Wireframe.RulerSide.Left && System.Math.Abs(line.Y - guideSpacePosition) < 3) ||
                        (this.RulerSide == Wireframe.RulerSide.Top && System.Math.Abs(line.X - guideSpacePosition) < 3))
                    {
                        guideOver = line;
                        break;
                    }

                }
            }

            if (guideOver != null || mGrabbedGuide != null)
            {
                System.Windows.Forms.Cursor cursorToSet;
                if (this.RulerSide == Wireframe.RulerSide.Left)
                {
                    cursorToSet = System.Windows.Forms.Cursors.SizeNS;
                }
                else // top
                {
                    cursorToSet = System.Windows.Forms.Cursors.SizeWE;
                }

                this.mCursor.SetWinformsCursor(cursorToSet);
            }

            if (mCursor.IsInWindow && mCursor.PrimaryPush)
            {
                mGrabbedGuide = guideOver;
                nudgeXOffset = 0;
                nudgeYOffset = 0;

            }
            if (mCursor.PrimaryDown && mGrabbedGuide != null)
            {

                if (this.RulerSide == Wireframe.RulerSide.Left)
                {
                    if(mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Up))
                    {
                        nudgeYOffset--;
                    }
                    else if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Down))
                    {
                        nudgeYOffset++;
                    }
                    mGrabbedGuide.Y = guideSpacePosition;
                }
                else
                {
                    if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Left))
                    {
                        nudgeXOffset--;
                    }
                    else if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Right))
                    {
                        nudgeXOffset++;
                    }
                    mGrabbedGuide.X = guideSpacePosition;
                }
            }

            UpdateGrabbedGuideText(guideSpacePosition);

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

        public float ConvertToPixelBasedCoordinate(float value)
        {
            value *= this.mZoomValue;
            value = MathFunctions.RoundToInt(value);
            value /= this.mZoomValue;
            return value;
        }

        private void UpdateGrabbedGuideText(float guideSpaceY)
        {
            // need to make it bigger to support scrollbars
            //const float distanceFromEdge = 10;
            const float distanceFromEdge = 30;
            mGrabbedGuideText.Visible = false;
            if (mCursor.PrimaryDown && mGrabbedGuide != null)
            {
                mGrabbedGuideText.Visible = true;

                if (this.RulerSide == Wireframe.RulerSide.Left)
                {
                    mGrabbedGuideText.Y = mGrabbedGuide.Y - 21;
                    mGrabbedGuideText.X = Renderer.Camera.ClientWidth - distanceFromEdge - mGrabbedGuideText.Width;
                    mGrabbedGuideText.RawText = (mGrabbedGuide.Y / mZoomValue).ToString();
                    mGrabbedGuideText.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    mGrabbedGuideText.Y = Renderer.Camera.ClientHeight - distanceFromEdge - 22 ;
                    mGrabbedGuideText.X = mGrabbedGuide.X + 4;
                    mGrabbedGuideText.RawText = (mGrabbedGuide.X / mZoomValue).ToString();
                    mGrabbedGuideText.HorizontalAlignment = HorizontalAlignment.Left;

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
                    AddGuide(x, y);
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

            if (RulerSide == Wireframe.RulerSide.Left)
            {
                float halfResolutionHeight = camera.ClientHeight / (2.0f);
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
                float halfResolutionWidth = camera.ClientWidth / (2.0f);
                if (camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft)
                {
                    halfResolutionWidth = 0;
                } 
                mOffsetSprite.Y = 0;
                mOffsetSprite.X = MathFunctions.RoundToInt(
                    -camera.X * mZoomValue + halfResolutionWidth);
            }
        }

        private void AddGuide(float x, float y)
        {
            float relevantValue;
            if (this.RulerSide == Wireframe.RulerSide.Left)
            {
                relevantValue = y - mOffsetSprite.Y;
            }
            else// if (this.RulerSide == Wireframe.RulerSide.Top)
            {
                relevantValue = x - mOffsetSprite.X;
            }
            AddGuide(relevantValue);
        }

        private void AddGuide(float relevantValue)
        {

            Line line = new Line(mManagers);

            if (this.RulerSide == Wireframe.RulerSide.Left)
            {
                line.X = 10;
                line.Y = relevantValue;
                line.RelativePoint = new Microsoft.Xna.Framework.Vector2(6000, 0);
            }
            else if (this.RulerSide == Wireframe.RulerSide.Top)
            {
                line.Y = 10;
                line.X = relevantValue;
                line.RelativePoint = new Microsoft.Xna.Framework.Vector2(0, 6000);
            }
            line.Color = new Color(1, 1, 1, .5f);
            line.Z = 2;

            line.Parent = mOffsetSprite;
            mGuides.Add(line);
            ShapeManager.Add(line, mLayer);
        }

        private void ReactToRulerSides()
        {
            int countOnEachSide = (mRulerLines.Count - 1) / 2;

            for (int i = 0; i < mRulerLines.Count; i++)
            {
                if (i < countOnEachSide)
                {
                    mRulerLines[i].Color = Color.DarkGreen;
                }
                else if (i == countOnEachSide)
                {
                    mRulerLines[i].Color = Color.Black;
                }
                else
                {
                    mRulerLines[i].Color = Color.DarkRed;
                }
            }


            if (this.RulerSide == Wireframe.RulerSide.Left)
            {
                mRectangle.Width = 10;
                mRectangle.Height = 4000;

                for (int i = 0; i < mRulerLines.Count; i++)
                {
                    float y = (countOnEachSide - i) * mZoomValue * 10;
                    mRulerLines[i].Y = y;
                    float length = GetMethodForIndex(i);

                    mRulerLines[i].X = 10 - length;

                    mRulerLines[i].RelativePoint = 
                        new Microsoft.Xna.Framework.Vector2(length, 0);
                }
            }
            else if (this.RulerSide == Wireframe.RulerSide.Top)
            {
                mRectangle.Width = 4000;
                mRectangle.Height = 10;

                for (int i = 0; i < mRulerLines.Count; i++)
                {
                    float x = (countOnEachSide - i) * mZoomValue * 10;
                    mRulerLines[i].X = x;
                    float length = GetMethodForIndex(i);

                    mRulerLines[i].Y = 10 - length;

                    mRulerLines[i].RelativePoint =
                        new Microsoft.Xna.Framework.Vector2(0, length);
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
