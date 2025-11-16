using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;

namespace SkiaGum.Wpf
{
    public class GumSKElement : SKElement, ISystemManagers
    {

        #region Fields/Properties

        private ObservableCollection<BindableGue> GumElementsInternal { get; set; } = new ObservableCollection<BindableGue>();

        public IReadOnlyCollection<BindableGue> GumElements => GumElementsInternal;

        // this is public to support adding GUE's directly in gencode.
        public ObservableCollection<BindableGue> Children => GumElementsInternal;

        public bool EnableTouchEvents { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public SystemManagers SystemManagers { get; private set; }

        public IRenderer Renderer => SystemManagers.Renderer;

        //Renderer ISystemManagers.Renderer => SystemManagers.Renderer;
        //public SemaphoreSlim ExclusiveUiInteractionSemaphor = new SemaphoreSlim(1, 1);

        //float yPushed;
        //bool isWithinThreshold = false;

        //Func<Task> customClickEventToRaise;
        //Func<float, float, Task> customTouchEvent;


        #endregion


        public GumSKElement()
        {
            GumElementsInternal.CollectionChanged += HandleCollectionChanged;


            SystemManagers = new SystemManagers();
            SystemManagers.Initialize();
            SystemManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            DataContextChanged += HandleDataContextChanged;
        }


        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var toAdd in e.NewItems)
                    {
                        var bindableGue = toAdd as BindableGue;

                        bindableGue.AddToManagers(this);
                        bindableGue.BindingContext = this.DataContext;
                        // Currently SkiaGum SystemManagers != base Gum SystemManagers. Maybe we unify that at some point?
                        (bindableGue as GraphicalUiElement).AddToManagers(SystemManagers, layer:null);
                    }

                    break;
            }
        }


        public void Add(BindableGue toAdd)
        {
            GumElementsInternal.Add(toAdd);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var element in GumElementsInternal)
            {
                if (element is BindableGue bindableGue)
                {
                    bindableGue.BindingContext = this.DataContext;
                }
            }
        }


        protected override void OnPaintSurface(SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            SKImageInfo info = args.Info;

            SystemManagers.Canvas = canvas;

            // Vic says - Not sure why this was written to have a GlobalScale rather than
            // use the camera. Using the camera gives more flexibility and standardizes the
            // syntax across different platforms.

            var camera =
                SystemManagers.Renderer.Camera;

            camera.ClientWidth = info.Width;
            camera.ClientHeight = info.Height;

            GraphicalUiElement.CanvasWidth = info.Width / camera.Zoom;
            GraphicalUiElement.CanvasHeight = info.Height / camera.Zoom;
            //SystemManagers.Renderer.Camera.Zoom = GlobalScale;

            ForceGumLayout();

            IList<IRenderableIpso> castedList = GumElementsInternal.Cast<IRenderableIpso>().ToList();

            SystemManagers.Renderer.Draw(castedList, SystemManagers);

            base.OnPaintSurface(args);
        }

        void ISurfaceInvalidatable.InvalidateSurface() => base.InvalidateVisual();

        public void ForceGumLayout()
        {
            var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;
            GraphicalUiElement.IsAllLayoutSuspended = false;
            foreach (var item in this.GumElementsInternal)
            {
                item.UpdateLayout();
            }
            GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;
        }

        public void InvalidateSurface()
        {
            base.InvalidateVisual();
        }
    }
}
