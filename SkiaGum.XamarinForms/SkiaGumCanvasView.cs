using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaGum.Managers;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topten.RichTextKit;

namespace SkiaGum
{
    public class SkiaGumCanvasView : SKCanvasView, ISystemManagers
    {
        #region Fields/Properties

        private ObservableCollection<BindableGraphicalUiElement> GumElementsInternal { get; set; } = new ObservableCollection<BindableGraphicalUiElement>();

        public IReadOnlyCollection<BindableGraphicalUiElement> GumElements => GumElementsInternal;

        // this is public to support adding GUE's directly in gencode.
        public ObservableCollection<BindableGraphicalUiElement> Children => GumElementsInternal;

        public SemaphoreSlim ExclusiveUiInteractionSemaphor = new SemaphoreSlim(1, 1);

        float yPushed;
        bool isWithinThreshold = false;

        Func<Task> customClickEventToRaise;
        Func<float, float, Task> customTouchEvent;

        BindableGraphicalUiElement elementPushed;

        #endregion

        public SkiaGumCanvasView()
        {
            GumElementsInternal.CollectionChanged += HandleCollectionChanged;

            base.Touch += HandleTouch;
        }

        #region Touch-related logic

        private async void HandleTouch(object sender, SKTouchEventArgs args)
        {
            // Maybe we need to adjust this for other devices?
            float threshold = (float)20;

            // SkiaSharp views return
            // whether they handle touches
            // through args.Handled. If the 
            // value is false, then control passes
            // from this view to underlying views. Once
            // it is passed, it does not return to this view
            // until a new touch is initiated.
            switch (args.ActionType)
            {
                case SKTouchAction.Pressed:
                    yPushed = args.Location.Y;

                    isWithinThreshold = true;

                    if(customClickEventToRaise != null)
                    {
                        var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                        if (canProceed)
                        {
                            try
                            {
                                await customClickEventToRaise();
                            }
                            finally
                            {
                                ExclusiveUiInteractionSemaphor.Release(1);
                            }
                        }
                    }
                    else
                    {
                        var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                        if (canProceed)
                        {
                            elementPushed = FindClickableElement(args.Location.X, args.Location.Y, GumElementsInternal);
                            if (elementPushed != null)
                            {
                                DarkenElement(elementPushed);
                            }
                            ExclusiveUiInteractionSemaphor.Release(1);
                        }
                    }

                    args.Handled = true;
                    break;
                case SKTouchAction.Moved:
                    if (isWithinThreshold)
                    {
                        if (System.Math.Abs(args.Location.Y - yPushed) > threshold)
                        {
                            isWithinThreshold = false;
                            var whatToLighten = elementPushed;
                            if (whatToLighten != null)
                            {
                                LightenElement(whatToLighten);
                            }
                        }
                    }

                    args.Handled = isWithinThreshold;
                    break;
                case SKTouchAction.Released:
                    {
                        var whatToLighten = elementPushed;
                        if(whatToLighten != null)
                        {
                            LightenElement(whatToLighten);
                        }
                        if(customTouchEvent != null)
                        {
                            var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                            if (canProceed)
                            {
                                try
                                {
                                    await customTouchEvent(args.Location.X, args.Location.Y);
                                }
                                finally
                                {
                                    ExclusiveUiInteractionSemaphor.Release(1);
                                }
                            }
                        }

                        await TryClickOnContainedGumObjects(args.Location.X, args.Location.Y);
                    }


                    break;
            }
        }

        private void LightenElement(GraphicalUiElement whatToLighten)
        {
            if(whatToLighten is ColoredCircleRuntime circleRuntime)
            {
                circleRuntime.IsDimmed = false;
                InvalidateSurface();
            }
        }

        private void DarkenElement(BindableGraphicalUiElement elementPushed)
        {
            if (elementPushed is ColoredCircleRuntime circleRuntime)
            {
                circleRuntime.IsDimmed = true;
                InvalidateSurface();
            }
        }

        private async Task TryClickOnContainedGumObjects(float x, float y)
        {
            var clickableElement = FindClickableElement(x, y, GumElementsInternal);

            if(clickableElement != null)
            {
                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                if (canProceed)
                {
                    try
                    {
                        await clickableElement.ClickedAsync();
                    }
                    finally
                    {
                        ExclusiveUiInteractionSemaphor.Release(1);
                    }
                }
            }
        }

        public void SetClickEvent(Func<Task> eventToRaise)
        {
            customClickEventToRaise = eventToRaise;
            EnableTouchEvents = true;
        }

        public async Task RaiseClickEvent()
        {
            if (customClickEventToRaise != null)
            {
                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                if (canProceed)
                {
                    try
                    {
                        await customClickEventToRaise();
                    }
                    finally
                    {
                        ExclusiveUiInteractionSemaphor.Release(1);
                    }
                }
            }
        }

        public void SetTouchEvent(Func<float, float, Task> eventHandlingXY)
        {
            customTouchEvent = eventHandlingXY;
            EnableTouchEvents = true;
        }

        private BindableGraphicalUiElement FindClickableElement(float x, float y, IList<BindableGraphicalUiElement> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var gumElement = list[i];

                if (gumElement.Visible && gumElement.IsPointInside(x, y))
                {
                    if (gumElement.ClickedAsync != null)
                    {
                        return gumElement;
                    }
                    else
                    {
                        var children = gumElement.Children.Select(item => item as BindableGraphicalUiElement).Where(item => item != null).ToList();

                        var foundElement =  FindClickableElement(x, y, children);

                        if(foundElement != null)
                        {
                            return foundElement;
                        }
                    }
                }
            }
            return null;
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach(var toAdd in e.NewItems)
                    {
                        var bindableGue = toAdd as BindableGraphicalUiElement;

                        bindableGue.AddToManagers(this);
                        bindableGue.BindingContext = this.BindingContext;
                    }

                    break;
            }
        }

        #endregion

        public void Add(BindableGraphicalUiElement toAdd)
        {
            GumElementsInternal.Add(toAdd);
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            foreach (var element in GumElementsInternal)
            {
                if(element is BindableGraphicalUiElement bindableGue)
                {
                    bindableGue.BindingContext = this.BindingContext;
                }
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            SKImageInfo info = args.Info;

            canvas.Clear();


            GraphicalUiElement.CanvasWidth = info.Width;
            GraphicalUiElement.CanvasHeight = info.Height;

            foreach (var element in GumElementsInternal)
            {
                if(element.Visible)
                {
                    element.UpdateLayout();
                    ((IRenderable)element).Render(canvas);
                }
            }
            base.OnPaintSurface(args);
        }

        public GraphicalUiElement GetViewAt(float x, float y)
        {
            var found = Children.FirstOrDefault(item =>
            {
                if (item.Visible)
                {
                    return item.IsPointInside(x, y);
                }
                else
                {
                    return false;
                }
            });

            return found;
        }
    }
}
