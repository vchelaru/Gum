using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    public class Layer
    {
        #region Fields

        List<IRenderable> mRenderables = new List<IRenderable>();

        ReadOnlyCollection<IRenderable> mRenderablesReadOnly;

        #endregion

        public IPositionedSizedObject ScissorIpso { get; set; }

        public LayerCameraSettings LayerCameraSettings
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public ReadOnlyCollection<IRenderable> Renderables
        {
            get
            {
                return mRenderablesReadOnly;
            }
        }

        internal List<IRenderable> RenderablesWriteable
        {
            get
            {
                return mRenderables;
            }
        }

        public Layer()
        {
            mRenderablesReadOnly = new ReadOnlyCollection<IRenderable>(mRenderables);
        }

        internal void Add(IRenderable renderable)
        {
            lock (mRenderables)
            {
                mRenderables.Add(renderable);
            }
        }

        internal void Remove(IRenderable renderable)
        {
            mRenderables.Remove(renderable);
        }

        /// <summary>
        /// This is a stable sort on Z.  It's incredibly fast on already-sorted lists so we'll do this over something like the built-in 
        /// binary sorts that .NET offers.
        /// </summary>
        internal void SortRenderables()
        {
            if (mRenderables.Count == 1 || mRenderables.Count == 0)
                return;

            int whereObjectBelongs;

            for (int i = 1; i < mRenderables.Count; i++)
            {
                if ((mRenderables[i]).Z < (mRenderables[i - 1]).Z)
                {
                    if (i == 1)
                    {
                        mRenderables.Insert(0, mRenderables[i]);
                        mRenderables.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if ((mRenderables[i]).Z >= (mRenderables[whereObjectBelongs]).Z)
                        {
                            mRenderables.Insert(whereObjectBelongs + 1, mRenderables[i]);
                            mRenderables.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && (mRenderables[i]).Z < (mRenderables[0]).Z)
                        {
                            mRenderables.Insert(0, mRenderables[i]);
                            mRenderables.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return Name + " : " + mRenderables.Count + " IRenderables";
        }

        public bool ContainsRenderable(IRenderable whatToTest)
        {
            if (this.Renderables.Contains(whatToTest))
            {
                return true;
            }

            foreach (IRenderable renderable in this.Renderables)
            {
                if (renderable is SortableLayer)
                {
                    if (((SortableLayer)renderable).ContainsRenderable(whatToTest))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal Microsoft.Xna.Framework.Rectangle GetScissorRectangleFor(Camera camera)
        {
            var ipso = ScissorIpso;

            int left = global::RenderingLibrary.Math.MathFunctions.RoundToInt(ipso.GetAbsoluteLeft() - camera.AbsoluteLeft);
            int right = global::RenderingLibrary.Math.MathFunctions.RoundToInt(ipso.GetAbsoluteRight() - camera.AbsoluteLeft);
            int top = global::RenderingLibrary.Math.MathFunctions.RoundToInt(ipso.GetAbsoluteTop() - camera.AbsoluteTop);
            int bottom = global::RenderingLibrary.Math.MathFunctions.RoundToInt(ipso.GetAbsoluteBottom() - camera.AbsoluteTop);

            left = System.Math.Max(0, left);
            top = System.Math.Max(0, top);
            right = System.Math.Max(0, right);
            bottom = System.Math.Max(0, bottom);

            left = System.Math.Min(left, camera.ClientWidth);
            right = System.Math.Min(right, camera.ClientWidth);

            top = System.Math.Min(top, camera.ClientHeight);
            bottom = System.Math.Min(bottom, camera.ClientHeight);

            int width = right - left;
            int height = bottom - top;

            return new Microsoft.Xna.Framework.Rectangle(
                left,
                top,
                width,
                height);
        }
    }
}
