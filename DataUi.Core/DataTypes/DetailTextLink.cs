using System;

namespace WpfDataUi.DataTypes;

/// <summary>
/// A span of an <see cref="InstanceMember.DetailText"/> that the view draws as a clickable link.
/// </summary>
/// <param name="Text">The exact text inside the detail text that is the link.</param>
/// <param name="Navigate">Runs when the user clicks the link.</param>
public record DetailTextLink(string Text, Action Navigate);
