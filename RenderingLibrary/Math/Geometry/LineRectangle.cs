using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace RenderingLibrary.Math.Geometry;

public class LineRectangle : SpriteBatchRenderableBase, IVisible, IRenderableIpso, ISetClipsChildren
{
    #region Fields

    float mWidth = 32;
    float mHeight = 32;

    // Does position not update points? Is this a bug?
    public Vector2 Position;

    float mRotation;

    LinePrimitive mLinePrimitive;

    ObservableCollectionNoReset<IRenderableIpso> mChildren;


    IRenderableIpso mParent;


    SystemManagers mManagers;

    bool mVisible;

    #endregion

    #region Properties

    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    /// <summary>
    /// This is similar to the Visible property, but affects only this.
    /// This allows LineRectangles to not render without making their children invisible.
    /// </summary>
    public bool LocalVisible
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    }

    public float X
    {
        get => Position.X;
        set => Position.X = value;
    }

    public float Y
    {
        get => Position.Y;
        set => Position.Y = value;
    }

    public float Z
    {
        get;
        set;
    }


    public bool ClipsChildren
    {
        get;
        set;
    }

    public float Rotation
    {
        get
        {
            return mRotation;
        }
        set
        {
            mRotation = value;
            UpdatePoints();
        }
    }

    public bool FlipHorizontal { get; set; }

    public float Width
    {
        get { return mWidth; }
        set
        {
            mWidth = value;
            UpdatePoints();
        }
    }

    public IRenderableIpso Parent
    {
        get { return mParent; }
        set
        {
            if (mParent != value)
            {
                if (mParent != null)
                {
                    mParent.Children.Remove(this);
                }
                mParent = value;
                if (mParent != null)
                {
                    mParent.Children.Add(this);
                }
            }
        }
    }

    public float Height
    {
        get { return mHeight; }
        set
        {
            mHeight = value;
            UpdatePoints();
        }
    }
    public Color Color
    {
        get => mLinePrimitive.Color;
        set => mLinePrimitive.Color = value;
    }

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public object Tag { get; set; }

    public BlendState BlendState
    {
        get { return BlendState.NonPremultiplied; }
    }

    private Renderer AssociatedRenderer
    {
        get
        {
            if (mManagers != null)
            {
                return mManagers.Renderer;
            }
            else
            {
                return Renderer.Self;
            }
        }
    }

    public bool IsDotted
    {
        get;
        set;
    }

    public bool Wrap
    {
        get { return true; }
    }

    public float LinePixelWidth
    {
        get { return mLinePrimitive.LinePixelWidth; }
        set { mLinePrimitive.LinePixelWidth = value; }
    }

    public bool IsRenderTarget
    {
        get;
        // Originally this didn't have a set, but we are using line rectangles for
        // containers in Gum, so adding it here too
        set;
    }


    public int Alpha => Color.A;



    #endregion

    #region Methods

    public LineRectangle()
        : this(null)
    {

    }

    public LineRectangle(SystemManagers managers)
    {
        LocalVisible = true;

        mManagers = managers;

        mChildren = new ();

        Visible = true;
        Renderer? renderer;
        if (mManagers != null)
        {
            renderer = mManagers.Renderer;
        }
        else
        {
            renderer = SystemManagers.Default?.Renderer;
        }
        mLinePrimitive = new LinePrimitive(renderer?.TryGetSinglePixelTexture());
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);
        mLinePrimitive.Add(0, 0);

        UpdatePoints();

        // Why is it dotted by default? That's confusing.
        //IsDotted = true;
    }

    private void UpdatePoints()
    {
        UpdateLinePrimitive(mLinePrimitive, this);

    }

    public static void UpdateLinePrimitive(LinePrimitive linePrimitive, IRenderableIpso ipso)
    {
        Matrix matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.GetAbsoluteRotation()));

        // 0/4--1
        // |    |
        // 3----2

        linePrimitive.Replace(0, Vector2.Zero);
        linePrimitive.Replace(1, Vector2.Transform(new Vector2(ipso.Width, 0), matrix) );
        linePrimitive.Replace(2, Vector2.Transform(new Vector2(ipso.Width, ipso.Height), matrix) );
        linePrimitive.Replace(3, Vector2.Transform(new Vector2(0, ipso.Height), matrix) );
        linePrimitive.Replace(4, Vector2.Zero); // close back on itself

    }


    public override void Render(ISystemManagers managers)
    {
        //if (AbsoluteVisible && LocalVisible)
        if(LocalVisible)
        {
            // todo - add rotation
            var systemManagers = managers as SystemManagers;
            RenderLinePrimitive(mLinePrimitive, systemManagers.Renderer.SpriteRenderer, this, systemManagers, IsDotted);

        }
    }


    public static void RenderLinePrimitive(LinePrimitive linePrimitive, SpriteRenderer spriteRenderer, 
        IRenderableIpso ipso, SystemManagers managers, bool isDotted)
    {
        linePrimitive.Position.X = ipso.GetAbsoluteX();
        linePrimitive.Position.Y = ipso.GetAbsoluteY();

        Renderer renderer;
        if (managers != null)
        {
            renderer = managers.Renderer;
        }
        else
        {
            renderer = Renderer.Self;
        }


        if (isDotted)
        {
            var textureToUse = renderer.DottedLineTexture;
            linePrimitive.Render(spriteRenderer, managers, textureToUse, .25f * renderer.Camera.Zoom);
        }
        else
        {
            var textureToUse = renderer.SinglePixelTexture;
            if(renderer.SinglePixelSourceRectangle != null)
            {
                linePrimitive.Render(spriteRenderer, managers, textureToUse, 0, renderer.SinglePixelSourceRectangle);
            }
            else
            {
                linePrimitive.Render(spriteRenderer, managers, textureToUse, .25f * renderer.Camera.Zoom);
            }

        }
    }

    #endregion

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    void IRenderable.PreRender() { }

    #region IVisible Members
    /// <inheritdoc/>
    public bool Visible
    {
        get { return mVisible; }
        set
        {
            mVisible = value;
        }
    }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();
    /// <inheritdoc/>
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    #endregion

    public override string ToString()
    {
        return Name + " (LineRectangle)";
    }
}
