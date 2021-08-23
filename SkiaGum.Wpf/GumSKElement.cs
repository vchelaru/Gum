using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaGum.Managers;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;

namespace SkiaGum.Wpf
{
    public class GumSKElement : SKElement, ISystemManagers
    {

        #region Fields/Properties

        private ObservableCollection<BindableGraphicalUiElement> GumElementsInternal { get; set; } = new ObservableCollection<BindableGraphicalUiElement>();

        public IReadOnlyCollection<BindableGraphicalUiElement> GumElements => GumElementsInternal;

        // this is public to support adding GUE's directly in gencode.
        public ObservableCollection<BindableGraphicalUiElement> Children => GumElementsInternal;

        public bool EnableTouchEvents { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        SystemManagers SystemManagers;

        public static float GlobalScale { get; set; } = 1;


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

            DataContextChanged += HandleDataContextChanged;
        }


        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var toAdd in e.NewItems)
                    {
                        var bindableGue = toAdd as BindableGraphicalUiElement;

                        bindableGue.AddToManagers(this);
                        bindableGue.BindingContext = this.DataContext;
                    }

                    break;
            }
        }


        public void Add(BindableGraphicalUiElement toAdd)
        {
            GumElementsInternal.Add(toAdd);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var element in GumElementsInternal)
            {
                if (element is BindableGraphicalUiElement bindableGue)
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

            GraphicalUiElement.CanvasWidth = info.Width / GlobalScale;
            GraphicalUiElement.CanvasHeight = info.Height / GlobalScale;
            SystemManagers.Renderer.Camera.Zoom = GlobalScale;

            SystemManagers.Renderer.Draw(this.GumElementsInternal, SystemManagers);

            base.OnPaintSurface(args);
        }

        public void InvalidateSurface()
        {
            throw new NotImplementedException();
        }
    }
}
