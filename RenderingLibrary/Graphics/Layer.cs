using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Vector3 = System.Numerics.Vector3;
using Matrix = System.Numerics.Matrix4x4;

namespace RenderingLibrary.Graphics
{
    public class Layer
    {
        #region Fields

        List<IRenderableIpso> mRenderables = new List<IRenderableIpso>();

        ReadOnlyCollection<IRenderableIpso> mRenderablesReadOnly;

        #endregion

        #region Properties

        public IRenderableIpso ScissorIpso { get; set; }

        /// <summary>
        /// Contains values which the Layer can use to override the camera settings.
        /// By default this is null, which means the Layer uses the camera settings. 
        /// </summary>
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

        public ReadOnlyCollection<IRenderableIpso> Renderables
        {
            get
            {
                return mRenderablesReadOnly;
            }
        }

        //internal List<IRenderableIpso> RenderablesWriteable
        //{
        //    get
        //    {
        //        return mRenderables;
        //    }
        //}

        public Layer ParentLayer
        {
            get;
            set;
        }

        public bool SecondarySortOnY
        {
            get; set;
        }

        public bool? IsLinearFilteringEnabled { get; set; } = null;

        #endregion

        public Layer()
        {
            mRenderablesReadOnly = new ReadOnlyCollection<IRenderableIpso>(mRenderables);
        }

        public void Add(IRenderableIpso renderable)
        {
            // September 14, 2025
            // Why do we lock here?
            // All UI logic should be
            // on the primary thread. Are
            // we ever doing something on a
            // different thread? This has a cost
            // so let's remove it:
            //lock (mRenderables)
            {
                mRenderables.Add(renderable);
            }
        }

        public void Remove(IRenderableIpso renderable) => mRenderables.Remove(renderable);

        public void Insert(int index, IRenderableIpso renderable) => mRenderables.Insert(index, renderable);

        /// <summary>
        /// This is a stable sort on Z.  It's incredibly fast on already-sorted lists so we'll do this over something like the built-in 
        /// binary sorts that .NET offers.
        /// </summary>
        public void SortRenderables()
        {
            /////////////Early Out//////////////
            if (mRenderables.Count < 2)
                return;
            ///////////End Early Out////////////

            int whereObjectBelongs;

            for (int i = 1; i < mRenderables.Count; i++)
            {
                var atI = mRenderables[i];
                if ((atI).Z < (mRenderables[i - 1]).Z)
                {
                    if (i == 1)
                    {
                        mRenderables.Insert(0, atI);
                        mRenderables.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if (atI.Z >= (mRenderables[whereObjectBelongs]).Z)
                        {
                            mRenderables.Insert(whereObjectBelongs + 1, atI);
                            mRenderables.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && atI.Z < (mRenderables[0]).Z)
                        {
                            mRenderables.Insert(0, atI);
                            mRenderables.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }

            if (SecondarySortOnY)
            {
                for (int i = 1; i < mRenderables.Count; i++)
                {
                    var atI = mRenderables[i];
                    var atIMinus1 = mRenderables[i - 1];

                    var atIAbsoluteY = atI.GetAbsoluteY();

                    if(atI.Z == atIMinus1.Z && atIAbsoluteY < atIMinus1.GetAbsoluteY())
                    {
                        if (i == 1)
                        {
                            mRenderables.Insert(0, atI);
                            mRenderables.RemoveAt(i + 1);
                            continue;
                        }

                        for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                        {
                            if (atI.Z >= (mRenderables[whereObjectBelongs]).Z ||
                                atIAbsoluteY >= (mRenderables[whereObjectBelongs]).GetAbsoluteY())
                            {
                                mRenderables.Insert(whereObjectBelongs + 1, atI);
                                mRenderables.RemoveAt(i + 1);
                                break;
                            }
                            else if (whereObjectBelongs == 0 &&
                                atI.Z < (mRenderables[0]).Z &&
                                atIAbsoluteY < (mRenderables[0]).GetAbsoluteY())
                            {
                                mRenderables.Insert(0, atI);
                                mRenderables.RemoveAt(i + 1);
                                break;
                            }
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

        public void ScreenToWorld(Camera camera, float screenX, float screenY, out float worldX, out float worldY)
        {
            var effectiveZoom = LayerCameraSettings?.Zoom ?? camera.Zoom;

            Matrix transformationMatrix;

            var effectiveCameraX = camera.X;
            var effectiveCameraY = camera.Y;

            if(LayerCameraSettings?.IsInScreenSpace == true)
            {
                effectiveCameraX = 0;
                effectiveCameraY = 0;
            }

            if (camera.CameraCenterOnScreen == RenderingLibrary.CameraCenterOnScreen.Center)
            {
                // make local vars to make stepping in faster if debugging
                var zoom = effectiveZoom;
                var width = camera.ClientWidth;
                var height = camera.ClientHeight;
                transformationMatrix = Camera.GetTransformationMatrix(effectiveCameraX, effectiveCameraY, zoom, width, height, false);
            }
            else
            {
                transformationMatrix =  Matrix.CreateTranslation(-effectiveCameraX, -effectiveCameraY, 0) *
                                         Matrix.CreateScale(new Vector3(effectiveZoom, effectiveZoom, 1));
            }


            Matrix.Invert(transformationMatrix, out var matrix);

            Vector3 position = new Vector3(screenX, screenY, 0);
            Vector3 transformed = Vector3.Transform(position, matrix);

            worldX = transformed.X;
            worldY = transformed.Y;
        }
    }
}
