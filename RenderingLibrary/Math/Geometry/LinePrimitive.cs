using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace RenderingLibrary.Math.Geometry
{
    public class LinePrimitive
    {
        #region Fields

        /// <summary>
        /// Determines whether the line is broken up into separate segments or
        /// if it should be treated as one continual line.  This defaults to false.
        /// </summary>
        public bool BreakIntoSegments
        {
            get;
            set;
        }

        Texture2D mTexture;

        /// <summary>
        /// The list of points relative to the LinePrimitive (in object space)
        /// </summary>
        List<Vector2> mVectors;

        /// <summary>
        /// Gets/sets the color of the primitive line object.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Gets/sets the position of the primitive line object.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Gets/sets the render depth of the primitive line object (0 = front, 1 = back)
        /// </summary>
        public float Depth;

        public float LinePixelWidth = 1;

        #endregion

        /// <summary>
        /// Gets the number of vectors which make up the primtive line object.
        /// </summary>
        public int VectorCount
        {
            get
            {
                return mVectors.Count;
            }
        }

        /// <summary>
        /// Creates a new primitive line object.
        /// </summary>
        /// <param name="singlePixelTexture">The texture to use when rendering the line.</param>
        public LinePrimitive(Texture2D singlePixelTexture)
        {
            // create pixels
            mTexture = singlePixelTexture;

            Color = Color.White;
            Position = new Vector2(0, 0);
            Depth = 0;

            mVectors = new List<Vector2>();
        }
        
        /// <summary>
        /// Adds a vector to the LinePrimitive object. The position is relative to the position of the LinePrimitive (object space)
        /// </summary>
        /// <param name="vector">The vector to add.</param>
        public void Add(Vector2 vector)
        {
            mVectors.Add(vector);
        }

        /// <summary>
        /// Adds a vector to the LinePrimitive object.
        /// </summary>
        /// <param name="x">The X position of the new point.</param>
        /// <param name="y">The Y position of the new point.</param>
        public void Add(float x, float y)
        {
            Add(new Vector2(x, y));
        }

        /// <summary>
        /// Insers a vector into the primitive line object.
        /// </summary>
        /// <param name="index">The index to insert it at.</param>
        /// <param name="vector">The vector to insert.</param>
        public void Insert(int index, Vector2 vector)
        {
            mVectors.Insert(index, vector);
        }

        public Vector2 PointAt(int index)
        {
            if (index >= mVectors.Count)
            {
                throw new IndexOutOfRangeException($"index:{index}, count{mVectors.Count}");
            }
            return mVectors[index];
        }

        /// <summary>
        /// Removes a vector from the primitive line object.
        /// </summary>
        /// <param name="vector">The vector to remove.</param>
        public void Remove(Vector2 vector)
        {
            mVectors.Remove(vector);
        }

        /// <summary>
        /// Removes a vector from the primitive line object.
        /// </summary>
        /// <param name="index">The index of the vector to remove.</param>
        public void RemoveAt(int index)
        {
            mVectors.RemoveAt(index);
        }

        /// <summary>
        /// Replaces a vector at the given index with the argument Vector2.
        /// </summary>
        /// <param name="index">What index to replace.</param>
        /// <param name="whatToReplaceWith">The new vector that will be placed at the given index</param>
        public void Replace(int index, Vector2 whatToReplaceWith)
        {
            mVectors[index] = whatToReplaceWith;
        }

        /// <summary>
        /// Clears all vectors from the primitive line object.
        /// </summary>
        public void ClearVectors()
        {
            mVectors.Clear();
        }

        /// <summary>
        /// Renders the primtive line object.
        /// </summary>
        /// <param name="spriteRenderer">The sprite renderer to use to render the primitive line object.</param>
        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            Render(spriteRenderer, managers, mTexture, .2f);
        }

        internal bool IsPointInside(float worldX, float worldY)
        {
            bool b = false;

            for (int i = 0, j = mVectors.Count - 1; i < mVectors.Count; j = i++)
            {
                var atI = mVectors[i] + Position;
                var atJ = mVectors[j] + Position;

                if ((((atI.Y <= worldY) && (worldY < atJ.Y)) || ((atJ.Y <= worldY) && (worldY < atI.Y))) &&
                    (worldX < (atJ.X - atI.X) * (worldY - atI.Y) / (atJ.Y - atI.Y) + atI.X)) b = !b;
            }

            return b;
        }

        internal void SetPointAt(Vector2 point, int index)
        {
            mVectors[index] = point;
        }

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers, Texture2D textureToUse, float repetitionsPerLength)
        {
            if (mVectors.Count < 2)
                return;

            Renderer renderer;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            Vector2 offset = new Vector2(renderer.Camera.RenderingXOffset, renderer.Camera.RenderingYOffset);

            int extraStep = 0;
            if (BreakIntoSegments)
            {
                extraStep = 1;
            }
            for (int i = 1; i < mVectors.Count; i++)
            {
                Vector2 vector1 = mVectors[i - 1];
                Vector2 vector2 = mVectors[i];


                // rotation should be handled in the object creating "this"
                //if(rotation != 0)
                //{
                //    var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotation));

                //    var new1 = vector1.X * matrix.Right + vector1.Y * matrix.Up;
                //    var new2 = vector2.X * matrix.Right + vector2.Y * matrix.Up;

                //    vector1.X = new1.X;
                //    vector1.Y = new1.Y;

                //    vector2.X = new2.X;
                //    vector2.Y = new2.Y;
                //}

                // calculate the distance between the two vectors
                float distance = Vector2.Distance(vector1, vector2);

                int repetitions = (int)(distance * repetitionsPerLength);

                if (repetitions < 1)
                {
                    repetitions = 1;
                }

                //repetitions = 128;

                // calculate the angle between the two vectors
                float angle = (float)System.Math.Atan2((double)(vector2.Y - vector1.Y),
                    (double)(vector2.X - vector1.X));

                Rectangle sourceRectangle = new Rectangle(
                    0, 
                    0, 
                    textureToUse.Width * repetitions, 
                    textureToUse.Height);

                // stretch the pixel between the two vectors
                spriteRenderer.Draw(textureToUse,
                    offset + Position + vector1,
                    sourceRectangle,
                    Color,
                    angle,
                    Vector2.Zero,
                    new Vector2(distance / ((float)repetitions * textureToUse.Width), LinePixelWidth / renderer.CurrentZoom),
                    SpriteEffects.None,
                    Depth,
                    this);

                i += extraStep;
            }
        }

        /// <summary>
        /// Creates a circle starting from 0, 0.
        /// </summary>
        /// <param name="radius">The radius (half the width) of the circle.</param>
        /// <param name="sides">The number of sides on the circle (the more the detailed).</param>
        public void CreateCircle(float radius, int sides)
        {
            mVectors.Clear();

            float max = 2 * (float)System.Math.PI;
            float step = max / (float)sides;

            for (float theta = 0; theta < max; theta += step)
            {
                mVectors.Add(new Vector2(radius * (float)System.Math.Cos((double)theta),
                    radius * (float)System.Math.Sin((double)theta)));
            }

            // then add the first vector again so it's a complete loop
            mVectors.Add(new Vector2(radius * (float)System.Math.Cos(0),
                    radius * (float)System.Math.Sin(0)));
        }

        public void Shift(float x, float y)
        {
            Vector2 shiftAmount = new Vector2(x, y);
            for(int i = 0; i < mVectors.Count; i++)
            {
                mVectors[i] = mVectors[i] + shiftAmount;
            }
        }

    }
}
