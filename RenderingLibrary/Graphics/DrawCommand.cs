namespace RenderingLibrary.Graphics;

/// <summary>
/// The kind of operation a <see cref="DrawCommand"/> instructs the renderer to perform.
/// </summary>
public enum DrawCommandKind
{
    /// <summary>Render the <see cref="DrawCommand.Target"/> renderable.</summary>
    DrawRenderable,
    /// <summary>Enter the scissor / clip scope established by <see cref="DrawCommand.Target"/>.</summary>
    BeginClip,
    /// <summary>Exit the scissor / clip scope established by <see cref="DrawCommand.Target"/>.</summary>
    EndClip,
}

/// <summary>
/// One entry in the flat command list produced by <see cref="IRenderableOrderer.BuildDrawList"/>
/// and consumed by <c>Renderer.Submit</c>. Splits the main-pass render path into a build phase
/// (tree walk produces the list) and a submit phase (renderer iterates the list).
/// </summary>
public readonly struct DrawCommand
{
    /// <summary>Initializes a new draw command.</summary>
    public DrawCommand(DrawCommandKind kind, IRenderableIpso target)
    {
        Kind = kind;
        Target = target;
    }

    /// <summary>The kind of operation to perform.</summary>
    public DrawCommandKind Kind { get; }

    /// <summary>The renderable this command targets.</summary>
    public IRenderableIpso Target { get; }
}
