namespace AvaloniaDataUi;

/// <summary>
/// Two members of one category that share a row (such as X and Y). A name matches a member whose
/// <see cref="WpfDataUi.DataTypes.InstanceMember.Name"/> is that name, or ends with it after a dot
/// (<c>Label.X</c> for X).
/// </summary>
/// <param name="First">The member that decides where the shared row sits and is shown on the left.</param>
/// <param name="Second">The member shown on the right.</param>
public sealed record RowPair(string First, string Second);
