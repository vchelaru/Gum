using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Asks the Editor tab's canvas to draw; the reply completes once it has presented that frame.
/// Awaiting it throws when no canvas answers.
/// </summary>
public sealed class EditorCanvasFrameRequestMessage : AsyncRequestMessage<bool>
{
}
