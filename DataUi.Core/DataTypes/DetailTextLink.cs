using System;

namespace WpfDataUi.DataTypes;

/// <summary>
/// A span of an <see cref="InstanceMember.DetailText"/> that the view draws as a clickable link, or
/// (for <see cref="InstanceMember.DetailAction"/>) an action drawn after the first line.
/// </summary>
/// <param name="Text">The exact text inside the detail text that is the link; for an action, the text shown when no icon is available.</param>
/// <param name="Navigate">Runs when the user clicks the link.</param>
/// <param name="IconName">A head-neutral icon name (such as <c>Delete</c>) drawn instead of <paramref name="Text"/> when the view can supply it; null for a text link.</param>
/// <param name="ToolTip">Tooltip shown while the pointer is over the link; null for none.</param>
public record DetailTextLink(string Text, Action Navigate, string? IconName = null, string? ToolTip = null);
