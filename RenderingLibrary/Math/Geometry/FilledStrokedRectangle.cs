using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;

namespace RenderingLibrary.Math.Geometry;

/// <summary>
/// Flat <see cref="IRenderableIpso"/> that draws a fill and a stroke in one <see cref="Render"/>
/// call, gated independently (<see cref="IsFilled"/> for the fill, <see cref="StrokeWidth"/> &gt; 0
/// for the stroke — neither hides the other). Added for FlatRedBall's Glue codegen (issue #4341),
/// which emits one flat contained field per standard element and cannot adopt
/// <c>RectangleRuntime</c>'s two-slot <c>GraphicalUiElement</c> composite. Reuses the fill and
/// stroke draw techniques directly from <c>Sprite.Render</c> and
/// <see cref="LineRectangle.RenderLinePrimitive"/>/<see cref="LineRectangle.UpdateLinePrimitive"/>
/// (both already generic over any <see cref="IRenderableIpso"/>) rather than reimplementing either,
/// so a fix to either draw path never needs hand-mirroring into a third place. Scope is plain
/// square-corner fill + stroke only — no gradient, dropshadow, dashed stroke, corner radius, or
/// antialiasing; those require the optional <c>Gum.Shapes</c>/<c>MonoGameGumShapes</c> package that
/// <c>RectangleRuntime</c> itself gates behind, which FRB has no integration with.
/// </summary>
public class FilledStrokedRectangle : SpriteBatchRenderableBase, IVisible, IRenderableIpso
{
    #region Fields

    Vector2 Position;

    float mWidth = 32;
    float mHeight = 32;
    float mRotation;

    IRenderableIpso? mParent;
    ObservableCollectionNoReset<IRenderableIpso> mChildren;

    SystemManagers? mManagers;

    LinePrimitive mLinePrimitive;

    Color _fillColor;
    Color _strokeColor;
    float _strokeWidth;

    #endregion

    #region Properties

    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    bool IRenderableIpso.ClipsChildren => false;

    bool IRenderableIpso.IsRenderTarget => false;

    /// <summary>
    /// Unused unless this is ever placed in a render target's texture-blit path (see
    /// <see cref="IRenderableIpso.Alpha"/>'s only reader, <c>Renderer.DrawRenderTargetToScreen</c>),
    /// which requires <see cref="IRenderableIpso.IsRenderTarget"/> — always false here. Reports the
    /// more opaque of the two colors so that reader still degrades sensibly if this type is ever
    /// reused somewhere that path applies.
    /// </summary>
    public int Alpha => System.Math.Max(_fillColor.A, _strokeColor.A);

    /// <summary>
    /// When <c>true</c>, paints a solid fill using <see cref="FillColor"/>. Independent of the
    /// stroke — both may render in the same <see cref="Render"/> call. Defaults to <c>false</c> so
    /// a fresh rectangle is a stroke-only outline, matching <c>RectangleRuntime</c>'s default.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Fill color, used when <see cref="IsFilled"/> is <c>true</c>.
    /// </summary>
    public Color FillColor
    {
        get => _fillColor;
        set => _fillColor = value;
    }

    /// <summary>
    /// Stroke color, used when <see cref="StrokeWidth"/> is &gt; 0.
    /// </summary>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            mLinePrimitive.Color = value;
        }
    }

    /// <summary>
    /// Stroke width in pixels. The stroke only renders when this is &gt; 0 — independent of
    /// <see cref="IsFilled"/>. Defaults to 1 so a fresh rectangle keeps a visible outline.
    /// </summary>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
            mLinePrimitive.LinePixelWidth = value;
        }
    }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public float X
    {
        get => Position.X;
        set => Position.X = value;
    }

    /// <inheritdoc/>
    public float Y
    {
        get => Position.Y;
        set => Position.Y = value;
    }

    /// <inheritdoc/>
    public float Z { get; set; }

    /// <inheritdoc/>
    public float Rotation
    {
        get => mRotation;
        set
        {
            mRotation = value;
            UpdatePoints();
        }
    }

    /// <inheritdoc/>
    public bool FlipHorizontal { get; set; }

    /// <inheritdoc/>
    public float Width
    {
        get => mWidth;
        set
        {
            mWidth = value;
            UpdatePoints();
        }
    }

    /// <inheritdoc/>
    public float Height
    {
        get => mHeight;
        set
        {
            mHeight = value;
            UpdatePoints();
        }
    }

    /// <inheritdoc/>
    public IRenderableIpso? Parent
    {
        get => mParent;
        set
        {
            if (mParent != value)
            {
                mParent?.Children.Remove(this);
                mParent = value;
                mParent?.Children.Add(this);
            }
        }
    }

    /// <inheritdoc/>
    public ObservableCollection<IRenderableIpso> Children => mChildren;

    /// <inheritdoc/>
    public object? Tag { get; set; }

    // Matches LineRectangle's Wrap: the fill pass's single-pixel UV stays within [0,1] regardless
    // (wrap vs. clamp is irrelevant there), but the stroke pass's LinePrimitive can source a
    // texture-repeat rectangle wider than the underlying texture (see LinePrimitive.Render), which
    // needs wrap addressing to sample correctly.
    /// <inheritdoc/>
    public bool Wrap => true;

    #endregion

    #region Methods

    /// <summary>
    /// Creates a new instance with no <see cref="SystemManagers"/>, resolving one from
    /// <see cref="SystemManagers.Default"/> at construction time.
    /// </summary>
    public FilledStrokedRectangle() : this(null) { }

    /// <summary>
    /// Creates a new instance using the given <paramref name="managers"/>, or
    /// <see cref="SystemManagers.Default"/> when <c>null</c>.
    /// </summary>
    public FilledStrokedRectangle(SystemManagers? managers)
    {
        mManagers = managers;
        mChildren = new();
        Visible = true;
        _fillColor = Color.White;
        _strokeColor = Color.White;
        _strokeWidth = 1;

        Renderer? renderer = mManagers != null
            ? mManagers.Renderer
            : SystemManagers.Default?.Renderer;

        mLinePrimitive = new LinePrimitive(renderer?.TryGetSinglePixelTexture());
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Color = _strokeColor;
        mLinePrimitive.LinePixelWidth = _strokeWidth;

        UpdatePoints();
    }

    private void UpdatePoints() => LineRectangle.UpdateLinePrimitive(mLinePrimitive, this);

    /// <inheritdoc/>
    public override void Render(ISystemManagers managers)
    {
        var systemManagers = managers as SystemManagers;
        Renderer renderer = systemManagers?.Renderer ?? Renderer.Self;

        if (IsFilled && Width > 0 && Height > 0)
        {
            var texture = renderer.SinglePixelTexture;
            var sourceRectangle = renderer.SinglePixelSourceRectangle;
            var rotation = this.GetAbsoluteRotation(ignoreParentRotationIfRenderTarget: true);

            // managers is always a real SystemManagers when Render is invoked by the actual
            // renderer -- same assumption SolidRectangle/LineRectangle's own Render() methods make.
            Sprite.Render(systemManagers!, renderer.SpriteRenderer, this, texture, FillColor,
                sourceRectangle, flipVertical: false, rotationInDegrees: rotation);
        }

        if (StrokeWidth > 0)
        {
            LineRectangle.RenderLinePrimitive(mLinePrimitive, renderer.SpriteRenderer, this,
                systemManagers!, isDotted: false);
        }
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent) => mParent = parent;

    void IRenderable.PreRender() { }

    #region IVisible Members

    /// <inheritdoc/>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();

    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    #endregion

    /// <inheritdoc/>
    public override string ToString() => Name + " (FilledStrokedRectangle)";

    #endregion
}
