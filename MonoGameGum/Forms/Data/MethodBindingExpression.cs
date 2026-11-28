using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if !FRB
using Gum.Mvvm;
#endif
using Gum.Wireframe;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

/// <summary>
/// Binds a view model event to a method on the target, so raising the event calls the method. The method may
/// have any visibility and may be inherited. The binding follows the target's BindingContext: it unsubscribes
/// from the outgoing view model and subscribes to the new one.
/// </summary>
internal class MethodBindingExpression : BindingExpressionBase
{
    private readonly IBindingTarget _target;
    private readonly string _methodName;
    private readonly string _path;
    private object? _source;
    private EventInfo? _sourceEvent;
    private Delegate? _handler;
    private bool _hasReportedMissingPath;

    public MethodBindingExpression(IBindingTarget target, string methodName, string path)
    {
        _target = target;
        _methodName = methodName;
        _path = path;
        _hasReportedMissingPath = false;
        _target.BindingContextChanged += OnTargetBindingContextChanged;
    }

    [RequiresUnreferencedCode("Calls " + nameof(AttachToSource) + ".")]
    public void Start() => AttachToSource(_target.BindingContext);

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic relay when an already-bound target's BindingContext changes. The trim risk " +
            "is already surfaced wherever SetBinding was actually called to create this binding.")]
    private void OnTargetBindingContextChanged(object? sender, BindingContextChangedEventArgs e) =>
        AttachToSource(e.NewBindingContext);

    [RequiresUnreferencedCode(
        "Looks up the event on the source's own type by name (GetEvent) and the method on the target's type. " +
        "Those members may be removed under PublishTrimmed if nothing else in the app references them.")]
    private void AttachToSource(object? source)
    {
        DetachFromSource();

        if (source == null)
        {
            return;
        }

        PathSegment[] segments = PathSegmentParser.ParseSegments(_path);
        if (segments.Length > 1 || segments[0].Index.HasValue)
        {
            object? leafOwner = PropertyPathObserver.WalkSegments(source, segments, segments.Length - 1);
            if (leafOwner?.GetType().GetEvent(segments[segments.Length - 1].Name) != null)
            {
                throw new NotSupportedException(
                    $"Dotted binding paths are not supported for event bindings. Got '{_path}'.");
            }
            ThrowNotAProperty();
        }

        EventInfo? sourceEvent = source.GetType().GetEvent(_path);
        if (sourceEvent == null)
        {
            Type sourceType = source.GetType();
            if (sourceType.GetProperty(_path) != null || sourceType.GetField(_path) != null)
            {
                ThrowNotAProperty();
            }
            ReportMissingPathOnce(source);
            return;
        }

        Delegate handler = Delegate.CreateDelegate(sourceEvent.EventHandlerType!, _target, _methodName);
        sourceEvent.AddEventHandler(source, handler);
        _source = source;
        _sourceEvent = sourceEvent;
        _handler = handler;
    }

    // The target name is a method, so a view model value has no property to land on.
    private void ThrowNotAProperty()
    {
        Type targetType = _target.GetType();
        throw new Exception($"The type {targetType} does not have a property {_methodName}. If this property " +
            "exists, make sure that it's not private.");
    }

    [RequiresUnreferencedCode("Calls " + nameof(BinderHelpers.GetMissingPathMessage) + ".")]
    private void ReportMissingPathOnce(object source)
    {
        if (_hasReportedMissingPath)
        {
            return;
        }

        string? message = BinderHelpers.GetMissingPathMessage(source, _path, allowEventLeaf: true,
            _target.GetType().Name, _methodName);
        if (message != null)
        {
            _hasReportedMissingPath = true;
            GumRuntime.ElementSaveExtensions.ReportDiagnostic(message);
        }
    }

    private void DetachFromSource()
    {
        if (_source != null && _sourceEvent != null && _handler != null)
        {
            _sourceEvent.RemoveEventHandler(_source, _handler);
        }
        _source = null;
        _sourceEvent = null;
        _handler = null;
    }

    public override void Dispose()
    {
        _target.BindingContextChanged -= OnTargetBindingContextChanged;
        DetachFromSource();
        base.Dispose();
    }
}
