using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Packing
{
    public class RectangleNode
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public int ID { get; set; }

        public int Left
        {
            get
            {
                return this.X;
            }
        }

        public int Top
        {
            get
            {
                return this.Y;
            }
        }

        public int Bottom
        {
            get
            {
                return Y + Height;
            }
        }

        public int Right
        {
            get
            {
                return X + Width;
            }
        }

        public RectangleNode Child1 { get; set; }
        public RectangleNode Child2 { get; set; }

        public bool TryInsert(int id, Rectangle rectangle, List<RectangleNode> openNodes, List<RectangleNode> closedNodes)
        {
            return TryInsert(id, rectangle.Width, rectangle.Height, openNodes, closedNodes);
        }

        public bool TryInsert(int id, int width, int height, List<RectangleNode> openNodes, List<RectangleNode> closedNodes)
        {
            bool toReturn = true;

            if(width > this.Width || height > this.Height)
            {
                toReturn = false;
            }
            else
            {
                bool perfectFit = width == this.Width && height == this.Height;

                if(perfectFit)
                {
                    this.ID = id;
                    // don't modify the width/height, perfect fit

                    openNodes.Remove(this);
                    closedNodes.Add(this);
                }
                else
                {

                    openNodes.Remove(this);

                    if(this.Width == width)
                    {
                        Child1 = new RectangleNode();
                        Child1.X = this.X;
                        Child1.Y = this.Y;
                        Child1.Width = this.Width;
                        Child1.Height = height;

                        Child1.ID = id;
                        closedNodes.Add(Child1);

                        Child2 = new RectangleNode();
                        Child2.X = this.X;
                        Child2.Y = this.Y + height;
                        Child2.Width = this.Width;
                        Child2.Height = this.Height - height;

                        openNodes.Add(Child2);

                    }
                    else if(this.Height == height)
                    {
                        Child1 = new RectangleNode();
                        Child1.X = this.X;
                        Child1.Y = this.Y;
                        Child1.Width = width;
                        Child1.Height = this.Height;

                        Child1.ID = id;
                        closedNodes.Add(Child1);

                        Child2 = new RectangleNode();
                        Child2.X = this.X + width;
                        Child2.Y = this.Y;
                        Child2.Width = this.Width - width;
                        Child2.Height = this.Height;

                        openNodes.Add(Child2);

                    }
                    else
                    {
                        // make two children, give one the rectangle
                        int leftoverWidth = this.Width - width;
                        int leftoverHeight = this.Height - height;


                        Child1 = new RectangleNode();
                        Child2 = new RectangleNode();

                        Child1.X = this.X;
                        Child1.Y = this.Y;

                        if(leftoverWidth < leftoverHeight)
                        {
                            // --------------------
                            // |        1         |
                            // --------------------
                            // |                  |
                            // |        2         |
                            // |                  |
                            // --------------------
                            
                            Child1.Width = this.Width;
                            Child1.Height = height;

                            Child2.X = this.X;
                            Child2.Y = this.Y + height;

                            Child2.Width = this.Width;
                            Child2.Height = this.Height - height;
                        }
                        else
                        {
                            // --------------------
                            // |    |             |
                            // |    |             |
                            // |    |             |
                            // |  1 |     2       |
                            // |    |             |
                            // |    |             |
                            // |    |             |
                            // --------------------

                            Child1.Height = this.Height;
                            Child1.Width = width;

                            Child2.X = this.X + width;
                            Child2.Y = this.Y;

                            Child2.Height = this.Height;
                            Child2.Width = this.Width - width;
                        }

                        openNodes.Add(Child1);
                        openNodes.Add(Child2);
                        toReturn = Child1.TryInsert(id, width, height, openNodes, closedNodes);
                    }
                }
            }

            return toReturn;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", X, Y, Width, Height); 
        }
    }
}
