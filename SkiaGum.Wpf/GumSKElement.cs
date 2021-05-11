using Gum.Wireframe;
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

        //public SemaphoreSlim ExclusiveUiInteractionSemaphor = new SemaphoreSlim(1, 1);

        //float yPushed;
        //bool isWithinThreshold = false;

        //Func<Task> customClickEventToRaise;
        //Func<float, float, Task> customTouchEvent;


        #endregion


        public GumSKElement()
        {
            GumElementsInternal.CollectionChanged += HandleCollectionChanged;
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
                        //bindableGue.BindingContext = this.BindingContext;
                    }

                    break;
            }
        }


        public void Add(BindableGraphicalUiElement toAdd)
        {
            GumElementsInternal.Add(toAdd);
        }

        //protected override void OnBindingContextChanged()
        //{
        //    base.OnBindingContextChanged();

        //    foreach (var element in GumElementsInternal)
        //    {
        //        if (element is BindableGraphicalUiElement bindableGue)
        //        {
        //            bindableGue.BindingContext = this.BindingContext;
        //        }
        //    }
        //}


        protected override void OnPaintSurface(SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            SKImageInfo info = args.Info;

            canvas.Clear();


            GraphicalUiElement.CanvasWidth = info.Width;
            GraphicalUiElement.CanvasHeight = info.Height;

            foreach (var element in GumElementsInternal)
            {
                element.UpdateLayout();
                ((IRenderable)element).Render(canvas);
            }
        }

        public void InvalidateSurface()
        {
            throw new NotImplementedException();
        }
    }
}
