namespace Gum.Logic;

/// <summary>
/// A reference to or from <see cref="MovedInstanceName"/> that was dropped because Create Component moved that
/// instance. <see cref="KeptValue"/> is the stored value the dropped line leaves in place, such as
/// <c>Caption.X = 5</c>, or null when nothing takes its place.
/// </summary>
public record MovedInstanceReference(string MovedInstanceName, string Description, string? KeptValue = null);
