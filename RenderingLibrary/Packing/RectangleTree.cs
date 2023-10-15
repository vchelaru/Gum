using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace RenderingLibrary.Packing
{
    public class RectangleTree
    {
        public int Width
        {
            get
            {
                return Root.Width;
            }
            set
            {
                Root.Width = value;
            }
        }

        public int Height
        {
            get
            {
                return Root.Height;
            }
            set
            {
                Root.Height = value;
            }
        }

        public RectangleNode Root {  get; private set;}

        public List<RectangleNode> OpenNodes { get; private set; }
        public List<RectangleNode> ClosedNodes { get; private set; }

        public RectangleTree()
        {
            Root = new RectangleNode();
            OpenNodes = new List<RectangleNode>();
            OpenNodes.Add(Root);

            ClosedNodes = new List<RectangleNode>();
        }

        public bool TryInsert(int id, Rectangle rectangle)
        {
            return TryInsert(id, rectangle.Width, rectangle.Height);
        }

        public bool TryInsert(int id, int with, int height)
        {
            for (int i = 0; i < OpenNodes.Count; i++)
            {
                var openNode = OpenNodes[i];

                if (openNode.TryInsert(id, with, height, OpenNodes, ClosedNodes))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
