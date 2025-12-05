using Gum.Wireframe;
using Microsoft.Maui;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SkiaGum.Maui;

#region TimeSpanExtensionMethods
internal static class TimeSpanExtensionMethods
{
    public enum TimeSpanFormat
    {
        MinutesAndSeconds,
        TimeOfDay,
        TimeOfDayNoMinutes,
        SecondsAndMilliseconds
    }

    public static string ToTimeString(this TimeSpan timeSpan, TimeSpanFormat format = TimeSpanFormat.MinutesAndSeconds)
    {
        switch (format)
        {
            case TimeSpanFormat.MinutesAndSeconds:
                return timeSpan.ToString(@"m\:ss");
            case TimeSpanFormat.TimeOfDay:
                {
                    var time = DateTime.Now.Date + timeSpan;
                    return time.ToString(@"t");

                }
            case TimeSpanFormat.TimeOfDayNoMinutes:
                {
                    var time = DateTime.Now.Date + timeSpan;
                    return time.ToString(@"h tt");
                }
            case TimeSpanFormat.SecondsAndMilliseconds:
                {
                    var time = DateTime.Now.Date + timeSpan;

                    return time.ToString(@"s.fff");
                }

        }
        return string.Empty;
    }

    public static string ToTimeString(this TimeSpan? timeSpan, TimeSpanFormat format = TimeSpanFormat.MinutesAndSeconds)
    {
        if (timeSpan == null)
        {
            return "<null>";
        }
        else
        {
            return ToTimeString(timeSpan.Value, format);
        }
    }
}
#endregion

public class SkiaGumCanvasView : global::SkiaSharp.Views.Maui.Controls.SKCanvasView, ISystemManagers, IDisposable
{
    #region Fields/Properties

    //private ObservableCollection<BindableGue> GumElementsInternal { get; set; } = new ObservableCollection<BindableGue>();

    //public IReadOnlyCollection<BindableGue> GumElements => GumElementsInternal;

    // this is public to support adding GUE's directly in gencode.
    public ObservableCollection<BindableGue> Children { get; private set; } = new ObservableCollectionNoReset<BindableGue>();

    SystemManagers SystemManagers;
    public Renderer Renderer => SystemManagers.Renderer;

    IRenderer ISystemManagers.Renderer => Renderer;

    public SemaphoreSlim ExclusiveUiInteractionSemaphore = new SemaphoreSlim(1, 1);
    public string SemaphoreTag = "GumSemaphore";

    float yPushed;
    bool isWithinThreshold = false;

    Func<Task> customPushEventToRaise;
    Func<Task> customReleaseEventToRaise;

    Func<float, float, Task> customTouchEvent;

    public bool AutoSizeHeightAccordingToContents { get; set; }
    public bool AutoSizeWidthAccordingToContents { get; set; }

    public double InternalPixelHeight => this.Height * DeviceDensity;
    public double InternalPixelWidth => this.Width * DeviceDensity;

    InteractiveGue elementPushed;

    /// <summary>
    /// The scale used when rendering the visuals. This is usually the device density.
    /// Leaving this at 1 will make everything draw to-the-pixel regardles of device density.
    /// </summary>
    public static float GlobalScale { get; set; } = 1;

    // Device density which is used to divide the height and width request 
    public static float DeviceDensity { get; set; } = 1;

    public static event Action<BindableGue> ElementClicked;
    public static event Action<BindableGue> ElementPushed;


    // There seems to be a bug in MAUI .NET 8 (preview) which will not resize a 
    // canvas unless its page is invalidated. This is added here to work around the bug
    public ISurfaceInvalidatable PageContainingThis { get; set; }

    #endregion

    #region Events

    public event Action AfterLayoutBeforeDraw;
    public event Action AfterAutoSizeChanged;

    public event Func<Task<bool>> CanProceedFunc;
    public event Action ReleaseFunc;
    #endregion

    public SkiaGumCanvasView()
    {
        Children.CollectionChanged += HandleCollectionChanged;

        SystemManagers = new SystemManagers();
        SystemManagers.Initialize();

        base.Touch += HandleTouch;
    }


    public async Task<bool> CanProceed()
    {
        if (CanProceedFunc != null)
        {
            return await CanProceedFunc();
        }
        else
        {
            var canProceed = await ExclusiveUiInteractionSemaphore.WaitAsync(0);
            var tag = SemaphoreTag;
            return canProceed;
        }
    }

    void ReleaseSemaphore(int count = 1)
    {
        if (ReleaseFunc != null)
        {
            ReleaseFunc();
        }
        else
        {
            ExclusiveUiInteractionSemaphore.Release(count);
        }
    }

    #region Touch-related Logic

    protected virtual async void HandleTouch(object sender, SKTouchEventArgs args)
    {
        // Maybe we need to adjust this for other devices?
        float threshold = (float)20;

        float touchX = args.Location.X / GlobalScale;
        float touchY = args.Location.Y / GlobalScale;

        var actionType = args.ActionType;

        //args.Handled = await TryHandleTouch(threshold, touchX, touchY, actionType);
    }

    //public async Task<bool> TryHandleTouch(float threshold, float touchX, float touchY, SKTouchAction actionType)
    //{
    //    var wasHandled = false;
    //    // SkiaSharp views return
    //    // whether they handle touches
    //    // through args.Handled. If the 
    //    // value is false, then control passes
    //    // from this view to underlying views. Once
    //    // it is passed, it does not return to this view
    //    // until a new touch is initiated.
    //    switch (actionType)
    //    {
    //        case SKTouchAction.Pressed:
    //            yPushed = touchY;

    //            isWithinThreshold = true;

    //            if (customPushEventToRaise != null)
    //            {
    //                if (await CanProceed())
    //                {
    //                    try
    //                    {
    //                        await customPushEventToRaise();
    //                    }
    //                    finally
    //                    {
    //                        ReleaseSemaphore();
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                if (await CanProceed())
    //                {
    //                    elementPushed = FindElement(touchX, touchY, GumElementsInternal, item => item.Clicked != null || item.DragAsync != null);
    //                    if (elementPushed != null)
    //                    {
    //                        DarkenElement(elementPushed);
    //                    }
    //                    ReleaseSemaphore();
    //                }

    //                await TryPushOnContainedGumObjects(touchX, touchY);
    //            }

    //            wasHandled = true;
    //            break;
    //        case SKTouchAction.Moved:
    //            if (isWithinThreshold)
    //            {
    //                if (System.Math.Abs(touchY - yPushed) > threshold && elementPushed?.DragAsync == null)
    //                {
    //                    isWithinThreshold = false;
    //                    var whatToLighten = elementPushed;
    //                    if (whatToLighten != null)
    //                    {
    //                        LightenElement(whatToLighten);
    //                    }
    //                }

    //                if (isWithinThreshold)
    //                {
    //                    if (elementPushed?.DragAsync != null)
    //                    {
    //                        await elementPushed.DragAsync(touchX, touchY);
    //                    }
    //                    if (elementPushed?.DragOff != null && elementPushed.IsPointInside(touchX, touchY) == false)
    //                    {
    //                        await elementPushed.DragOff();
    //                    }
    //                }
    //            }

    //            wasHandled = isWithinThreshold;
    //            break;
    //        case SKTouchAction.Released:
    //            {
    //                if (customReleaseEventToRaise != null)
    //                {
    //                    if (await CanProceed())
    //                    {
    //                        try
    //                        {
    //                            await customReleaseEventToRaise();
    //                        }
    //                        finally
    //                        {
    //                            ReleaseSemaphore();
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    var whatToLighten = elementPushed;
    //                    if (whatToLighten != null)
    //                    {
    //                        LightenElement(whatToLighten);
    //                    }
    //                    if (customTouchEvent != null)
    //                    {
    //                        if (await CanProceed())
    //                        {
    //                            try
    //                            {
    //                                await customTouchEvent(touchX, touchY);
    //                            }
    //                            finally
    //                            {
    //                                ReleaseSemaphore();
    //                            }
    //                        }
    //                    }


    //                    if (elementPushed?.ReleasedIfPushed != null && await CanProceed())
    //                    {
    //                        try
    //                        {
    //                            await elementPushed?.ReleasedIfPushed();
    //                        }
    //                        finally
    //                        {
    //                            ReleaseSemaphore();
    //                        }
    //                    }


    //                    await TryClickOnContainedGumObjects(touchX, touchY);
    //                }
    //            }


    //            break;
    //    }

    //    return wasHandled;
    ////}

    private void LightenElement(GraphicalUiElement whatToLighten)
    {
        if (whatToLighten is ColoredCircleRuntime circleRuntime)
        {
            circleRuntime.IsDimmed = false;
            InvalidateSurface();
        }
    }

    private void DarkenElement(BindableGue elementPushed)
    {
        if (elementPushed is ColoredCircleRuntime circleRuntime)
        {
            circleRuntime.IsDimmed = true;
            InvalidateSurface();
        }
    }

    BindableGue itemPushed;

    // Made public for auto tests:
    //public async Task TryPushOnContainedGumObjects(float x, float y)
    //{
    //    var clickableElement = FindElement(x, y, GumElementsInternal, item => item.PushedAsync != null);

    //    if (clickableElement != null)
    //    {
    //        await TryPushElement(x, y, clickableElement);
    //    }
    //}

    //public async Task TryClickOnContainedGumObjects(float x, float y)
    //{
    //    var clickableElement = FindElement(x, y, GumElementsInternal, item => item.ClickedAsync != null);

    //    if (clickableElement != null)
    //    {
    //        await TryClickElement(clickableElement);
    //    }
    //}

    //public async Task TryClickElement(BindableGue clickableElement)
    //{
    //    if (await CanProceed())
    //    {
    //        try
    //        {
    //            await clickableElement.ClickedAsync();
    //            ElementClicked?.Invoke(clickableElement);
    //        }
    //        finally
    //        {
    //            ReleaseSemaphore();
    //        }
    //    }
    //}

    //public async Task TryPushElement(float x, float y, BindableGue clickableElement)
    //{
    //    if (await CanProceed())
    //    {
    //        try
    //        {
    //            itemPushed = clickableElement;
    //            await clickableElement.PushedAsync(x, y);
    //            ElementPushed?.Invoke(clickableElement);
    //        }
    //        finally
    //        {
    //            ReleaseSemaphore();
    //        }
    //    }
    //}

    public void SetPushEvent(Func<Task> eventToRaise)
    {
        customPushEventToRaise = eventToRaise;
        EnableTouchEvents = true;
    }

    public void SetReleaseEvent(Func<Task> eventToRaise)
    {
        customReleaseEventToRaise = eventToRaise;
        EnableTouchEvents = true;
    }

    public void SetTouchEvent(Func<float, float, Task> eventHandlingXY)
    {
        customTouchEvent = eventHandlingXY;
        EnableTouchEvents = true;
    }


    //public async Task RaiseClickEvent()
    //{
    //    if (customPushEventToRaise != null || customReleaseEventToRaise != null)
    //    {
    //        if (await CanProceed())
    //        {
    //            try
    //            {
    //                //addbuttonanalyticshere?;
    //                if (customPushEventToRaise != null)
    //                {
    //                    await customPushEventToRaise();
    //                }
    //                else if (customReleaseEventToRaise != null)
    //                {
    //                    await customReleaseEventToRaise();
    //                }
    //            }
    //            finally
    //            {
    //                ReleaseSemaphore();
    //            }
    //        }
    //    }
    //    foreach (var recognizer in this.GestureRecognizers)
    //    {
    //        if (recognizer is TapGestureRecognizer tapGestureRecognizer)
    //        {
    //            //tapGestureRecognizer.SendTapped(this);
    //            // internal void SendTapped(View sender, Func<IElement?, Point?>? getPosition = null)
    //            // Vic says - not sure if we use this outside of testing, but I don't know if this works in MAUI
    //            //typeof(TapGestureRecognizer).GetMethod("SendTapped").Invoke(tapGestureRecognizer, new object[] { this });

    //            var method = typeof(TapGestureRecognizer).GetMethod("SendTapped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //            if (method == null)
    //            {
    //                throw new Exception($"Could not find SendTapped method on TapGestureRecognizer.");
    //            }
    //            method.Invoke(tapGestureRecognizer, new object[] { this, null });
    //        }
    //    }
    //}

    private InteractiveGue FindElement(float x, float y, IList<BindableGue> list, Func<InteractiveGue, bool> condition)
    {
        //for (int i = 0; i < list.Count; i++)
        // Items later in the list appear on top, so we need to test back-to-front
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var gumElement = list[i] as InteractiveGue;

            // Children may sit outside of a container, so we should not restrict children checking on visibility bounds.
            // Yea this makes it slower but it's important for some clicks
            //if (gumElement.Visible && gumElement.IsPointInside(x, y))

            if (gumElement?.Visible == true)
            {
                var passesCondition =
                    (condition == null || condition(gumElement));

                if (passesCondition && gumElement.IsPointInside(x, y))
                {
                    return gumElement as InteractiveGue;
                }
                else
                {
                    var children = gumElement.Children.Select(item => item as BindableGue).Where(item => item != null).ToList();

                    var foundElement = FindElement(x, y, children, condition);

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
                    var bindableGue = toAdd as BindableGue;

                    bindableGue.AddToManagers(this);
                    bindableGue.BindingContext = this.BindingContext;
                }

                break;
        }
    }

    public void AddChild(BindableGue toAdd)
    {
        Children.Add(toAdd);

        //if (toAdd.ClickedAsync != null || toAdd.PushedAsync != null || toAdd.DragAsync != null)
        //{
        //    this.EnableTouchEvents = true;
        //}
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        foreach (var element in Children)
        {
            if (element is BindableGue bindableGue)
            {
                if (!string.IsNullOrEmpty(element.BindingContextBinding))
                {
                    if (BindingContext != null)
                    {
                        var vmProperty = BindingContext.GetType().GetProperty(element.BindingContextBinding);
                        var value = vmProperty.GetValue(BindingContext);

                        bindableGue.BindingContext = value;

                    }

                }
                else
                {
                    bindableGue.BindingContext = this.BindingContext;
                }
            }
        }
    }

    DateTime? lastRecordedTime = null;
    //void RecordTime(string output)
    //{
    //    if (lastRecordedTime == null)
    //    {
    //        lastRecordedTime = DateTime.Now;
    //    }
    //    else
    //    {
    //        var difference = DateTime.Now - lastRecordedTime;
    //        System.Diagnostics.Debug.WriteLine($"   ### ({this.AutomationId}) {output} {difference.ToTimeString(TimeSpanFormat.SecondsAndMilliseconds)}");
    //        lastRecordedTime = DateTime.Now;
    //    }
    //}

    // if true, then this is getting resized due to the control having a change in size. For this, we can skip the layout
    // to improve perofrmance
    bool SkipLayoutOnNextDrawFromDimensionSizeChange = false;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs args)
    {
        // fast here....
        //RecordTime("Start OnPaintSurface");
        var canvas = args.Surface.Canvas;
        SKImageInfo info = args.Info;

        SystemManagers.Canvas = canvas;

        GraphicalUiElement.CanvasWidth = info.Width / GlobalScale;
        GraphicalUiElement.CanvasHeight = info.Height / GlobalScale;
        SystemManagers.Renderer.Camera.Zoom = GlobalScale;

        // fast here

        //RecordTime($"Set Camera Zoom | Layouts Before:{GraphicalUiElement.UpdateLayoutCallCount}");

        if (isLayoutSuppressedByInvalidateSurfaceCall)
        {
            isLayoutSuppressedByInvalidateSurfaceCall = false;
        }
        // For some reason this causes things to have really weird layouts. I don't know why....
        //if(SkipLayoutOnNextDrawFromDimensionSizeChange == false)
        //{
        //    ForceGumLayout();
        //}
        //else
        //{
        //    SkipLayoutOnNextDrawFromDimensionSizeChange = false;
        //}
        // so let's just force it:
        // At some point in the future Vic should investigate this for maximum performance.
        else
        {
            ForceGumLayout();
        }

        // fast

        AfterLayoutBeforeDraw?.Invoke();
        //RecordTime($"Gum Layout | Layouts After:{GraphicalUiElement.UpdateLayoutCallCount}");

        // temporary workaround:
        var renderables = new List<IRenderableIpso>();
        renderables.AddRange(this.Children);
        SystemManagers.Renderer.Draw(renderables, SystemManagers);


        //RecordTime("Draw");
        // slow here
        base.OnPaintSurface(args);

        //RecordTime("Base Paint");



        if (AutoSizeHeightAccordingToContents || AutoSizeWidthAccordingToContents)
        {
            UpdateDimensionsFromAutoSize();
        }

        //RecordTime("Resize...");

    }

    public void UpdateDimensionsFromAutoSize()
    {
        var bottomRight = GetBottomRightMostElementCorner();

        var desiredHeightRequest = bottomRight.Y / DeviceDensity;

        var shouldInvokeEvent = false;

        if (AutoSizeHeightAccordingToContents && this.HeightRequest != desiredHeightRequest)
        {
            SkipLayoutOnNextDrawFromDimensionSizeChange = true;
            HeightRequest = desiredHeightRequest;
            shouldInvokeEvent = true;
        }

        var desiredWidthRequest = bottomRight.X / DeviceDensity;

        if (AutoSizeWidthAccordingToContents && this.WidthRequest != desiredWidthRequest)
        {
            SkipLayoutOnNextDrawFromDimensionSizeChange = true;
            WidthRequest = desiredWidthRequest;
            shouldInvokeEvent = true;
        }

        var totalPixels = desiredWidthRequest * desiredHeightRequest;
        if (totalPixels > 1000 * 1000)
        {
            System.Diagnostics.Debug.WriteLine($"Resize {this.AutomationId} to ({desiredWidthRequest},{desiredHeightRequest})");
        }

        if (shouldInvokeEvent)
        {
            InvalidateSurface();
            var parentIView = this.Parent as IView;
            parentIView?.InvalidateMeasure();
            AfterAutoSizeChanged?.Invoke();
        }
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

    public void ForceGumLayout()
    {
        var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;
        GraphicalUiElement.IsAllLayoutSuspended = false;
        foreach (var item in this.Children)
        {
            item.UpdateLayout(updateParent: false, updateChildren: true);
        }
        GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;
    }

    public Vector2 GetBottomRightMostElementCorner()
    {
        Vector2 bottomRight = Vector2.Zero;

        foreach (var item in this.Children)
        {
            GetBottomRightMostRecursive(item, ref bottomRight);
        }
        return bottomRight;
    }

    private void GetBottomRightMostRecursive(BindableGue gue, ref Vector2 bottomRight)
    {
        if (gue.Visible == false)
        {
            return;
        }
        var right = gue.GetAbsoluteRight();
        var bottom = gue.GetAbsoluteBottom();

        bottomRight.X = Math.Max(right, bottomRight.X);
        bottomRight.Y = Math.Max(bottom, bottomRight.Y);

        if (gue.Children == null)
        {
            foreach (BindableGue item in gue.ContainedElements)
            {
                GetBottomRightMostRecursive(item, ref bottomRight);
            }
        }
        else
        {
            foreach (BindableGue item in gue.Children)
            {
                GetBottomRightMostRecursive(item, ref bottomRight);
            }
        }
    }

    public void SetHeightRequestToContents(bool forceUpdate = true)
    {
        if (forceUpdate)
        {
            ForceGumLayout();
        }

        var requiredSize = GetBottomRightMostElementCorner();
        HeightRequest = requiredSize.Y;
    }

    bool isLayoutSuppressedByInvalidateSurfaceCall;
    public void InvalidateSurface(bool suppressLayout)
    {
        isLayoutSuppressedByInvalidateSurfaceCall = suppressLayout;
        base.InvalidateSurface();
    }

    public new void InvalidateSurface()
    {
        bool shouldForcefullyRefresh = false;
#if IOS
        shouldForcefullyRefresh = AutoSizeHeightAccordingToContents || AutoSizeWidthAccordingToContents;
#endif
        if (shouldForcefullyRefresh)
        {
            this.ForceGumLayout();

            UpdateDimensionsFromAutoSize();
        }

        base.InvalidateSurface();


    }

    public void Dispose()
    {
        foreach(var child in Children)
        {
            if(child.RenderableComponent is IDisposable renderableDisposable)
            {
                renderableDisposable?.Dispose();
            }
        }
    }
}
