using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Asks the Editor tab's canvas to describe why it may not be drawing, for the unattended run's
/// failure message (#5680). The reply is empty when no canvas answers.
/// </summary>
public sealed class EditorCanvasStateRequestMessage : RequestMessage<string>
{
}
