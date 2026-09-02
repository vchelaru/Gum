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
            SortByZ(mRenderables, SecondarySortOnY);
        }

        /// <summary>
        /// Stable sort on <see cref="IRenderableIpso.Z"/> (then, when <paramref name="secondarySortOnY"/>
        /// is true, on absolute Y for equal-Z entries), extracted out of <see cref="SortRenderables"/> so
        /// a caller with a flat list of top-level renderables that isn't a <see cref="Layer"/> - the
        /// deferred immediate-mode flush in <c>Renderer.End</c> - can sort the same way before handing
        /// the list to <see cref="IRenderableOrderer.BuildDrawList(IList{IRenderableIpso}, List{DrawCommand}, ClipBoundsSource)"/>.
        /// </summary>
        internal static void SortByZ(List<IRenderableIpso> renderables, bool secondarySortOnY = false)
        {
            /////////////Early Out//////////////
            if (renderables.Count < 2)
                return;
            ///////////End Early Out////////////

            int whereObjectBelongs;

            for (int i = 1; i < renderables.Count; i++)
            {
                var atI = renderables[i];
                if ((atI).Z < (renderables[i - 1]).Z)
                {
                    if (i == 1)
                    {
                        renderables.Insert(0, atI);
                        renderables.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if (atI.Z >= (renderables[whereObjectBelongs]).Z)
                        {
                            renderables.Insert(whereObjectBelongs + 1, atI);
                            renderables.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && atI.Z < (renderables[0]).Z)
                        {
                            renderables.Insert(0, atI);
                            renderables.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }

            if (secondarySortOnY)
            {
                for (int i = 1; i < renderables.Count; i++)
                {
                    var atI = renderables[i];
                    var atIMinus1 = renderables[i - 1];

                    var atIAbsoluteY = atI.GetAbsoluteY();

                    if(atI.Z == atIMinus1.Z && atIAbsoluteY < atIMinus1.GetAbsoluteY())
                    {
                        if (i == 1)
                        {
                            renderables.Insert(0, atI);
                            renderables.RemoveAt(i + 1);
                            continue;
                        }

                        for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                        {
                            if (atI.Z >= (renderables[whereObjectBelongs]).Z ||
                                atIAbsoluteY >= (renderables[whereObjectBelongs]).GetAbsoluteY())
                            {
                                renderables.Insert(whereObjectBelongs + 1, atI);
                                renderables.RemoveAt(i + 1);
                                break;
                            }
                            else if (whereObjectBelongs == 0 &&
                                atI.Z < (renderables[0]).Z &&
                                atIAbsoluteY < (renderables[0]).GetAbsoluteY())
                            {
                                renderables.Insert(0, atI);
                                renderables.RemoveAt(i + 1);
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

        /// <summary>
        /// Resolves the effective camera position and zoom for this layer, factoring in
        /// LayerCameraSettings (position, zoom, IsInScreenSpace) on top of the main camera.
        /// This is the single source of truth for that resolution — ScreenToWorld/WorldToScreen
        /// (hit-testing) and every backend's per-layer draw-time camera transform (rendering)
        /// all route through this so they cannot drift apart (issue #4367).
        /// </summary>
        public void GetEffectiveCamera(Camera camera, out float effectiveCameraX, out float effectiveCameraY, out float effectiveZoom)
        {
            // When IsInScreenSpace is true the main camera is ignored entirely, including its
            // zoom — a screen-space HUD should not scale when the world camera zooms.
            if (LayerCameraSettings?.IsInScreenSpace == true)
            {
                effectiveZoom = LayerCameraSettings.Zoom ?? 1;
            }
            else
            {
                effectiveZoom = LayerCameraSettings?.Zoom ?? camera.Zoom;
            }

            effectiveCameraX = camera.X;
            effectiveCameraY = camera.Y;

            if (LayerCameraSettings?.IsInScreenSpace == true)
            {
                effectiveCameraX = 0;
                effectiveCameraY = 0;
            }

            if (LayerCameraSettings?.Position is System.Numerics.Vector2 layerPosition)
            {
                effectiveCameraX += layerPosition.X;
                effectiveCameraY += layerPosition.Y;
            }
        }

        /// <summary>
        /// Builds the world-to-screen matrix for this layer, factoring in LayerCameraSettings
        /// (position, zoom, IsInScreenSpace) on top of the main camera. Both ScreenToWorld and
        /// WorldToScreen route through this so their behavior cannot drift apart.
        /// </summary>
        private Matrix GetEffectiveTransformationMatrix(Camera camera, out float effectiveZoom)
        {
            GetEffectiveCamera(camera, out float effectiveCameraX, out float effectiveCameraY, out effectiveZoom);

            if (camera.CameraCenterOnScreen == RenderingLibrary.CameraCenterOnScreen.Center)
            {
                return Camera.GetTransformationMatrix(effectiveCameraX, effectiveCameraY, effectiveZoom, camera.ClientWidth, camera.ClientHeight, forRendering: false);
            }
            else
            {
                return Matrix.CreateTranslation(-effectiveCameraX, -effectiveCameraY, 0) *
                       Matrix.CreateScale(new Vector3(effectiveZoom, effectiveZoom, 1));
            }
        }

        public void ScreenToWorld(Camera camera, float screenX, float screenY, out float worldX, out float worldY)
        {
            Matrix transformationMatrix = GetEffectiveTransformationMatrix(camera, out float effectiveZoom);

            Matrix.Invert(transformationMatrix, out var matrix);

            Vector3 position = new Vector3(screenX, screenY, 0);
            Vector3 transformed = Vector3.Transform(position, matrix);

#if FRB
            // FRB handles its own client offsets, so don't update those here, mirroring Camera.ScreenToWorld.
            worldX = transformed.X;
            worldY = transformed.Y;
#else
            worldX = transformed.X - camera.ClientLeft / effectiveZoom;
            worldY = transformed.Y - camera.ClientTop / effectiveZoom;
#endif
        }

        /// <summary>
        /// Converts a world-space point on this layer to screen space using the same effective
        /// camera/layer transform as ScreenToWorld. Use this (instead of Camera.WorldToScreen)
        /// whenever the point lives on a Layer so LayerCameraSettings is honored.
        /// </summary>
        public void WorldToScreen(Camera camera, float worldX, float worldY, out float screenX, out float screenY)
        {
            Matrix transformationMatrix = GetEffectiveTransformationMatrix(camera, out float effectiveZoom);

#if FRB
            Vector3 position = new Vector3(worldX, worldY, 0);
#else
            Vector3 position = new Vector3(worldX + camera.ClientLeft / effectiveZoom, worldY + camera.ClientTop / effectiveZoom, 0);
#endif
            Vector3 transformed = Vector3.Transform(position, transformationMatrix);

            screenX = transformed.X;
            screenY = transformed.Y;
        }
    }
}
