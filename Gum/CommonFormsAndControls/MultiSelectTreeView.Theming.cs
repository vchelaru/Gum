using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace CommonFormsAndControls;
#nullable enable
public partial class MultiSelectTreeView
{
    #region Appearance
    private float _dpiScale = 1f;
    private int _cachedFontHeight;
    private readonly Dictionary<(Color color, float width), Pen> _penCache = new();
    private SolidBrush? _rowBgBrushHot;
    private SolidBrush? _rowBgBrushNormal;
    private Color _hoverBgColor = Color.Transparent;
    public Color HoverBgColor
    {
        get => _hoverBgColor;
        set
        {
            if (value == _hoverBgColor) return;
            _hoverBgColor = value;
            _rowBgBrushHot?.Dispose();
            _rowBgBrushHot = new SolidBrush(_hoverBgColor);
            Invalidate(); // redraw to reflect new hover color
        }
    }
    public Color SelectedBorderColor { get; set; } = Color.Blue;

    public Color ChevronColor { get; set; } = Color.Empty; // empty -> follows ForeColor
    public float ChevronThickness { get; set; } = 2.0f;    // DIP
    public int ChevronBoxSize => this.FontHeight;          // DIP


    #endregion

    #region Fields

    private TreeNode? _hotChevronNode;
    private TreeNode? _hotNode;

    private TreeNode? _hotDropNode;
    private DropKind _dropKind = DropKind.None;

    private TreeNode? _dragCandidateNode;
    private Point _mouseDownPoint;
    private bool _initiatedDrag;

    private bool _inMouseSeq;
    private Keys _modsAtMouseDown;
    private bool _suppressClickSelection;
    private bool _eatRowClickSeq;
    private bool _eatGlyphSeq;
    private TreeNode? _mouseDownNode;

    /// <summary>
    /// Describes how a potential drop would be applied relative to a target node.
    /// </summary>
    public enum DropKind { None, Before, Into, IntoFirst, After }

    /// <summary>
    /// When true, the control will perform native reordering (moving) of dragged nodes
    /// if no external drag logic is desired. Host code that supplies its own drag/drop
    /// logic (like Gum's ElementTreeViewManager) can set this to false to suppress
    /// automatic reordering.
    /// </summary>
    public bool EnableNativeReorder { get; set; } = true;

    private const int TVS_HASBUTTONS = 0x0001;

    #endregion

    #region Win32

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int vk);

    private const int VK_MENU = 0x12; // Alt

    #endregion

    #region Setup

    partial void ExtendedConstructor()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        HotTracking = false;
        HideSelection = true;
        ShowLines = false;
        ShowRootLines = false;
        ShowNodeToolTips = false;
        Scrollable = false;
        AllowDrop = true;

        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);

        AfterSelect += OnAfterSelect;
        GotFocus += (_, _) => Invalidate();
        LostFocus += (_, _) => Invalidate();
        MouseLeave += (_, _) => SetHotNode(null);
        MouseDown += TreeView_MouseDown;
        MouseMove += (_, e) => SetHotNode(GetNodeAtRowY(e.Location.Y), e.Location);
        AfterExpand += (_, _) => Invalidate();
        AfterCollapse += (_, _) => Invalidate();
        ForeColorChanged += (_, _) => Invalidate();
    }

    private void OnAfterSelect(object? sender, TreeViewEventArgs args)
    {
        Rectangle inv = Rectangle.Empty;

        // current single
        if (mSelectedNode != null && mSelectedNode.Bounds != Rectangle.Empty)
        {
            inv = RowRect(mSelectedNode);
        }

        // multiselect
        foreach (var n in SelectedNodes)
        {
            if (n.Bounds != Rectangle.Empty)
                inv = inv.IsEmpty ? RowRect(n) : Rectangle.Union(inv, RowRect(n));
        }

        // event-associated
        if (args?.Node != null && args.Node.Bounds != Rectangle.Empty)
        {
            var r = RowRect(args.Node);
            inv = inv.IsEmpty ? r : Rectangle.Union(inv, r);
        }

        if (!inv.IsEmpty)
        {
            Invalidate(inv);
            Update(); // flush paint to show immediately -- this makes it happen _now_
        }
        else
        {
            Invalidate();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        using (var g = CreateGraphics())
            _dpiScale = g.DpiX / 96f;

        _cachedFontHeight = Font.Height;

        const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
        const int TVS_EX_DOUBLEBUFFER = 0x0004;
        SendMessage(Handle, TVM_SETEXTENDEDSTYLE, (IntPtr)TVS_EX_DOUBLEBUFFER, (IntPtr)TVS_EX_DOUBLEBUFFER);

        RebuildBrushes();
    }

    private int DpiScaleI(int v) => (int)Math.Round(v * _dpiScale);
    private float DpiScaleF(float v) => v * _dpiScale;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style &= ~TVS_HASBUTTONS; // remove native +/- buttons
            return cp;
        }
    }
    #endregion

    private void SetHotNode(TreeNode? node, Point? mouse = null)
    {
        if (ReferenceEquals(_hotNode, node) && mouse == null) return;

        TreeNode? oldHotNode = _hotNode;
        TreeNode? oldHotChevron = _hotChevronNode;

        _hotNode = node;
        _hotChevronNode = null;

        if (node != null && mouse != null)
        {
            Rectangle glyph = GetChevronRect(node);
            if (!glyph.IsEmpty && glyph.Contains(mouse.Value))
            {
                _hotChevronNode = node;
            }
        }

        Rectangle union = Rectangle.Empty;

        if (oldHotNode != null)
        {
            Rectangle oldBounds = new Rectangle(0, oldHotNode.Bounds.Top, ClientSize.Width, oldHotNode.Bounds.Height);
            union = oldBounds;
        }
        if (node != null)
        {
            Rectangle newBounds = new Rectangle(0, node.Bounds.Top, ClientSize.Width, node.Bounds.Height);
            union = union.IsEmpty ? newBounds : Rectangle.Union(union, newBounds);
        }
        if (oldHotChevron != null)
        {
            union = union.IsEmpty ? oldHotChevron.Bounds : Rectangle.Union(union, oldHotChevron.Bounds);
        }
        if (_hotChevronNode != null)
        {
            union = union.IsEmpty ? _hotChevronNode.Bounds : Rectangle.Union(union, _hotChevronNode.Bounds);
        }

        if (!union.IsEmpty)
        {
            Invalidate(union);
        }
    }

    #region Drawing
    private Pen GetPen(Color color, float widthPx)
    {
        // widthPx should already be DPI-scaled
        var key = (color, widthPx);
        if (_penCache.TryGetValue(key, out var pen)) return pen;
        pen = new Pen(color, Math.Max(1f, widthPx))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        _penCache[key] = pen;
        return pen;
    }
    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = false;

        bool isSelected = e.Node != null && SelectedNodes.Contains(e.Node);
        bool isHot = e.Node == _hotNode;

        var g = e.Graphics;
        // Keep defaults – no expensive high-quality modes
        // g.SmoothingMode = SmoothingMode.None; // (default is fine)

        // Full-row rect once
        Rectangle row = new Rectangle(0, e.Bounds.Top, ClientSize.Width, e.Bounds.Height);

        // Fill background with cached brushes (no new brush per node)
        var rowBrush = isHot ? _rowBgBrushHot : _rowBgBrushNormal;
        if (rowBrush != null)
            g.FillRectangle(rowBrush, row);

        // Single layout pass
        Rectangle rChevron, rState, rImage, rText;

        if (e.Node != null)
        {
            LayoutNodeRow(e.Node, row, out rChevron, out rState, out rImage, out rText);
            // Chevron (no SmoothingScope, no per-call pen allocations)
            if (!rChevron.IsEmpty)
            {
                var color = (e.Node == _hotChevronNode) ? SelectedBorderColor :
                            (ChevronColor.IsEmpty ? ForeColor : ChevronColor);

                // Width already DPI-scaled once
                float w = Math.Max(1f, DpiScaleF(ChevronThickness));
                var pen = GetPen(color, w);

                if (e.Node.IsExpanded == true)
                    DrawChevronDownFast(g, pen, rChevron);
                else
                    DrawChevronRightFast(g, pen, rChevron);
            }
            // State image – GDI path is fast
            if (!rState.IsEmpty && StateImageList != null &&
                e.Node.StateImageIndex >= 0 && e.Node.StateImageIndex < StateImageList.Images.Count)
            {
                StateImageList.Draw(g, rState.Location, e.Node.StateImageIndex);
            }
            // Node image – use ImageList.Draw only. No per-node quality switches.
            if (!rImage.IsEmpty && ImageList != null)
            {
                int idx = e.Node.ImageIndex;
                if (idx < 0 && !string.IsNullOrEmpty(e.Node.ImageKey))
                    idx = ImageList.Images.IndexOfKey(e.Node.ImageKey);

                if (idx >= 0 && idx < ImageList.Images.Count)
                    ImageList.Draw(g, rImage.Location, idx);
            }
            // Text – TextRenderer is already the fast path
            TextRenderer.DrawText(
                g,
                e.Node.Text,
                e.Node.NodeFont ?? Font,
                rText,
                ForeColor,
                Color.Transparent,
                TextFormatFlags.NoClipping |
                TextFormatFlags.GlyphOverhangPadding |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.VerticalCenter);

        }




        if (isSelected)
        {
            // 1px cached pen
            var pen = GetPen(SelectedBorderColor, 1f);
            var rr = Rectangle.Inflate(row, -1, -1);
            g.DrawRectangle(pen, rr);
        }

        if (e.Node != null && _hotDropNode == e.Node && _dropKind != DropKind.None)
        {
            // Keep this light – thin pen only, no AA
            var pen = GetPen(SelectedBorderColor, 2f);
            switch (_dropKind)
            {
                case DropKind.Into:

                case DropKind.IntoFirst:
                    {
                        // Indent rectangle to the child insertion level (node.Level + 1)
                        int leftIndent = Math.Max(0, DisplayRectangle.Left + (e.Node.Level + 1) * Indent);
                        Rectangle rr = new Rectangle(leftIndent, e.Bounds.Top + 1, Math.Max(0, ClientSize.Width - leftIndent - 2), e.Bounds.Height - 2);
                        g.DrawRectangle(pen, rr);
                    }
                    break;
                case DropKind.Before:
                    {
                        var prev = e.Node.PrevNode; // boundary between prev & current
                        int y = prev != null && !prev.IsExpanded ? prev.Bounds.Bottom - 1 : e.Bounds.Top + 1;
                        int leftIndent = Math.Max(0, DisplayRectangle.Left + (e.Node.Level + 1) * Indent);
                        g.DrawLine(pen, leftIndent, y, ClientSize.Width - 3, y);
                    }
                    break;
                case DropKind.After:
                    {
                        // Draw lower half of split boundary inside this row so next row won't overwrite
                        int y = e.Bounds.Bottom - 1;
                        int leftIndent = Math.Max(0, DisplayRectangle.Left + (e.Node.Level + 1) * Indent);
                        g.DrawLine(pen, leftIndent, y, ClientSize.Width - 3, y);
                    }
                    break;


            }
        }

        ////  --- Originally I wanted this, but it's also kind of noisy, keeping for reference.
        // Parent highlight for insertion (Before/After): draw a subtle dashed rectangle around parent row
        //if (_hotDropNode != null && (_dropKind == DropKind.Before || _dropKind == DropKind.After))
        //{
        //    var parent = _hotDropNode.Parent;
        //    if (parent != null && parent == e.Node)
        //    {
        //        int leftIndentParent = Math.Max(0, DisplayRectangle.Left + (parent.Level) * Indent);
        //        Rectangle parentRow = new Rectangle(leftIndentParent, parent.Bounds.Top, Math.Max(0, ClientSize.Width - leftIndentParent), parent.Bounds.Height);
        //        using (var pParent = new Pen(Color.FromArgb(180, SelectedBorderColor), 1f))
        //        {
        //            pParent.DashStyle = DashStyle.Dash;
        //            var rr = Rectangle.Inflate(parentRow, -1, -1);
        //            g.DrawRectangle(pParent, rr);
        //        }
        //    }
        //}

        // Draw upper half of AFTER boundary line in the next sibling row (so line spans both rows)
        if (_dropKind == DropKind.After && _hotDropNode != null && _hotDropNode.NextNode == e.Node)
        {
            // Use same pen thickness
            var pen = GetPen(SelectedBorderColor, 2f);
            int yTop = e.Bounds.Top - 1; // top boundary of this (next) row
            int leftIndent = Math.Max(0, DisplayRectangle.Left + (_hotDropNode.Level + 1) * Indent);
            g.DrawLine(pen, leftIndent, yTop, ClientSize.Width - 3, yTop);
        }
    }

    private Rectangle GetChevronRect(TreeNode node)
    {
        if (node == null || node.Bounds == Rectangle.Empty) return Rectangle.Empty;

        Rectangle row = new(0, node.Bounds.Top, ClientSize.Width, node.Bounds.Height);
        Rectangle rChevron;
        LayoutNodeRow(node, row, out rChevron, out _, out _, out _);
        return rChevron;
    }

    private static void DrawChevronRight(Graphics g, Pen pen, Rectangle r)
    {
        int s = Math.Min(r.Width, r.Height);
        int x0 = r.Left + (r.Width - s) / 2;
        int y0 = r.Top + (r.Height - s) / 2;

        float mx = s * 0.40f;
        float my = s * 0.28f;

        using (Pen p = (Pen)pen.Clone())
        {
            p.Width = Math.Max(1f, Math.Min(pen.Width, s / 9f));

            float leftX = x0 + mx;
            float midX = x0 + s - mx;
            float midY = y0 + s * 0.50f;
            float upY = y0 + my;
            float downY = y0 + s - my;

            g.DrawLine(p, leftX, upY, midX, midY);
            g.DrawLine(p, leftX, downY, midX, midY);
        }
    }

    private static void DrawChevronDown(Graphics g, Pen pen, Rectangle r)
    {
        int s = Math.Min(r.Width, r.Height);
        int x0 = r.Left + (r.Width - s) / 2;
        int y0 = r.Top + (r.Height - s) / 2;

        float mx = s * 0.28f;
        float my = s * 0.40f;

        using (Pen p = (Pen)pen.Clone())
        {
            p.Width = Math.Max(1f, Math.Min(pen.Width, s / 9f));

            float midX = x0 + s * 0.50f;
            float topY = y0 + my;
            float botY = y0 + s - my;
            float leftX = x0 + mx;
            float rightX = x0 + s - mx;

            g.DrawLine(p, leftX, topY, midX, botY);
            g.DrawLine(p, rightX, topY, midX, botY);
        }
    }

    private sealed class SmoothingScope : IDisposable
    {
        private readonly Graphics _g;
        private readonly SmoothingMode _prev;

        public SmoothingScope(Graphics g)
        {
            _g = g;
            _prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        public void Dispose()
        {
            _g.SmoothingMode = _prev;
        }
    }

    #endregion

    #region BaseOverrides


    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        RebuildBrushes();
    }

    private partial bool MouseUpOverride()
    {
        _dragCandidateNode = null;
        _initiatedDrag = false;
        if (_suppressClickSelection)
        {
            return true;
        }

        return false;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _cachedFontHeight = Font.Height;
        Indent = ChevronBoxSize;
    }

    // If you support per-monitor DPI, also recompute _dpiScale on WM_DPICHANGED.
    // Otherwise this suffices.

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        // If you really need tinted icons, build a tinted ImageList *here* once.
        // ImageAttributes = GetAttrs(ForeColor);  // remove per-node tinting path below
        RebuildBrushes();
    }
    private void RebuildBrushes()
    {
        _rowBgBrushNormal?.Dispose();
        _rowBgBrushHot?.Dispose();

        _rowBgBrushNormal = new SolidBrush(BackColor);
        _rowBgBrushHot = new SolidBrush(HoverBgColor);
    }
    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // base.OnPaintBackground(pevent); // comment out to reduce extra fill if safe
    }
    private System.Drawing.Imaging.ImageAttributes GetAttrs(Color color)
    {
        var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
        {
        new[] { color.R/255f, 0, 0, 0, 0 },
        new[] { 0, color.G/255f, 0, 0, 0 },
        new[] { 0, 0, color.B/255f, 0, 0 },
        new[] { 0, 0, 0, color.A/255f, 0 },
        new[] { 0, 0, 0, 0, 1f },
        });
        var ia = new System.Drawing.Imaging.ImageAttributes();
        ia.SetColorMatrix(cm);
        return ia;
    }

    private void TreeView_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            TreeNode? node = this.GetNodeAtRowY(e.Location.Y);
            _initiatedDrag = false;          // reset every left-press   
            _mouseDownPoint = e.Location;
            _dragCandidateNode = node;
            if (node != null && !mSelectedNodes.Contains(node))
            {
                // These were commented out from hash 3e123616856be6717c66b49b4f5dfcd5f9136907   
                // Uncommented because they appear to fix secondary issue in #1143   
                SelectSingleNode(node);
                SetNodeSelected(node, true);
            }

            mSelectedNode = node;

        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        // Only while left is down & we have a candidate  
        if ((Control.MouseButtons & MouseButtons.Left) == 0 || _dragCandidateNode == null)
            return;

        // Respect system drag threshold  
        var dx = Math.Abs(e.X - _mouseDownPoint.X);
        var dy = Math.Abs(e.Y - _mouseDownPoint.Y);
        var dragRect = SystemInformation.DragSize;
        if (dx < dragRect.Width / 2 && dy < dragRect.Height / 2)
            return;

        // Kick off classic drag by RAISING ItemDrag so external handlers run  
        if (!_initiatedDrag && _dragCandidateNode != null)
        {
            _initiatedDrag = true;

            // Ensure selection is in a good state before raising ItemDrag:  
            if (!mSelectedNodes.Contains(_dragCandidateNode))
            {
                SelectSingleNode(_dragCandidateNode);
                SetNodeSelected(_dragCandidateNode, true);
            }

            var nodeToDrag = _dragCandidateNode;
            _dragCandidateNode = null;

            // This invokes your ObjectTreeView.ItemDrag handler  
            OnItemDrag(new ItemDragEventArgs(MouseButtons.Left, nodeToDrag));

            // If native reordering is enabled, kick off the drag operation ourselves.
            // External hosts that want custom behavior can set EnableNativeReorder = false
            // and perform their own DoDragDrop in an ItemDrag handler.
            if (EnableNativeReorder)
            {
                object dragData = SelectedNodes.Count > 1
                    ? SelectedNodes.ToArray()
                    : (object)nodeToDrag;
                try
                {
                    DoDragDrop(dragData, DragDropEffects.Move | DragDropEffects.Copy);
                }
                catch
                {
                    // Swallow exceptions to avoid destabilizing host app
                }
            }
        }

    }

    #endregion

    #region Drag & Drop

    protected override void OnDragEnter(DragEventArgs e)
    {
        var data = e.Data;
        if (data != null)
        {
            TreeNode[] dragged = ExtractDraggedNodes(data);
            e.Effect = dragged.Length > 0
                ? ((e.AllowedEffect & DragDropEffects.Move) != 0 ? DragDropEffects.Move : DragDropEffects.Copy)
                : DragDropEffects.None;
        }
        base.OnDragEnter(e);
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        TreeNode[] dragged = ExtractDraggedNodes(e.Data);
        if (dragged.Length == 0)
        {
            e.Effect = DragDropEffects.None;
            ClearDropAdornment();
            base.OnDragOver(e);
            return;
        }

        Point pt = PointToClient(new Point(e.X, e.Y));
        AutoScrollWhileDragging(pt);

        (TreeNode? node, DropKind kind) result = GetDropAt(pt, dragged);
        TreeNode? node = result.node;
        DropKind kind = result.kind;

        // Allow consumer to validate / adjust drop target & kind
        if (node != null && kind != DropKind.None && ValidateSortingDrop != null)
        {
            var args = new ValidateDropEventArgs(dragged, node, kind);
            ValidateSortingDrop(this, args);
            if (!args.Allow)
            {
                node = null;
                kind = DropKind.None;
            }
            else
            {
                // consumer may have modified node or kind
                node = args.TargetNode;
                kind = args.Kind;
            }
        }

        bool valid = node != null && kind != DropKind.None;

        e.Effect = valid
            ? ((e.KeyState & 0x0008) != 0 ? DragDropEffects.Copy : DragDropEffects.Move) // Ctrl => Copy
            : DragDropEffects.None;

        UpdateDropAdornment(node, kind);
        base.OnDragOver(e);
    }

    protected override void OnDragLeave(EventArgs e)
    {
        ClearDropAdornment();
        base.OnDragLeave(e);
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        try
        {
            TreeNode[] dragged = ExtractDraggedNodes(e.Data);
            if (dragged.Length == 0)
            {
                base.OnDragDrop(e);
                return;
            }

            Point pt = PointToClient(new Point(e.X, e.Y));
            (TreeNode? node, DropKind kind) result = GetDropAt(pt, dragged);
            TreeNode? node = result.node;
            DropKind kind = result.kind;

            if (node == null || kind == DropKind.None)
            {
                ClearDropAdornment();
                base.OnDragDrop(e);
                return;
            }

            // Give consumer chance to intercept / cancel / adjust
            bool performNative = EnableNativeReorder; // capture current
            if (NodeSortingDropped != null)
            {
                var args = new DroppingEventArgs(dragged, node, kind) { PerformNativeReorder = performNative };
                NodeSortingDropped(this, args);
                if (args.Cancel)
                {
                    ClearDropAdornment();
                    base.OnDragDrop(e);
                    return;
                }
                node = args.TargetNode;
                kind = args.Kind;
                performNative = args.PerformNativeReorder;
            }

            // Reparent/insert logic lives elsewhere.
            if (performNative && node != null && kind != DropKind.None)
            {
                PerformNativeReorder(dragged, node, kind);
            }
        }
        finally
        {
            ClearDropAdornment();
            base.OnDragDrop(e);
        }
    }

    private void PerformNativeReorder(TreeNode[] dragged, TreeNode target, DropKind kind)
    {
        if (target == null || kind == DropKind.None) return;

        // Avoid invalid moves (dragging onto itself)
        dragged = dragged.Where(d => d != null && !ReferenceEquals(d, target)).ToArray();
        if (dragged.Length == 0) return;

        // Preserve original order as they appear currently (top to bottom). This helps multi-select drags.
        var allNodes = new List<TreeNode>();
        foreach (TreeNode root in Nodes)
        {
            CollectVisible(root, allNodes);
        }
        dragged = allNodes.Where(n => dragged.Contains(n)).ToArray();

        switch (kind)
        {
            case DropKind.Into:
                foreach (var d in dragged)
                {
                    if (IsDescendantOf(d, target)) continue; // prevent cycles
                    d.Remove();
                    target.Nodes.Add(d); // append at end
                }
                target.Expand();
                break;
            case DropKind.IntoFirst:
                {
                    var filtered = dragged.Where(d => d != target && !IsDescendantOf(target, d)).ToList();
                    foreach (var d in filtered)
                        d.Remove();
                    int idx = 0;
                    foreach (var d in filtered)
                    {
                        target.Nodes.Insert(idx++, d); // insert at front preserving order
                    }
                    target.Expand();
                }
                break;
            case DropKind.Before:
            case DropKind.After:
                var parent = target.Parent;
                TreeNodeCollection siblings = parent != null ? parent.Nodes : Nodes;
                int insertIndex = siblings.IndexOf(target);
                if (insertIndex < 0) insertIndex = siblings.Count;
                if (kind == DropKind.After) insertIndex++;

                // Capture original indices of dragged nodes relative to siblings before removals
                var draggedOriginalIndices = new List<int>();
                foreach (var d in dragged)
                {
                    int idx = siblings.IndexOf(d);
                    if (idx >= 0)
                    {
                        draggedOriginalIndices.Add(idx);
                    }
                }
                // Count how many dragged nodes are positioned before the insertion index; those removals shift the insertion index left
                int removalOffset = draggedOriginalIndices.Count(i => i < insertIndex);

                // When moving between parents, remove first, then insert in order.
                foreach (var d in dragged)
                {
                    if (IsDescendantOf(target, d)) continue; // prevent inserting ancestor below descendant
                }

                // Remove all dragged nodes prior to insert to get stable indices.
                foreach (var d in dragged)
                {
                    d.Remove();
                }

                // Adjust insertion index after removals so we insert at the visualized spot
                insertIndex -= removalOffset;
                if (insertIndex < 0) insertIndex = 0;

                // Insert maintaining relative order.
                int currentIndex = insertIndex;
                foreach (var d in dragged)
                {
                    if (currentIndex > siblings.Count) currentIndex = siblings.Count; // clamp
                    siblings.Insert(currentIndex, d);
                    currentIndex++;
                }
                break;
        }

        // Refresh selection adornment
        Invalidate();
    }

    private static void CollectVisible(TreeNode node, List<TreeNode> list)
    {
        list.Add(node);
        foreach (TreeNode child in node.Nodes)
        {
            CollectVisible(child, list);
        }
    }

    private void UpdateDropAdornment(TreeNode? node, DropKind kind)
    {
        if (ReferenceEquals(_hotDropNode, node) && _dropKind == kind) return;

        var set = new HashSet<TreeNode>();
        if (_hotDropNode != null)
        {
            set.Add(_hotDropNode);
            if (_hotDropNode.PrevNode != null) set.Add(_hotDropNode.PrevNode);
            if (_hotDropNode.NextNode != null) set.Add(_hotDropNode.NextNode);
            // Invalidate old parent if its dashed border may have been shown
            if ((_dropKind == DropKind.Before || _dropKind == DropKind.After) && _hotDropNode.Parent != null)
                set.Add(_hotDropNode.Parent);
        }
        if (node != null)
        {
            set.Add(node);
            if (node.PrevNode != null) set.Add(node.PrevNode);
            if (node.NextNode != null) set.Add(node.NextNode);
            // Invalidate new parent so dashed border can appear when appropriate
            if ((kind == DropKind.Before || kind == DropKind.After) && node.Parent != null)
                set.Add(node.Parent);
        }

        Rectangle inv = Rectangle.Empty;
        foreach (var n in set)
        {
            inv = inv.IsEmpty ? RowRect(n) : Rectangle.Union(inv, RowRect(n));
        }

        _hotDropNode = node;
        _dropKind = kind;

        if (!inv.IsEmpty) Invalidate(inv);
    }

    private void ClearDropAdornment()
    {
        if (_hotDropNode != null)
        {
            Rectangle inv = RowRect(_hotDropNode);
            if (_dropKind == DropKind.Before)
            {
                var prev = _hotDropNode.PrevNode;
                if (prev != null)
                    inv = Rectangle.Union(inv, RowRect(prev));
            }
            else if (_dropKind == DropKind.After)
            {
                var next = _hotDropNode.NextNode;
                if (next != null)
                    inv = Rectangle.Union(inv, RowRect(next));
            }
            // Also invalidate parent row if it was highlighted
            if ((_dropKind == DropKind.Before || _dropKind == DropKind.After) && _hotDropNode.Parent != null)
            {
                inv = Rectangle.Union(inv, RowRect(_hotDropNode.Parent));
            }
            _hotDropNode = null;
            _dropKind = DropKind.None;
            Invalidate(inv);
        }
    }

    private Rectangle RowRect(TreeNode n)
    {
        return new Rectangle(0, n.Bounds.Top, ClientSize.Width, n.Bounds.Height);
    }

    private void AutoScrollWhileDragging(Point pt)
    {
        const int margin = 24;
        if (pt.Y < margin) ScrollLine(-1);
        else if (pt.Y > ClientSize.Height - margin) ScrollLine(+1);
    }

    private void ScrollLine(int dir)
    {
        const int WM_VSCROLL = 0x0115;
        const int SB_LINEUP = 0;
        const int SB_LINEDOWN = 1;
        int code = dir < 0 ? SB_LINEUP : SB_LINEDOWN;
        SendMessage(Handle, WM_VSCROLL, (IntPtr)code, IntPtr.Zero);
    }

    private (TreeNode? node, DropKind kind) GetDropAt(Point clientPt, TreeNode[] dragged)
    {
        TreeNode? over = GetNodeAtRowY(clientPt.Y);
        if (over == null) return (null, DropKind.None);

        if (IsNodeOrDescendantOfAny(over, dragged)) return (null, DropKind.None);

        Rectangle r = over.Bounds;
        int topZone = r.Top + r.Height / 4;
        int bottomZone = r.Bottom - r.Height / 4;

        if (clientPt.Y < topZone) return (over, DropKind.Before);
        if (clientPt.Y > bottomZone)
        {
            // If dropping "after" an expanded node, treat as inserting as first child (IntoFirst)
            if (over.IsExpanded && over.Nodes.Count > 0)
                return (over, DropKind.IntoFirst);
            return (over, DropKind.After);
        }

        if (CanDropInto(over, dragged)) return (over, DropKind.Into);

        var midKind = (clientPt.Y - topZone < bottomZone - clientPt.Y) ? DropKind.Before : DropKind.After;
        if (midKind == DropKind.After && over.IsExpanded && over.Nodes.Count > 0)
            return (over, DropKind.IntoFirst);
        return (over, midKind);
    }

    private static bool CanDropInto(TreeNode target, TreeNode[] dragged)
    {
        return true;
    }

    private static bool IsDescendantOf(TreeNode node, TreeNode potentialAncestor)
    {
        for (TreeNode? p = node.Parent; p != null; p = p.Parent)
        {
            if (ReferenceEquals(p, potentialAncestor)) return true;
        }
        return false;
    }

    private static bool IsNodeOrDescendantOfAny(TreeNode node, TreeNode[] set)
    {
        foreach (TreeNode n in set)
        {
            if (ReferenceEquals(node, n) || IsDescendantOf(node, n)) return true;
        }
        return false;
    }

    #endregion

    #region Layout helpers

    private void LayoutNodeRow(TreeNode node, Rectangle row, out Rectangle chevron, out Rectangle state, out Rectangle image, out Rectangle text)
    {
        int spacing = 0;// DpiScaleI(3);
        int chevronBox = ChevronBoxSize;

        int left = DisplayRectangle.Left + node.Level * Indent;


        int chevronSize = Math.Min(chevronBox, row.Height - 2);
        int chevX = left + (chevronBox - chevronSize) / 2;
        int chevY = row.Top + (row.Height - chevronSize) / 2;
        chevron = node.Nodes.Count > 0
            ? new Rectangle(chevX, chevY, chevronSize, chevronSize)
            : Rectangle.Empty;

        int cursor = left + chevronBox + spacing;

        state = Rectangle.Empty;
        if (StateImageList != null && node.StateImageIndex >= 0 && node.StateImageIndex < StateImageList.Images.Count)
        {
            Size sz = StateImageList.ImageSize;
            int y = row.Top + (row.Height - sz.Height) / 2;
            state = new Rectangle(cursor, y, sz.Width, sz.Height);
            cursor += sz.Width + spacing;
        }

        image = Rectangle.Empty;
        if (ImageList != null)
        {
            int idx = node.ImageIndex;
            if (idx < 0 && !string.IsNullOrEmpty(node.ImageKey))
            {
                idx = ImageList.Images.IndexOfKey(node.ImageKey);
            }

            if (idx >= 0 && idx < ImageList.Images.Count)
            {
                Size sz = ImageList.ImageSize;
                int y = row.Top + (row.Height - sz.Height) / 2;
                image = new Rectangle(cursor, y, sz.Width, sz.Height);
                cursor += sz.Width + spacing;
            }
        }

        int right = ClientSize.Width;
        // Vertically center the text rectangle within the row using the effective font height
        int fontHeight = (node.NodeFont ?? this.Font).Height;
        int textHeight = Math.Min(row.Height, fontHeight);
        int textY = row.Top + (row.Height - textHeight) / 2;
        text = new Rectangle(cursor, textY, Math.Max(0, right - cursor - 1), textHeight);
    }

    #endregion

    #region Modifiers

    private static Keys ModsFromWParam(IntPtr wParam)
    {
        const int MK_CONTROL = 0x0008;
        const int MK_SHIFT = 0x0004;
        int wp = (int)wParam;
        Keys k = Keys.None;
        if ((wp & MK_CONTROL) != 0) k |= Keys.Control;
        if ((wp & MK_SHIFT) != 0) k |= Keys.Shift;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) k |= Keys.Alt;
        return k;
    }

    private Keys EffectiveModifiers()
    {
        return _inMouseSeq ? _modsAtMouseDown : Control.ModifierKeys;
    }

    #endregion

    private partial bool BeforeWndProcBase(ref Message m)
    {
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_LBUTTONDBLCLK = 0x0203;
        const int TV_FIRST = 0x1100;
        const int TVM_ENSUREVISIBLE = TV_FIRST + 20;

        switch (m.Msg)
        {
            case WM_LBUTTONDOWN:
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);

                    if (!Focused)
                    {
                        Focus();
                    }

                    _modsAtMouseDown = ModsFromWParam(m.WParam);
                    _inMouseSeq = true;

                    var node = GetNodeAtRowY(y);
                    if (node != null)
                    {
                        // --- GLYPH FIRST (use your LayoutNodeRow-based rect) ---  
                        var glyph = GetChevronRect(node);  // <-- uses the same layout as drawing  
                        if (!glyph.IsEmpty)
                        {
                            var hit = glyph; hit.Inflate(DpiScaleI(2), DpiScaleI(2));
                            if (hit.Contains(x, y))
                            {
                                node.Toggle();
                                _eatGlyphSeq = true;
                                _eatRowClickSeq = false;
                                _mouseDownNode = node;

                                _suppressClickSelection = true;  // <- suppress selection on the upcoming MouseUp  
                                _dragCandidateNode = null;
                                _initiatedDrag = false;
                                return true; // swallow native  
                            }
                        }

                        // --- ROW SELECTION PATH ---  
                        _eatRowClickSeq = true;
                        _mouseDownNode = node;
                        OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));
                        return true;
                    }
                    break;
                }

            case WM_LBUTTONUP:
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);

                    if (_eatGlyphSeq)
                    {
                        _eatGlyphSeq = false;
                        _inMouseSeq = false;

                        // Raise managed MouseUp for listeners, but selection will be suppressed.  
                        OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));
                        _suppressClickSelection = false; // one-shot  
                        return true;
                    }

                    if (_eatRowClickSeq)
                    {
                        var node = GetNodeAtRowY(y);
                        if (node != null && node == _mouseDownNode)
                            AfterClickSelect?.Invoke(this, new TreeViewEventArgs(node));

                        _eatRowClickSeq = false;
                        _mouseDownNode = null;
                        _inMouseSeq = false;
                        OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));
                        return true;
                    }

                    _inMouseSeq = false;
                    break;
                }

            case WM_LBUTTONDBLCLK:
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);

                    if (!Focused)
                    {
                        Focus();
                    }

                    var node = GetNodeAtRowY(y);
                    if (node != null)
                    {
                        // Don?t let chevron dbl-click do it twice; glyph path already handles toggling  
                        var glyph = GetChevronRect(node);   // uses your LayoutNodeRow-based rect  
                        if (!glyph.IsEmpty && glyph.Contains(x, y))
                        {
                            // we already toggle on WM_LBUTTONDOWN in glyph path; just swallow  
                            OnDoubleClick(EventArgs.Empty);
                            return true;
                        }

                        // Full-row dbl-click behavior (match label dbl-click): toggle if it has children  
                        if (node.Nodes.Count > 0)
                            node.Toggle();

                        // Fire the usual managed events so app code sees a node dbl-click  
                        var ea = new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 2, x, y);
                        OnNodeMouseDoubleClick(ea);
                        OnDoubleClick(EventArgs.Empty);
                        return true; // swallow native so there?s no duplicate behavior  
                    }

                    break;
                }
            case TVM_ENSUREVISIBLE:
                {
                    if (m.Msg == TVM_ENSUREVISIBLE)
                    {
                        // lParam = HTREEITEM
                        var node = NodeFromHandle(m.LParam);
                        if (node != null)
                        {
                            EnsureVisibleRequested?.Invoke(this, new EnsureVisibleEventArgs(node));
                            // swallow native scrolling since we handle it via custom scroller
                            return true;
                        }
                    }
                    break;
                }
        }

        return false;
    }

    private partial bool AfterWndProcBase(ref Message m)
    {
        const int TV_FIRST = 0x1100;
        const int TVM_INSERTITEMA = TV_FIRST + 0;   // ANSI insert  
        const int TVM_INSERTITEMW = TV_FIRST + 50;  // Unicode insert  
        const int TVM_DELETEITEM = TV_FIRST + 1;   // delete  

        bool changed =
            m.Msg == TVM_INSERTITEMA ||
            m.Msg == TVM_INSERTITEMW ||
            m.Msg == TVM_DELETEITEM;

        if (changed)
        {
            // coalesce multiple inserts/removes in one UI tick  
            if (_mutateQueued) return true;
            _mutateQueued = true;
            BeginInvoke(new Action(() =>
            {
                _mutateQueued = false;
                StructureMutated?.Invoke(this, EventArgs.Empty);
            }));
        }

        return false;
    }

    #region EnsureVisibility
    public sealed class EnsureVisibleEventArgs : EventArgs
    {
        public EnsureVisibleEventArgs(TreeNode node) => Node = node;
        public TreeNode Node { get; }
    }

    // In MultiSelectTreeView:
    public event EventHandler<EnsureVisibleEventArgs>? EnsureVisibleRequested;

    // Cache the private NodeFromHandle(IntPtr) method once
    private Func<IntPtr, TreeNode?>? _nodeFromHandle;

    private TreeNode? NodeFromHandle(IntPtr hItem)
    {
        if (_nodeFromHandle == null)
        {
            var mi = typeof(TreeView).GetMethod("NodeFromHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (mi != null)
            {
                _nodeFromHandle = (Func<IntPtr, TreeNode?>)mi.CreateDelegate(typeof(Func<IntPtr, TreeNode?>), this);
            }
            else
            {
                return null;
            }
        }
        return _nodeFromHandle(hItem);
    }


    #endregion

    private static TreeNode[] ExtractDraggedNodes(IDataObject? data)
    {
        if (data?.GetDataPresent(typeof(TreeNode[])) == true)
            return (TreeNode[])data.GetData(typeof(TreeNode[]))!;
        if (data?.GetDataPresent(typeof(TreeNode)) == true)
            return new[] { (TreeNode)data.GetData(typeof(TreeNode))! };
        return Array.Empty<TreeNode>();
    }

    #region Drop Validation / Interception Events
    /// <summary>
    /// Arguments for <see cref="MultiSelectTreeView.ValidateSortingDrop"/> allowing a consumer to allow / deny or adjust a pending drag-over adornment.
    /// </summary>
    public sealed class ValidateDropEventArgs : EventArgs
    {
        internal ValidateDropEventArgs(TreeNode[] dragged, TreeNode target, DropKind kind)
        {
            DraggedNodes = dragged;
            TargetNode = target;
            Kind = kind;
            Allow = true;
        }
        public TreeNode[] DraggedNodes { get; }
        /// <summary>The proposed target node (can be reassigned).</summary>
        public TreeNode? TargetNode { get; set; }
        /// <summary>The proposed drop kind (can be reassigned).</summary>
        public DropKind Kind { get; set; }
        /// <summary>Set to false to disallow the drop at this location.</summary>
        public bool Allow { get; set; }
    }

    /// <summary>
    /// Raised during drag-over to let consumer validate / modify the potential drop target / kind.
    /// </summary>
    public event EventHandler<ValidateDropEventArgs>? ValidateSortingDrop;

    /// <summary>
    /// Arguments for <see cref="MultiSelectTreeView.NodeSortingDropped"/> allowing interception before native reorder occurs.
    /// </summary>
    public sealed class DroppingEventArgs : EventArgs
    {
        internal DroppingEventArgs(TreeNode[] dragged, TreeNode target, DropKind kind)
        {
            DraggedNodes = dragged;
            TargetNode = target;
            Kind = kind;
        }
        public TreeNode[] DraggedNodes { get; }
        public TreeNode TargetNode { get; set; }
        public DropKind Kind { get; set; }
        /// <summary>Set true to cancel the drop entirely.</summary>
        public bool Cancel { get; set; }
        /// <summary>If true (default = current control setting) native reorder will be performed after the event unless canceled.</summary>
        public bool PerformNativeReorder { get; set; }
    }

    /// <summary>
    /// Raised just before a drop is committed. Handlers can modify target/kind, cancel, or suppress native reordering.
    /// </summary>
    public event EventHandler<DroppingEventArgs>? NodeSortingDropped;
    #endregion

    private static void DrawChevronRightFast(Graphics g, Pen pen, Rectangle r)
    {
        int s = Math.Min(r.Width, r.Height);
        int x0 = r.Left + (r.Width - s) / 2;
        int y0 = r.Top + (r.Height - s) / 2;

        float mx = s * 0.40f;
        float my = s * 0.28f;

        float leftX = x0 + mx;
        float midX = x0 + s - mx;
        float midY = y0 + s * 0.50f;
        float upY = y0 + my;
        float downY = y0 + s - my;

        g.DrawLine(pen, leftX, upY, midX, midY);
        g.DrawLine(pen, leftX, downY, midX, midY);
    }

    private static void DrawChevronDownFast(Graphics g, Pen pen, Rectangle r)
    {
        int s = Math.Min(r.Width, r.Height);
        int x0 = r.Left + (r.Width - s) / 2;
        int y0 = r.Top + (r.Height - s) / 2;

        float mx = s * 0.28f;
        float my = s * 0.40f;

        float midX = x0 + s * 0.50f;
        float topY = y0 + my;
        float botY = y0 + s - my;
        float leftX = x0 + mx;
        float rightX = x0 + s - mx;

        g.DrawLine(pen, leftX, topY, midX, botY);
        g.DrawLine(pen, rightX, topY, midX, botY);
    }
}