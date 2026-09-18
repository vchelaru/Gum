using System;
using System.Collections.Generic;
using Gum.Dialogs;

namespace Gum;

/// <summary>Sent once the head's application object has started, before the startup sequence runs.</summary>
public record ApplicationStartupMessage;

/// <summary>
/// Sent when the application is exiting. Recipients register the work they need done (saving
/// layout, window placement) through <see cref="OnTearDown"/>; the head runs the actions in order.
/// </summary>
public class ApplicationTeardownMessage(List<Action> teardownList)
{
    /// <summary>Queues <paramref name="action"/> to run during teardown.</summary>
    public void OnTearDown(Action action) => teardownList.Add(action);
}

/// <summary>Sent whenever the effective theme changes, including once at startup.</summary>
public record ThemeChangedMessage(IEffectiveThemeSettings settings);

/// <summary>
/// Sent when the Performance tab switches the renderer's sibling ordering between depth-first and
/// batch-key grouping, so a running preview can follow the tool (issue #4860).
/// </summary>
public record SiblingOrderingChangedMessage(bool SortByBatchKey);
