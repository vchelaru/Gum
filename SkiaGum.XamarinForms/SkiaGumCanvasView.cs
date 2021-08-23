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

        SystemManagers SystemManagers;

        public SemaphoreSlim ExclusiveUiInteractionSemaphor = new SemaphoreSlim(1, 1);

        float yPushed;
        bool isWithinThreshold = false;

        Func<Task> customClickEventToRaise;
        Func<float, float, Task> customTouchEvent;

        BindableGraphicalUiElement elementPushed;

        /// <summary>
        /// The scale used when rendering the visuals. This is usually the device density.
        /// Leaving this at 1 will make everything draw to-the-pixel regardles of device density.
        /// </summary>
        public static float GlobalScale { get; set; } = 1;

        #endregion

        public SkiaGumCanvasView()
        {
            GumElementsInternal.CollectionChanged += HandleCollectionChanged;

            SystemManagers = new SystemManagers();
            SystemManagers.Initialize();

            base.Touch += HandleTouch;
        }

        #region Touch-related Logic

        protected virtual async void HandleTouch(object sender, SKTouchEventArgs args)
        {
            // Maybe we need to adjust this for other devices?
            float threshold = (float)20;

            float touchX = args.Location.X / GlobalScale;
            float touchY = args.Location.Y / GlobalScale;



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
                    yPushed = touchY;

                    isWithinThreshold = true;

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
                    else
                    {
                        var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                        if (canProceed)
                        {
                            elementPushed = FindClickableElement(touchX, touchY, GumElementsInternal);
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
                        if (System.Math.Abs(touchY - yPushed) > threshold)
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
                        if (whatToLighten != null)
                        {
                            LightenElement(whatToLighten);
                        }
                        if (customTouchEvent != null)
                        {
                            var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                            if (canProceed)
                            {
                                try
                                {
                                    await customTouchEvent(touchX, touchY);
                                }
                                finally
                                {
                                    ExclusiveUiInteractionSemaphor.Release(1);
                                }
                            }
                        }

                        await TryClickOnContainedGumObjects(touchX, touchY);
                    }


                    break;
            }
        }

        private void LightenElement(GraphicalUiElement whatToLighten)
        {
            if (whatToLighten is ColoredCircleRuntime circleRuntime)
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

            if (clickableElement != null)
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

        public void SetTouchEvent(Func<float, float, Task> eventHandlingXY)
        {
            customTouchEvent = eventHandlingXY;
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

                        var foundElement = FindClickableElement(x, y, children);

                        if (foundElement != null)
                        {
                            return foundElement;
                        }
                    }
                }
            }
            return null;
        }

        public void SimulateSkTouchAction(SKTouchAction action, float x, float y)
        {
            HandleTouch(this, new SKTouchEventArgs(0, action, new SKPoint(x, y), inContact: true));
        }

        #endregion

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var toAdd in e.NewItems)
                    {
                        var bindableGue = toAdd as BindableGraphicalUiElement;

                        bindableGue.AddToManagers(this);
                        bindableGue.BindingContext = this.BindingContext;
                    }

                    break;
            }
        }

        public void Add(BindableGraphicalUiElement toAdd)
        {
            GumElementsInternal.Add(toAdd);
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            foreach (var element in GumElementsInternal)
            {
                if (element is BindableGraphicalUiElement bindableGue)
                {
                    bindableGue.BindingContext = this.BindingContext;
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
