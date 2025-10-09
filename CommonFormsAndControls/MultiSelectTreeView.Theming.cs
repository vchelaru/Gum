using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CommonFormsAndControls;
#nullable enable
public partial class MultiSelectTreeView
{
    #region Appearance

    public Color HoverBgColor { get; set; } = Color.Transparent;
    public Color SelectedBorderColor { get; set; } = Color.Blue;

    public Color ChevronColor { get; set; } = Color.Empty; // empty -> follows ForeColor
    public float ChevronThickness { get; set; } = 2.0f;    // DIP
    public int ChevronBoxSize { get; set; } = 14;          // DIP

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

    private bool _swallowSequence;
    private Rectangle _lastGlyphRect;

    private ImageAttributes ImageAttributes;

    private enum DropKind { None, Before, Into, After }

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

        AfterSelect += (_, _) => Invalidate();
        GotFocus += (_, _) => Invalidate();
        LostFocus += (_, _) => Invalidate();
        MouseLeave += (_, _) => SetHotNode(null);
        MouseDown += TreeView_MouseDown;
        MouseMove += (_, e) => SetHotNode(GetNodeAtRowY(e.Location.Y), e.Location);
        AfterExpand += (_, _) => Invalidate();
        AfterCollapse += (_, _) => Invalidate();
        ForeColorChanged += (_, _) => Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
        const int TVS_EX_DOUBLEBUFFER = 0x0004;
        SendMessage(Handle, TVM_SETEXTENDEDSTYLE, (IntPtr)TVS_EX_DOUBLEBUFFER, (IntPtr)TVS_EX_DOUBLEBUFFER);
    }

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

    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = false;

        bool isSelected = SelectedNodes.Contains(e.Node);
        bool isHot = e.Node == _hotNode;

        Rectangle row = new Rectangle(0, e.Bounds.Top, ClientSize.Width, e.Bounds.Height);
        Color rowBg = isHot ? HoverBgColor : BackColor;

        using (SolidBrush b = new SolidBrush(rowBg))
        {
            e.Graphics.FillRectangle(b, row);
        }

        Rectangle rChevron;
        Rectangle rState;
        Rectangle rImage;
        Rectangle rText;
        LayoutNodeRow(e.Node, row, out rChevron, out rState, out rImage, out rText);

        if (e.Node.Nodes.Count > 0)
        {
            Rectangle box = GetChevronRect(e.Node);
            if (!box.IsEmpty)
            {
                using (SmoothingScope scope = new SmoothingScope(e.Graphics))
                {
                    Color color = (e.Node == _hotChevronNode)
                        ? SelectedBorderColor
                        : (ChevronColor.IsEmpty ? ForeColor : ChevronColor);

                    using (Pen pen = new Pen(color, Math.Max(1f, DpiScale(ChevronThickness))))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        pen.LineJoin = LineJoin.Round;

                        if (e.Node.IsExpanded)
                        {
                            DrawChevronDown(e.Graphics, pen, box);
                        }
                        else
                        {
                            DrawChevronRight(e.Graphics, pen, box);
                        }
                    }
                }
            }
        }

        if (!rState.IsEmpty)
        {
            StateImageList.Draw(e.Graphics, rState.Location, e.Node.StateImageIndex);
        }

        if (!rImage.IsEmpty)
        {
            int idx = e.Node.ImageIndex;
            if (idx < 0 && !string.IsNullOrEmpty(e.Node.ImageKey))
            {
                idx = ImageList.Images.IndexOfKey(e.Node.ImageKey);
            }
            if (idx >= 0 && idx < ImageList.Images.Count)
            {

                var img = ImageList.Images[idx];

                // If you want a fallback when attrs aren't ready:
                if (ImageAttributes == null)
                {
                    // Fallback: normal draw
                    ImageList.Draw(e.Graphics, rImage.Location, idx);
                }
                else
                {
                    // Tinted draw — respects ForeColor (including alpha) via your cached ImageAttributes
                    e.Graphics.DrawImage(
                        img,
                        rImage,                         // destination rect
                        0, 0, img.Width, img.Height,    // source rect
                        GraphicsUnit.Pixel,
                        ImageAttributes);
                }
            }
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.Node.Text,
            e.Node.NodeFont ?? Font,
            rText,
            ForeColor,
            Color.Transparent,
            TextFormatFlags.NoClipping | TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.EndEllipsis);

        if (isSelected)
        {
            using (Pen p = new Pen(SelectedBorderColor))
            {
                e.Graphics.DrawRectangle(p, Rectangle.Inflate(row, -1, -1));
            }
        }

        if (_hotDropNode == e.Node && _dropKind != DropKind.None)
        {
            using (Pen pen = new Pen(SelectedBorderColor, Math.Max(2f, DpiScale(2f))))
            {
                switch (_dropKind)
                {

                    //note: because we are taking over node-drop externally, and "before/after"
                    // is not supported by that, we'll just forward the visuals to "into"
                    case DropKind.Before:
                        //e.Graphics.DrawLine(pen, 0, e.Bounds.Top, ClientSize.Width, e.Bounds.Top);
                        //break;
                    case DropKind.After:
                        //e.Graphics.DrawLine(pen, 0, e.Bounds.Bottom - 1, ClientSize.Width, e.Bounds.Bottom - 1);
                        //break;
                    case DropKind.Into:
                        using (Pen p = new Pen(Color.FromArgb(160, SelectedBorderColor), 1f))
                        {
                            Rectangle rr = Rectangle.Inflate(new Rectangle(0, e.Bounds.Top, ClientSize.Width, e.Bounds.Height), -1, -1);
                            e.Graphics.DrawRectangle(p, rr);
                        }
                        break;
                }
            }
        }
    }

    private Rectangle GetChevronRect(TreeNode node)
    {
        if (node == null || node.Bounds == Rectangle.Empty) return Rectangle.Empty;

        Rectangle row = new (0, node.Bounds.Top, ClientSize.Width, node.Bounds.Height);
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

    private float DpiScale(float value)
    {
        using (Graphics g = CreateGraphics())
        {
            return value * (g.DpiX / 96f);
        }
    }

    private int DpiScaleI(int v)
    {
        return (int)Math.Round(DpiScale(v));
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

    protected override void OnForeColorChanged(EventArgs e)
    {
        ImageAttributes = GetAttrs(ForeColor);
        base.OnForeColorChanged(e);
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

    private void TreeView_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            TreeNode node = this.GetNodeAtRowY(e.Location.Y);
            _initiatedDrag = false;          // reset every left-press   
            _mouseDownPoint = e.Location;
            _dragCandidateNode = node;
            if (!mSelectedNodes.Contains(node))
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
        }

    }

    #endregion

    #region Drag & Drop

    protected override void OnDragEnter(DragEventArgs e)
    {
        TreeNode[] dragged = ExtractDraggedNodes(e.Data);
        e.Effect = dragged.Length > 0
            ? ((e.AllowedEffect & DragDropEffects.Move) != 0 ? DragDropEffects.Move : DragDropEffects.Copy)
            : DragDropEffects.None;

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

            // Reparent/insert logic lives elsewhere.
        }
        finally
        {
            ClearDropAdornment();
            base.OnDragDrop(e);
        }
    }

    private void UpdateDropAdornment(TreeNode? node, DropKind kind)
    {
        if (ReferenceEquals(_hotDropNode, node) && _dropKind == kind) return;

        Rectangle inv = Rectangle.Empty;
        if (_hotDropNode != null) inv = Rectangle.Union(inv, RowRect(_hotDropNode));
        if (node != null) inv = inv.IsEmpty ? RowRect(node) : Rectangle.Union(inv, RowRect(node));

        _hotDropNode = node;
        _dropKind = kind;

        if (!inv.IsEmpty) Invalidate(inv);
    }

    private void ClearDropAdornment()
    {
        if (_hotDropNode != null)
        {
            Rectangle r = RowRect(_hotDropNode);
            _hotDropNode = null;
            _dropKind = DropKind.None;
            Invalidate(r);
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
        if (clientPt.Y > bottomZone) return (over, DropKind.After);

        if (CanDropInto(over, dragged)) return (over, DropKind.Into);

        return (clientPt.Y - topZone < bottomZone - clientPt.Y)
            ? (over, DropKind.Before)
            : (over, DropKind.After);
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
        int spacing = DpiScaleI(3);

        int left = DisplayRectangle.Left + node.Level * Indent;

        int chevronBox = Math.Max(Indent, DpiScaleI(ChevronBoxSize));
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
        text = new Rectangle(cursor, row.Top, Math.Max(0, right - cursor - 1), row.Height);
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

    private static TreeNode[] ExtractDraggedNodes(IDataObject data)
    {
        if (data.GetDataPresent(typeof(TreeNode[])))
            return (TreeNode[])data.GetData(typeof(TreeNode[]));
        if (data.GetDataPresent(typeof(TreeNode)))
            return new[] { (TreeNode)data.GetData(typeof(TreeNode)) };
        return Array.Empty<TreeNode>();
    }
}