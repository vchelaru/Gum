using CommonFormsAndControls;
using Gum.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Gum.Controls
{
    public sealed class ThemedScrollContainer : UserControl
    {
        #region Public API

        public bool EnableVerticalScroll { get; set; } = true;
        public bool EnableHorizontalScroll { get; set; } = false;
        public bool AutoComputeExtent { get; set; } = true;

        public Size Extent
        {
            get { return _extent; }
            set { _extent = value; RecomputeBars(); ApplyOffsets(); }
        }

        public Point Offset
        {
            get { return new Point(_offsetX, _offsetY); }
            set
            {
                Point clamped = ClampOffset(value);
                if (clamped.X != _offsetX || clamped.Y != _offsetY)
                {
                    _offsetX = clamped.X;
                    _offsetY = clamped.Y;
                    ApplyOffsets();
                    EventHandler? handler = OffsetChanged;
                    if (handler != null) handler(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? OffsetChanged;

        public Control Canvas { get { return _canvas; } }
        public Control Viewport { get { return _viewport; } }
        public ThemedScrollBar VerticalBar { get { return _vbar; } }
        public ThemedScrollBar HorizontalBar { get { return _hbar; } }

        #endregion

        #region Fields

        private readonly Panel _viewport;
        private readonly Panel _canvas;
        private readonly ThemedScrollBar _vbar;
        private readonly ThemedScrollBar _hbar;

        private Size _extent = new Size(0, 0);
        private int _offsetX = 0;
        private int _offsetY = 0;
        private bool _syncing;

        #endregion

        #region Construction

        public ThemedScrollContainer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
            Padding = new Padding(0);

            _viewport = new NoFlickerPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            _viewport.Resize += (object? _, EventArgs __) => { RecomputeBars(); ApplyOffsets(); };
            _viewport.MouseWheel += Viewport_MouseWheel;

            _canvas = new NoFlickerPanel
            {
                Dock = DockStyle.None,
                Location = Point.Empty,
                BackColor = Color.Transparent
            };

            _vbar = new ThemedScrollBar
            {
                Orientation = ScrollOrientationEx.Vertical,
                Dock = DockStyle.Right,
                Visible = EnableVerticalScroll
            };
            _vbar.ValueChanged += (object? _, EventArgs __) =>
            {
                if (_syncing) return;
                _offsetY = _vbar.Value;
                ApplyOffsets();
            };

            _hbar = new ThemedScrollBar
            {
                Orientation = ScrollOrientationEx.Horizontal,
                Dock = DockStyle.Bottom,
                Visible = EnableHorizontalScroll
            };
            _hbar.ValueChanged += (object? _, EventArgs __) =>
            {
                if (_syncing) return;
                _offsetX = _hbar.Value;
                ApplyOffsets();
            };

            Controls.Add(_viewport);
            Controls.Add(_vbar);
            Controls.Add(_hbar);
            _viewport.Controls.Add(_canvas);

            _canvas.ControlAdded += (object? _, ControlEventArgs __) => { if (AutoComputeExtent) RecomputeBars(); };
            _canvas.ControlRemoved += (object? _, ControlEventArgs __) => { if (AutoComputeExtent) RecomputeBars(); };

            SizeChanged += ThemedScrollContainer_SizeChanged;
        }

        #endregion

        #region Public Methods

        public void AddContent(Control control)
        {
            _canvas.Controls.Add(control);
            if (AutoComputeExtent) RecomputeBars();
        }

        public void ClearContent()
        {
            _canvas.Controls.Clear();
            if (AutoComputeExtent) RecomputeBars();
        }

        #endregion

        #region Layout / Scroll

        private void RecomputeBars()
        {
            if (AutoComputeExtent)
            {
                if (_canvas.Controls.Count == 0)
                {
                    _extent = Size.Empty;
                }
                else
                {
                    int right = 0;
                    int bottom = 0;

                    foreach (Control child in _canvas.Controls)
                    {
                        Rectangle r = child.Bounds;
                        right = Math.Max(right, r.Right);
                        bottom = Math.Max(bottom, r.Bottom);
                    }

                    _extent = new Size(right, bottom);
                }
            }

            Size vp = ViewportSize();
            _canvas.Size = new Size(Math.Max(_extent.Width, vp.Width), Math.Max(_extent.Height, vp.Height));

            _vbar.Visible = EnableVerticalScroll && _extent.Height > vp.Height;
            _hbar.Visible = EnableHorizontalScroll && _extent.Width > vp.Width;

            PerformLayout();

            vp = ViewportSize();
            _syncing = true;
            try
            {
                if (_vbar.Visible)
                {
                    _vbar.Minimum = 0;
                    _vbar.Maximum = Math.Max(0, _extent.Height - 1);
                    _vbar.LargeChange = Math.Max(1, vp.Height);
                    _vbar.SmallChange = Math.Max(1, vp.Height / 6);
                    _vbar.Value = Math.Max(0, Math.Min(_vbar.Value, MaxStart(_vbar.Maximum, _vbar.LargeChange)));
                }
                else
                {
                    _offsetY = 0;
                }

                if (_hbar.Visible)
                {
                    _hbar.Minimum = 0;
                    _hbar.Maximum = Math.Max(0, _extent.Width - 1);
                    _hbar.LargeChange = Math.Max(1, vp.Width);
                    _hbar.SmallChange = Math.Max(1, vp.Width / 6);
                    _hbar.Value = Math.Max(0, Math.Min(_hbar.Value, MaxStart(_hbar.Maximum, _hbar.LargeChange)));
                }
                else
                {
                    _offsetX = 0;
                }
            }
            finally
            {
                _syncing = false;
            }

            Point clamped = ClampOffset(new Point(_offsetX, _offsetY));
            _offsetX = clamped.X;
            _offsetY = clamped.Y;

            Size vpAfter = ViewportSize();
            int desiredCanvasWidth = _hbar.Visible
                ? Math.Max(_extent.Width, vpAfter.Width)
                : vpAfter.Width;

            if (_canvas.Width != desiredCanvasWidth)
            {
                _canvas.Width = desiredCanvasWidth;
            }
        }

        private void ApplyOffsets()
        {
            _canvas.Location = new Point(-_offsetX, -_offsetY);

            _syncing = true;
            try
            {
                if (_vbar.Visible) _vbar.Value = _offsetY;
                if (_hbar.Visible) _hbar.Value = _offsetX;
            }
            finally
            {
                _syncing = false;
            }

            _viewport.Invalidate();
        }

        private void ThemedScrollContainer_SizeChanged(object? sender, EventArgs e)
        {
            RecomputeBars();
            ApplyOffsets();
        }

        private void Viewport_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_vbar.Visible) return;

            int deltaLines = Math.Sign(e.Delta) * -_vbar.SmallChange;
            Offset = new Point(_offsetX, _offsetY + deltaLines);
        }

        #endregion

        #region Utilities

        private Size ViewportSize()
        {
            return _viewport.ClientSize;
        }

        private static int MaxStart(int maximum, int largeChange)
        {
            return Math.Max(0, maximum - largeChange + 1);
        }

        private Point ClampOffset(Point p)
        {
            Size vp = ViewportSize();
            int maxX = Math.Max(0, _extent.Width - vp.Width);
            int maxY = Math.Max(0, _extent.Height - vp.Height);
            int x = Math.Max(0, Math.Min(p.X, maxX));
            int y = Math.Max(0, Math.Min(p.Y, maxY));
            return new Point(x, y);
        }

        #endregion

        #region Nested Types

        private sealed class NoFlickerPanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                    return cp;
                }
            }

            public NoFlickerPanel()
            {
                DoubleBuffered = true;
            }
        }

        #endregion
    }
}

public static class ThemedScrollContainerExtensions
{
    public static void WireTreeToScroller(this ThemedScrollContainer scroller, MultiSelectTreeView tree)
    {
        void UpdateExtent()
        {
            // --- VIRTUAL HEIGHT (visible rows * item height) ---
            int rows = CountVisibleNodes(tree);
            int rowH = Math.Max(1, tree.ItemHeight);
            int virtualHeight = rows * rowH;

            // --- VIRTUAL WIDTH (max rendered row width using your layout rules) ---
            int virtualWidth = ComputeVirtualWidth(tree);

            scroller.Extent = new Size(virtualWidth, virtualHeight);
        }

        // Initial
        UpdateExtent();

        // Changes that affect visibility/size:
        tree.AfterExpand += (_, __) => UpdateExtent();
        tree.AfterCollapse += (_, __) => UpdateExtent();
        tree.FontChanged += (_, __) => UpdateExtent();   // ItemHeight or font metrics change
        tree.SizeChanged += (_, __) => UpdateExtent();   // image/state sizes can depend on DPI
        tree.StructureMutated += (_, _) => UpdateExtent();

        tree.MouseWheel += (_, e) =>
        {
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            if (shift && scroller.HorizontalBar.Visible && scroller.EnableHorizontalScroll)
            {
                int step = Math.Max(1, scroller.HorizontalBar.SmallChange);
                int ticks = Math.Max(1, Math.Abs(e.Delta) / 120);
                int dx = Math.Sign(e.Delta) * -step * ticks;   // wheel up => scroll left
                scroller.Offset = new Point(scroller.Offset.X + dx, scroller.Offset.Y);
            }
            else if (scroller.VerticalBar.Visible && scroller.EnableVerticalScroll)
            {
                int step = Math.Max(1, scroller.VerticalBar.SmallChange);
                int ticks = Math.Max(1, Math.Abs(e.Delta) / 120);
                int dy = Math.Sign(e.Delta) * -step * ticks;   // wheel up => scroll up
                scroller.Offset = new Point(scroller.Offset.X, scroller.Offset.Y + dy);
            }
        };// If you add/remove nodes programmatically, call UpdateExtent() once after the batch.

        // ---- Helpers ----

        static int CountVisibleNodes(TreeView tv)
        {
            int c = 0;
            for (TreeNode n = tv.Nodes.Count > 0 ? tv.Nodes[0] : null; n != null; n = n.NextVisibleNode)
                c++;
            return c;
        }

        static int ComputeVirtualWidth(MultiSelectTreeView tv)
        {
            if (tv.IsDisposed) return tv.ClientSize.Width;

            // Measure using current DPI and fonts
            using var g = tv.CreateGraphics();
            float dpi = g.DpiX / 96f;

            // Match your LayoutNodeRow() spacing/slots:
            int spacing = (int)Math.Round(3 * dpi);                           // DpiScaleI(3)
            int chevronBox = Math.Max(tv.Indent, (int)Math.Round(tv.ChevronBoxSize * dpi));
            int stateW = tv.StateImageList != null ? tv.StateImageList.ImageSize.Width : 0;
            int imageW = tv.ImageList != null ? tv.ImageList.ImageSize.Width : 0;

            int maxRight = 0;

            // Walk visible nodes
            for (TreeNode n = tv.Nodes.Count > 0 ? tv.Nodes[0] : null; n != null; n = n.NextVisibleNode)
            {
                // Left edge includes horizontal offset & level indent
                int left = tv.DisplayRectangle.Left + n.Level * tv.Indent;

                // Start after chevron slot (always reserved) + spacing
                int cursor = left + chevronBox + spacing;

                // Optional state image (only if the node actually has one)
                if (tv.StateImageList != null && n.StateImageIndex >= 0 && n.StateImageIndex < tv.StateImageList.Images.Count)
                    cursor += stateW + spacing;

                // Optional normal image (only if present for this node)
                bool hasImage = false;
                if (tv.ImageList != null)
                {
                    int idx = n.ImageIndex;
                    if (idx < 0 && !string.IsNullOrEmpty(n.ImageKey))
                        idx = tv.ImageList.Images.IndexOfKey(n.ImageKey);
                    hasImage = (idx >= 0 && idx < tv.ImageList.Images.Count);
                }
                if (hasImage) cursor += imageW + spacing;

                // Text width (tight measure). Your draw uses EndEllipsis, so we measure the raw string.
                var font = n.NodeFont ?? tv.Font;
                var textSize = TextRenderer.MeasureText(
                    g,
                    n.Text ?? string.Empty,
                    font,
                    Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.GlyphOverhangPadding);

                int right = cursor + textSize.Width;

                // Small safety margin for focus/selection outline & rounding
                right += 4;

                if (right > maxRight)
                    maxRight = right;
            }

            // Ensure we don't report absurdly small widths when empty
            if (maxRight <= 0) maxRight = tv.ClientSize.Width;

            return maxRight + 10;
        }
    }
}
