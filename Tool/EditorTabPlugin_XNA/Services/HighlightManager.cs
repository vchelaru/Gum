using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using ToolsUtilitiesStandard.Helpers;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using EditorTabPlugin_XNA.Utilities;

namespace Gum.Wireframe;

public class HighlightManager
{
    SolidRectangle mOverlaySolidRectangle;
    Sprite mOverlaySprite;
    NineSlice mOverlayNineSlice;
    LinePolygon mOverlayLinePolygon;

    public IPositionedSizedObject? HighlightedIpso { get; set; }


    private Sprite? HighlightedSprite
    {
        get
        {

            return (HighlightedIpso as GraphicalUiElement)?.Component as Sprite;
        }
    }

    private NineSlice? HighlightedNineSlice
    {
        get
        {
            return (HighlightedIpso as GraphicalUiElement)?.Component as NineSlice;
            
        }
    }

    private LineRectangle? HighlightedLineRectangle
    {
        get
        {
            return (HighlightedIpso as GraphicalUiElement)?.Component as LineRectangle;
        }
    }

    private LinePolygon? HighlightedLinePolygon
    {
        get
        {
            return (HighlightedIpso as GraphicalUiElement)?.Component as LinePolygon;
        }
    }

    bool areHighlightsVisible;
    public bool AreHighlightsVisible
    {
        get => areHighlightsVisible;
        set
        {
            areHighlightsVisible = value;

            if(value == false)
            {
                UnhighlightIpso(HighlightedIpso as GraphicalUiElement);
            }
            else
            {
                UpdateHighlightObjects();

            }
        }
    }

    public HighlightManager(Layer layer)
    {

        mOverlaySolidRectangle = new SolidRectangle();
        mOverlaySolidRectangle.Name = "Overlay SolidRectangle";
        mOverlaySolidRectangle.Color = Color.LightGreen.WithAlpha(100);
        mOverlaySolidRectangle.Visible = false;
        ShapeManager.Self.Add(mOverlaySolidRectangle, layer);

        mOverlaySprite = new Sprite(null);
        mOverlaySprite.Name = "Overlay Sprite";
        mOverlaySprite.BlendState = BlendState.Additive;
        mOverlaySprite.Visible = false;
        SpriteManager.Self.Add(mOverlaySprite, layer);

        mOverlayNineSlice = new NineSlice();
        mOverlayNineSlice.Name = "Overlay NineSlice";
        mOverlayNineSlice.BlendState = BlendState.Additive;
        mOverlayNineSlice.Visible = false;
        SpriteManager.Self.Add(mOverlayNineSlice, layer);

        mOverlayLinePolygon = new LinePolygon();
        mOverlayLinePolygon.Name = "Overly LinePolygon";
        // polys are white by default so let's make it dark
        mOverlayLinePolygon.Color = Color.DarkGreen;
        mOverlayLinePolygon.IsDotted = true;
        mOverlayLinePolygon.Visible = false;
        ShapeManager.Self.Add(mOverlayLinePolygon, layer);

    }

    public void UnhighlightIpso(GraphicalUiElement highlightedIpso)
    {
        if (highlightedIpso?.Component is Sprite)
        {
            mOverlaySprite.Visible = false;
        }
        else if (highlightedIpso?.Component is NineSlice)
        {
            mOverlayNineSlice.Visible = false;
        }
        else if (highlightedIpso?.Component is LineRectangle)
        {
            mOverlaySolidRectangle.Visible = false;
        }
        else if(highlightedIpso?.Component is LinePolygon)
        {
            mOverlayLinePolygon.Visible = false;
        }
    }

    /// <summary>
    /// Updates additional UI used to highlight objects, such
    /// as a solid rectangle for highlighted containers or overlaying
    /// an additive Sprite over the highlighted Sprite
    /// </summary>
    public void UpdateHighlightObjects()
    {
        if (HighlightedSprite != null && AreHighlightsVisible)
        {
            mOverlaySprite.Visible = true;

            var bounds = HighlightedSprite.GetBounds();

            mOverlaySprite.X = bounds.left;
            mOverlaySprite.Y = bounds.top;

            mOverlaySprite.Width = bounds.right - bounds.left;
            mOverlaySprite.Height = bounds.bottom - bounds.top;
            mOverlaySprite.Texture = HighlightedSprite.Texture;


            mOverlaySprite.Wrap = HighlightedSprite.Wrap;

            mOverlaySprite.SourceRectangle = HighlightedSprite.SourceRectangle;

            mOverlaySprite.FlipHorizontal = HighlightedSprite.FlipHorizontal;
            mOverlaySprite.FlipVertical = HighlightedSprite.FlipVertical;

            mOverlaySprite.Rotation = HighlightedSprite.Rotation;
        }
        else if (HighlightedNineSlice != null && AreHighlightsVisible)
        {
            SetNineSliceOverlay();
        }
        else if (HighlightedLineRectangle != null && AreHighlightsVisible)
        {
            SolidRectangle overlay = mOverlaySolidRectangle;

            overlay.Visible = true;

            var bounds = HighlightedLineRectangle.GetBounds();

            overlay.X = bounds.left;
            overlay.Y = bounds.top;

            overlay.Width = bounds.right - bounds.left;
            overlay.Height = bounds.bottom - bounds.top;

            overlay.Rotation = HighlightedLineRectangle.Rotation;
        }
        else if(HighlightedLinePolygon != null && AreHighlightsVisible)
        {
            // todo - finish here
            var overlay = mOverlayLinePolygon;
            overlay.Visible = true;
            overlay.X = HighlightedLinePolygon.GetAbsoluteX();
            overlay.Y = HighlightedLinePolygon.GetAbsoluteY();

            List<Vector2> points = new List<Vector2>();
            // todo - set points here:
            for(int i = 0; i < HighlightedLinePolygon.PointCount; i++)
            {
                points.Add(HighlightedLinePolygon.PointAt(i));
            }

            overlay.SetPoints(points);

            overlay.Rotation = HighlightedLinePolygon.Rotation;
        }
    }

    private void SetNineSliceOverlay()
    {

        mOverlayNineSlice.Visible = true;

        var bounds = HighlightedNineSlice.GetBounds();

        mOverlayNineSlice.X = bounds.left;
        mOverlayNineSlice.Y = bounds.top;

        mOverlayNineSlice.Width = bounds.right - bounds.left;
        mOverlayNineSlice.Height = bounds.bottom - bounds.top;
        mOverlayNineSlice.TopLeftTexture = HighlightedNineSlice.TopLeftTexture;
        mOverlayNineSlice.TopTexture = HighlightedNineSlice.TopTexture;
        mOverlayNineSlice.TopRightTexture = HighlightedNineSlice.TopRightTexture;

        mOverlayNineSlice.LeftTexture = HighlightedNineSlice.LeftTexture;
        mOverlayNineSlice.CenterTexture = HighlightedNineSlice.CenterTexture;
        mOverlayNineSlice.RightTexture = HighlightedNineSlice.RightTexture;

        mOverlayNineSlice.BottomLeftTexture = HighlightedNineSlice.BottomLeftTexture;
        mOverlayNineSlice.BottomTexture = HighlightedNineSlice.BottomTexture;
        mOverlayNineSlice.BottomRightTexture = HighlightedNineSlice.BottomRightTexture;

        mOverlayNineSlice.Red = HighlightedNineSlice.Red;
        mOverlayNineSlice.Green = HighlightedNineSlice.Green;
        mOverlayNineSlice.Blue = HighlightedNineSlice.Blue;

        mOverlayNineSlice.SourceRectangle = HighlightedNineSlice.SourceRectangle;

        mOverlayNineSlice.Rotation = HighlightedNineSlice.Rotation;

    }
}
