using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Diagnostics;
using Gum.Messages;

namespace Gum.Logic;

/// <inheritdoc cref="IAddDestinationTracker"/>
public class AddDestinationTracker : IAddDestinationTracker
{
    private const int LoggedCallerFrames = 12;

    private object? _destination;
    private bool _hasSelectionChangedSinceAnchor;

    public AddDestinationTracker(IMessenger messenger)
    {
        messenger.Register<SelectionChangedMessage>(this, (_, _) => MarkSelectionChanged());
        // A pick on what is already selected changes nothing, but it still names the destination (#7007).
        messenger.Register<SelectionPickedMessage>(this, (_, _) => MarkSelectionChanged());
    }

    /// <inheritdoc/>
    public object? Destination => _destination;

    /// <inheritdoc/>
    public bool HasSelectionChangedSinceAnchor => _hasSelectionChangedSinceAnchor;

    /// <inheritdoc/>
    public void Anchor(object? destination)
    {
        if (ToolLog.IsEnabled && destination != _destination)
        {
            ToolLog.Write("adddest.anchor", detail: destination?.ToString() ?? "(none)");
        }
        _destination = destination;
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void MarkSelectionUnchanged()
    {
        _hasSelectionChangedSinceAnchor = false;
    }

    /// <inheritdoc/>
    public void MarkSelectionChanged()
    {
        // Which code cleared the destination, so a chip click that lands somewhere unexpected can be
        // traced to its sender (#8414).
        if (ToolLog.IsEnabled && _destination != null)
        {
            ToolLog.Write("adddest.clear", detail: $"{_destination} by {DescribeCallers(new StackTrace(), LoggedCallerFrames)}");
        }
        _destination = null;
        _hasSelectionChangedSinceAnchor = true;
    }

    /// <summary>
    /// The innermost <paramref name="maxFrames"/> methods of <paramref name="trace"/> outside this class
    /// and the messenger, as <c>Type.Method &lt;- Type.Method</c>.
    /// </summary>
    internal static string DescribeCallers(StackTrace trace, int maxFrames)
    {
        List<string> names = new List<string>();
        foreach (StackFrame frame in trace.GetFrames())
        {
            if (names.Count >= maxFrames)
            {
                break;
            }
            if (frame.GetMethod() is not MethodBase method || OuterType(method.DeclaringType) is not Type type)
            {
                continue;
            }
            if (type == typeof(AddDestinationTracker) || type.Namespace?.StartsWith("CommunityToolkit", StringComparison.Ordinal) == true)
            {
                continue;
            }
            names.Add($"{type.Name}.{SourceMethodName(method)}");
        }
        return string.Join(" <- ", names);
    }

    /// <summary>An async or iterator method's own name (<c>LoadProject</c>) rather than its state machine's <c>MoveNext</c>.</summary>
    private static string SourceMethodName(MethodBase method)
    {
        string? holder = method.DeclaringType?.Name;
        if (method.Name == "MoveNext" && holder != null && holder.StartsWith('<') && holder.IndexOf('>') is > 1 and int end)
        {
            return holder.Substring(1, end - 1);
        }
        return method.Name;
    }

    /// <summary>The type a lambda or iterator was written in, rather than its compiler-generated holder.</summary>
    private static Type? OuterType(Type? type)
    {
        while (type?.DeclaringType != null && type.IsDefined(typeof(CompilerGeneratedAttribute)))
        {
            type = type.DeclaringType;
        }
        return type;
    }
}
