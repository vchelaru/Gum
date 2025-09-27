using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Themes;

public class FrbMenuStripColorTable : ProfessionalColorTable
{
    private Color _backgroundColor;
    private Color _foregroundColor;
    private Color _primaryColor;

    public FrbMenuStripColorTable(Color backgroundColor, Color foregroundColor, Color primaryColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _primaryColor = primaryColor;
    }

    public override Color MenuItemSelected
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemSelectedGradientBegin
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemSelectedGradientEnd
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemBorder
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientBegin
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientEnd
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientMiddle
    {
        get { return _primaryColor; }
    }

    public override Color MenuBorder
    {
        get { return _primaryColor; }
    }

    public override Color MenuStripGradientBegin
    {
        get { return _backgroundColor; }
    }

    public override Color MenuStripGradientEnd
    {
        get { return _backgroundColor; }
    }

    //public override Color MenuStripGradientMiddle
    //{
    //    get { return _backgroundColor; }
    //}

    //public override Color MenuStripText
    //{
    //    get { return _foregroundColor; }
    //}

    public override Color ToolStripDropDownBackground
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientBegin
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientEnd
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientMiddle
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripBorder
    {
        get { return _primaryColor; }
    }

    public override Color ToolStripContentPanelGradientBegin
    {
        get { return _backgroundColor; }
    }
}

public class FrbMenuStripRenderer : ToolStripProfessionalRenderer
{
    private Color _backgroundColor;
    private Color _foregroundColor;
    private Color _primaryColor;
    private Color _primaryTransparent;
    private Font _font;

    public FrbMenuStripRenderer(Color backgroundColor, Color foregroundColor, Color primaryColor, Font font)
        : base(new FrbMenuStripColorTable(backgroundColor, foregroundColor, primaryColor))
    {
        TreeView v;

        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _primaryColor = primaryColor;
        _primaryTransparent = Color.FromArgb(64, primaryColor);
        _font = font;
    }

    // ---- Layout helper: ensure enough padding for big fonts (called during paint; idempotent) ----
    private static readonly Padding MinStripPad = new Padding(6, 3, 6, 3);
    private static readonly Padding MinItemPad = new Padding(8, 2, 8, 2);

    private void EnsureLayoutDimensions(ToolStrip ts)
    {
        if (ts is not MenuStrip ms) return;

        // Measure font height at current DPI
        int fontH;
        using (var g = ms.CreateGraphics())
            fontH = (int)Math.Ceiling(_font.GetHeight(g));

        // Derive vertical padding roughly 1/3 of font height
        int vPadStrip = Math.Max(MinStripPad.Top, fontH / 3);
        int vPadItem = Math.Max(MinItemPad.Top, Math.Max(2, (fontH - _font.Height) / 2));

        var desiredStripPad = new Padding(MinStripPad.Left, vPadStrip, MinStripPad.Right, vPadStrip);
        if (ms.Padding != desiredStripPad)
            ms.Padding = desiredStripPad;

        foreach (ToolStripItem it in ms.Items)
        {
            // Only top-level items need this (dropdowns are separate windows)
            if (it is ToolStripMenuItem && it.Owner == ms)
            {
                var desiredItemPad = new Padding(MinItemPad.Left, vPadItem, MinItemPad.Right, vPadItem);
                if (it.Padding != desiredItemPad)
                    it.Padding = desiredItemPad;

                if (!it.AutoSize) it.AutoSize = true;
            }
        }
    }

    // ---- Backgrounds ----
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        // Keep layout in sync with font before drawing
        //EnsureLayoutDimensions(e.ToolStrip);

        using var brush = new SolidBrush(_backgroundColor);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, e.Item.Size);

        using (var bg = new SolidBrush(_backgroundColor))
            e.Graphics.FillRectangle(bg, rect);

        if (e.Item.Selected)
        {
            using (var hover = new SolidBrush(_primaryTransparent))
                e.Graphics.FillRectangle(hover, rect);


        }

        if (e.Item.Pressed)
        {
            using (var pen = new Pen(_primaryColor, 1))
            {
                int x0 = rect.Left;
                int y0 = rect.Top;
                int x1 = rect.Right - 1;   // stay inside client area
                int y1 = rect.Bottom - 1;

                // top
                e.Graphics.DrawLine(pen, x0, y0, x1, y0);
                // left
                e.Graphics.DrawLine(pen, x0, y0, x0, y1);
                // right
                e.Graphics.DrawLine(pen, x1, y0, x1, y1);
                // (no bottom)
            }
        }
    }

    // ---- Text ----
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        bool isTopLevel = e.ToolStrip is MenuStrip;

        // Dropdown items: keep built-in layout (handles shortcut column)
        if (!isTopLevel)
        {
            e.TextFont = _font;
            e.TextColor = _foregroundColor;
            base.OnRenderItemText(e);
            return;
        }

        // Top-level items: draw ourselves to avoid WPF overpaint & keep ClearType
        var color = e.Item.Enabled ? _foregroundColor : ControlPaint.Dark(_foregroundColor);

        var oldHint = e.Graphics.TextRenderingHint;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Compute a vertically-centered baseline from actual font height
        int fontH;
        using (var g = e.ToolStrip.CreateGraphics())
            fontH = (int)Math.Ceiling(_font.GetHeight(g));

        int y = e.Item.Bounds.Y + (e.Item.Bounds.Height - fontH) / 2;
        var pt = new PointF(e.TextRectangle.X, Math.Max(e.TextRectangle.Y, y));

        using (var br = new SolidBrush(color))
            e.Graphics.DrawString(e.Text, _font, br, pt);

        e.Graphics.TextRenderingHint = oldHint;
        // no base call
    }

    // ---- Arrows ----
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = _foregroundColor;
        base.OnRenderArrow(e);
    }

    // ---- Optional: remove the default image margin fill ----
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Intentionally empty to keep dropdowns flat with our background
    }
}