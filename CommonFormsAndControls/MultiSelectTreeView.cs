using System;
using System.Timers;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CommonFormsAndControls
{
    public class MultiSelectTreeView : TreeView, IEnumerable<TreeNode>
    {
        #region Fields

		private Dictionary<TreeNode, Color> mOriginalColors;
		private System.Timers.Timer mSearchTimer;
		private string mSearchString;
		private char mFirstSearchChar;
		private char mLastSearchChar;
		private const double TIMERVALUE = 750;
		bool mNewKeyMatchesFirstKey;
		private TreeNode mSelectedNode;
        private bool mSelectedNodeChanged = false;
        private List<TreeNode> mSelectedNodes = null;

        private TreeNode nodeOnDragStart;


        #endregion

        #region Properties
        
        public bool AlwaysHaveOneNodeSelected
        {
            get;
            set;
        }

        public List<TreeNode> SelectedNodes
		{
			get
			{
				return mSelectedNodes;
			}
			set
			{
				ClearSelectedNodes();
				if( value != null )
				{
					foreach (TreeNode node in value)
					{
						SetNodeSelected(node, true);
					}
					OnAfterSelect(new TreeViewEventArgs(mSelectedNode));
				}
			}
		}

		// Note we use the new keyword to Hide the native treeview's SelectedNode property.
		public new TreeNode SelectedNode
		{
			get
            { 
                // March 1, 2012
                // This caches off
                // mSelectedNode because
                // we can't relyon the base
                // TreeView to tell us the SelectedNode - 
                // we've overidden that functionalty to get
                // multi-selection.  However, this causes a problem
                // when a TreeNode is removed.  The MultiSelectTreeView
                // isn't notified so it still thinks it's selected.  Therefore
                // we have to test the node to see if it's still part of the TreeView
                // by checking its TreeView property.
                if (mSelectedNode != null)
                {
                    if (mSelectedNode.TreeView != null)
                    {
                        return mSelectedNode;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return mSelectedNode;
                }
            }
			set
			{
                // Don't do anything if it's the same selection
                if (value == mSelectedNode)
                {
                    return;
                }

				ClearSelectedNodes();
				if (value != null)
				{
					ReactToClickedNode(value);

                    // ReactToClickedNode has event raising already, so this results in the event being raised twice:
					//OnAfterSelect(new TreeViewEventArgs(mSelectedNode));
				}
			}
        }

        public MultiSelectBehavior MultiSelectBehavior
        {
            get;
            set;
        }

        #endregion

        #region Events


        public event TreeViewEventHandler AfterClickSelect;

        #endregion

        #region Event Methods

        protected override void OnGotFocus(EventArgs e)
        {
            // Make sure at least one node has a selection
            // this way we can tab to the ctrl and use the 
            // keyboard to select nodes
#if !DEBUG
            try
#endif
            {
                if (mSelectedNode == null && this.TopNode != null && AlwaysHaveOneNodeSelected)
                {
                    SetNodeSelected(this.TopNode, true);
                }

                base.OnGotFocus(e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
        }

        // If false, then only select on a click
        public bool IsSelectingOnPush { get; set; } = true;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // If the user clicks on a node that was not
            // previously selected, select it now.
//			BeginUpdate();
#if !DEBUG
            try
#endif
            {
                base.SelectedNode = null;

                TreeNode previousSelectedNode = mSelectedNode;
                var previousCount = SelectedNodes.Count;

                TreeNode node = this.GetNodeAt(e.Location);
                if (node != null)
                {
                    int extraOnLeft = 0;

                    if (node.ImageIndex != -1)
                    {
                        extraOnLeft = -20;
                    }

                    int leftBound = node.Bounds.X + extraOnLeft; // Allow user to click on image
                    int rightBound = node.Bounds.Right + 10; // Give a little extra room
                    if (e.Location.X > leftBound && e.Location.X < rightBound)
                    {
                        if (mSelectedNodes.Contains(node) && e.Button == MouseButtons.Right && ModifierKeys != Keys.None)
                        {
                        }
                        else if ((ModifierKeys == Keys.None && MultiSelectBehavior != MultiSelectBehavior.RegularClick) && (mSelectedNodes.Contains(node)))
                        {
                            // Potential Drag Operation
                            // Let Mouse Up do select
                        }
                        else if(IsSelectingOnPush || ModifierKeys == Keys.Shift || ModifierKeys == Keys.Control || 
                            e.Button == MouseButtons.Right)
                        {
                            // For gum we want to prevent selection on a push. Should be on a click
                            ReactToClickedNode(node);
                        }
                    }
                }
                else
                {
                    ReactToClickedNode(node);
                }

                base.OnMouseDown(e);

                mSelectedNodeChanged = previousSelectedNode != mSelectedNode;

                if(!mSelectedNodeChanged)
                {
                    // If multiples are selected but no keys are held down, then we're going to deselect
                    // back down to 1.
                    if(mSelectedNodes.Count > 1 && ModifierKeys == Keys.None)
                    {
                        mSelectedNodeChanged = true;
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
//			EndUpdate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // If the clicked on a node that WAS previously
            // selected then, reselect it now. This will clear
            // any other selected nodes. e.g. A B C D are selected
            // the user clicks on B, now A C & D are no longer selected.
            // unless the user right clicked on a node that is already selected
            // then they probably want the context menu for the selected items.
#if !DEBUG
            try
#endif
            {
                // Check to see if a node was clicked on 
                TreeNode node = this.GetNodeAt(e.Location);
                if (node != null)
                {
                    var shouldSelect = (ModifierKeys == Keys.None && MultiSelectBehavior != MultiSelectBehavior.RegularClick) &&
                        ((mSelectedNodes.Count > 1 && mSelectedNodes.Contains(node)) || IsSelectingOnPush == false) &&
                        e.Button != MouseButtons.Right;

                    if (shouldSelect)
                    {
                        int extraOnLeft = 0;

                        if (node.ImageIndex != -1)
                        {
                            extraOnLeft = -20;
                        }

                        int leftBound = node.Bounds.X + extraOnLeft; // Allow user to click on image
                        int rightBound = node.Bounds.Right + 10; // Give a little extra room
                        if (e.Location.X > leftBound && e.Location.X < rightBound)
                        {
                            ReactToClickedNode(node);
                        }
                    }

                    if ((mSelectedNodeChanged || IsSelectingOnPush == false ) && AfterClickSelect != null)
                    {
                        AfterClickSelect(this, new TreeViewEventArgs(node));
                    }
                }

                // Not sure why the drag drop events are not raised automatically, trying to do so manually...
                if(nodeOnDragStart != null)
                {
                    DragEventArgs dragEventArgs = new DragEventArgs(null, 0, Cursor.Position.X, Cursor.Position.Y, DragDropEffects.All, DragDropEffects.All);

                    this.OnDragDrop(dragEventArgs);
                }

                nodeOnDragStart = null;


                base.OnMouseUp(e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
        }

        protected override void OnItemDrag(ItemDragEventArgs e)
        {
            // If the user drags a node and the node being dragged is NOT
            // selected, then clear the active selection, select the
            // node being dragged and drag it. Otherwise if the node being
            // dragged is selected, drag the entire selection.
#if !DEBUG
            try
#endif
            {
                TreeNode node = e.Item as TreeNode;

                if (node != null)
                {
                    if (!mSelectedNodes.Contains(node))
                    {
                        // These were commented out from hash 3e123616856be6717c66b49b4f5dfcd5f9136907
                        // Uncommented because they appear to fix secondary issue in #1143
                        SelectSingleNode(node);
                        SetNodeSelected(node, true);
                    }
                }

                nodeOnDragStart = e.Item as TreeNode;

                base.OnItemDrag(e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
        }



        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            // Never allow base.SelectedNode to be set!
#if !DEBUG
            try
#endif
            {
                base.SelectedNode = null;
                e.Cancel = true;

                base.OnBeforeSelect(e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
        }

        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            // Never allow base.SelectedNode to be set!
#if !DEBUG
            try
#endif
            {
                //if(mSelectedNodes.Count > 0)
                //{
                //    var firstNode = mSelectedNodes[0];
                //    firstNode.EnsureVisible();
                //}
                // should we ensure all?
                foreach(var node in mSelectedNodes)
                {
                    node.EnsureVisible();
                }
                base.OnAfterSelect(e);
                base.SelectedNode = null;
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                if (mSearchTimer.Enabled)
                {
                    mSearchString += (e.KeyChar).ToString();

                    if (mNewKeyMatchesFirstKey == false)
                        mNewKeyMatchesFirstKey = false;
                    else
                        mNewKeyMatchesFirstKey = (mFirstSearchChar == e.KeyChar);
                    mSearchTimer.Interval = TIMERVALUE;
                }
                else
                {
                    mFirstSearchChar = e.KeyChar;
                    mSearchString = e.KeyChar.ToString();
                    mSearchTimer.Start();
                    if (mSelectedNode != null && FindAndSelectNode(mSelectedNode))
                    {
                        return;
                    }
                    else if (this.TopNode != null && FindAndSelectNode(this.TopNode))
                    {
                        return;
                    }
                    return;
                }

                TreeNode ndCurrent = mSelectedNode;
                if (ndCurrent.Text.StartsWith(mSearchString))
                {
                    return;
                }
                else if (FindAndSelectNode(ndCurrent))
                {
                    return;
                }
                else if (mNewKeyMatchesFirstKey)
                {
                    mSearchString = e.KeyChar.ToString();
                    if (FindAndSelectNode(ndCurrent))
                        return;
                    else if (FindAndSelectNode(this.TopNode))
                        return;
                }
                else
                {
                    if (FindAndSelectNode(this.TopNode))
                        return;
                }
            }
#if !DEBUG
            finally
#endif
            {
                mLastSearchChar = e.KeyChar;
                base.OnKeyPress(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Handle all possible key strokes for the control.
            // including navigation, selection, etc.

            if (e.KeyCode == Keys.ShiftKey) return;
            if (e.KeyCode == Keys.ControlKey) return;

            //this.BeginUpdate();
            bool bShift = (ModifierKeys == Keys.Shift);

#if !DEBUG
            try
#endif
            {
                // Nothing is selected in the tree, this isn't a good state
                // select the top node
                if (mSelectedNode == null && this.TopNode != null)
                {
                    SetNodeSelected(this.TopNode, true);
                }

                // Nothing is still selected in the tree, this isn't a good state, leave.
                if (mSelectedNode == null) return;

                var isAltDown = (e.Modifiers & Keys.Alt) == Keys.Alt;
                var isControlDown = (e.Modifiers & Keys.Control) == Keys.Control;

                if (e.KeyCode == Keys.Left)
                {
                    if (mSelectedNode.IsExpanded && mSelectedNode.Nodes.Count > 0)
                    {
                        // Collapse an expanded node that has children
                        mSelectedNode.Collapse();
                    }
                    else if (mSelectedNode.Parent != null)
                    {
                        // Node is already collapsed, try to select its parent.
                        SelectSingleNode(mSelectedNode.Parent);
                    }
                }
                else if (e.KeyCode == Keys.Right)
                {
                    if (!mSelectedNode.IsExpanded)
                    {
                        // Expand a collpased node's children
                        mSelectedNode.Expand();
                    }
                    else
                    {
                        // Node was already expanded, select the first child
                        SelectSingleNode(mSelectedNode.FirstNode);
                    }
                }
                else if (e.KeyCode == Keys.Up && !isAltDown && !isControlDown)
                {
                    // Select the previous node
                    if (mSelectedNode.PrevVisibleNode != null)
                    {
                        ReactToClickedNode(mSelectedNode.PrevVisibleNode);
                    }
                }
                else if (e.KeyCode == Keys.Down && !isAltDown && !isControlDown)
                {
                    // Select the next node
                    if (mSelectedNode.NextVisibleNode != null)
                    {
                        ReactToClickedNode(mSelectedNode.NextVisibleNode);
                    }
                }
                else if (e.KeyCode == Keys.Home)
                {
                    if (bShift)
                    {
                        if (mSelectedNode.Parent == null)
                        {
                            // Select all of the root nodes up to this point 
                            if (this.Nodes.Count > 0)
                            {
                                ReactToClickedNode(this.Nodes[0]);
                            }
                        }
                        else
                        {
                            // Select all of the nodes up to this point under this nodes parent
                            ReactToClickedNode(mSelectedNode.Parent.FirstNode);
                        }
                    }
                    else
                    {
                        // Select this first node in the tree
                        if (this.Nodes.Count > 0)
                        {
                            SelectSingleNode(this.Nodes[0]);
                        }
                    }
                }
                else if (e.KeyCode == Keys.End)
                {
                    if (bShift)
                    {
                        if (mSelectedNode.Parent == null)
                        {
                            // Select the last ROOT node in the tree
                            if (this.Nodes.Count > 0)
                            {
                                ReactToClickedNode(this.Nodes[this.Nodes.Count - 1]);
                            }
                        }
                        else
                        {
                            // Select the last node in this branch
                            ReactToClickedNode(mSelectedNode.Parent.LastNode);
                        }
                    }
                    else
                    {
                        if (this.Nodes.Count > 0)
                        {
                            // Select the last node visible node in the tree.
                            // Don't expand branches incase the tree is virtual
                            TreeNode ndLast = this.Nodes[0].LastNode;
                            while (ndLast.IsExpanded && (ndLast.LastNode != null))
                            {
                                ndLast = ndLast.LastNode;
                            }
                            SelectSingleNode(ndLast);
                        }
                    }
                }
                else if (e.KeyCode == Keys.PageUp)
                {
                    // Select the highest node in the display
                    int nCount = this.VisibleCount;
                    TreeNode ndCurrent = mSelectedNode;
                    while ((nCount) > 0 && (ndCurrent.PrevVisibleNode != null))
                    {
                        ndCurrent = ndCurrent.PrevVisibleNode;
                        nCount--;
                    }
                    SelectSingleNode(ndCurrent);
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    // Select the lowest node in the display
                    int nCount = this.VisibleCount;
                    TreeNode ndCurrent = mSelectedNode;
                    while ((nCount) > 0 && (ndCurrent.NextVisibleNode != null))
                    {
                        ndCurrent = ndCurrent.NextVisibleNode;
                        nCount--;
                    }
                    SelectSingleNode(ndCurrent);
                }
                else
                {
                }
                base.OnKeyDown(e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
#endif

            {

            }
        }

        #endregion

        #region Methods

        #region Constructor

        public MultiSelectTreeView()
		{
            
			mSelectedNodes = new List<TreeNode>();
			mOriginalColors = new Dictionary<TreeNode, Color>();
			// Create a timer
			mSearchTimer = new System.Timers.Timer(TIMERVALUE);

			mSearchTimer.AutoReset = false;
	
			// Hook up the Elapsed event for the timer.
			mSearchTimer.Elapsed += new ElapsedEventHandler(TimerElapsed);

			mSearchString = "";
			mFirstSearchChar = '\0';
			mLastSearchChar = '\0';
			//  assume the next key they are going to hit is the same as the first
			mNewKeyMatchesFirstKey = true;


				
			base.SelectedNode = null;
        }

        #endregion

        #region Public Methods

        public void Deselect(TreeNode node)
        {
            if (mSelectedNodes.Remove(node))
            {
                node.BackColor = this.BackColor;
                node.ForeColor = mOriginalColors[node];
                mOriginalColors.Remove(node);
            }
        }

        public void AddNodeToSelected(TreeNode node)
        {
            mSelectedNode = node;

            if (!mSelectedNodes.Contains(node))
            {
                mSelectedNodes.Add(node);
                mOriginalColors.Add(node, node.ForeColor);
                node.BackColor = SystemColors.Highlight;
                node.ForeColor = SystemColors.HighlightText;
            }
        }

        public void CallAfterClickSelect(object sender, TreeViewEventArgs args)
        {
            if (AfterClickSelect != null)
            {
                AfterClickSelect(sender, args);
            }
        }

        #endregion

		// Vic says - The short of it is this code stops flickering.
		// Here's the long of it:  This class inherits from the MultiSelectTreeView
		// class.  The base class doesn't support multiple selection of objects, so we
		// have to implement our own selection logic for multiple selection by changing
		// the color of TreeNodes when they are selected.  Unfortunately, changing the color
		// of tree nodes causes the window to update, which makes the displayed text flicker.
		// Well, the following code fixes it thanks to some really smart dude who posted here:
		// http://www.codeguru.com/forum/archive/index.php/t-182326.html
		private const int WM_ERASEBKGND = 0x0014;
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_ERASEBKGND)
			{
				m.Result = IntPtr.Zero;
				return;
			}
			base.WndProc(ref m);
		}

        #region Private Methods

		private void ClearSelectedNodes()
		{
			foreach (TreeNode node in mSelectedNodes)
			{
				node.BackColor = this.BackColor;
				node.ForeColor = mOriginalColors[node];
			}

			mSelectedNodes.Clear();
			mOriginalColors.Clear();
			mSelectedNode = null;
		}

        private bool FindAndSelectNode(TreeNode node)
        {
            if(node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            while ((node.NextVisibleNode != null))
            {
                node = node.NextVisibleNode;
                if (node.Text.StartsWith(mSearchString))
                {
                    SelectSingleNode(node);
                    OnAfterSelect(new TreeViewEventArgs(mSelectedNode));
                    return true;
                }
            }
            return false;
        }
        
        protected static TreeNode GetNextTreeNodeCrawling(TreeNode treeNode)
        {
            const int maxNumberOfTries = 10;
            int numberTriedSoFar = 0;

            // This may
            // get called
            // while another
            // thread modifies
            // the tree node.  The
            // try/catch makes it so
            // we try more times and don't
            // fail if something has been modified.
            while (numberTriedSoFar < maxNumberOfTries)
            {
                try
                {
                    treeNode = treeNode.NextNodeCrawlingTree();
                    break;
                }
                catch
                {
                    System.Threading.Thread.Sleep(10);
                    numberTriedSoFar++;
                }
            }
            return treeNode;
        }

        private void HandleException(Exception ex)
        {
            // Perform some error handling here.
            // We don't want to bubble errors to the CLR. 
            MessageBox.Show(ex.Message);
        }

        private void ReactToClickedNode( TreeNode node )
		{
#if !DEBUG
            try
#endif
            {

                bool shouldRaiseEvents = true;

                if (node == null)
                {
                    if (AlwaysHaveOneNodeSelected == false)
                    {
                        for (int i = SelectedNodes.Count - 1; i > -1; i--)
                        {
                            Deselect(SelectedNodes[i]);
                        }
                        mSelectedNode = null;
                    }
                    else
                    {
                        shouldRaiseEvents = false;
                    }
                }
                else if (mSelectedNode == null || ModifierKeys == Keys.Control || MultiSelectBehavior == MultiSelectBehavior.RegularClick)
                {
                    // Ctrl+Click selects an unselected node, or unselects a selected node.
                    bool bIsSelected = mSelectedNodes.Contains(node);
                    SetNodeSelected(node, !bIsSelected);
                }
                else if (ModifierKeys == Keys.Shift)
                {
                    // Shift+Click selects nodes between the selected node and here.
                    TreeNode ndStart = mSelectedNode;
                    TreeNode ndEnd = node;

                    if (ndStart.Parent == ndEnd.Parent)
                    {
                        // Selected node and clicked node have same parent, easy case.
                        if (ndStart.Index < ndEnd.Index)
                        {
                            // If the selected node is beneath the clicked node walk down
                            // selecting each Visible node until we reach the end.
                            while (ndStart != ndEnd)
                            {
                                ndStart = ndStart.NextVisibleNode;
                                if (ndStart == null) break;
                                SetNodeSelected(ndStart, true);
                            }
                        }
                        else if (ndStart.Index == ndEnd.Index)
                        {
                            // Clicked same node, do nothing
                        }
                        else
                        {
                            // If the selected node is above the clicked node walk up
                            // selecting each Visible node until we reach the end.
                            while (ndStart != ndEnd)
                            {
                                ndStart = ndStart.PrevVisibleNode;
                                if (ndStart == null) break;
                                SetNodeSelected(ndStart, true);
                            }
                        }
                    }
                    else
                    {
                        // Selected node and clicked node have different parent, hard case.
                        // We need to find a common parent to determine if we need
                        // to walk down selecting, or walk up selecting.

                        TreeNode ndStartP = ndStart;
                        TreeNode ndEndP = ndEnd;
                        int startDepth = Math.Min(ndStartP.Level, ndEndP.Level);

                        // Bring lower node up to common depth
                        while (ndStartP.Level > startDepth)
                        {
                            ndStartP = ndStartP.Parent;
                        }

                        // Bring lower node up to common depth
                        while (ndEndP.Level > startDepth)
                        {
                            ndEndP = ndEndP.Parent;
                        }

                        // Walk up the tree until we find the common parent
                        while (ndStartP.Parent != ndEndP.Parent)
                        {
                            ndStartP = ndStartP.Parent;
                            ndEndP = ndEndP.Parent;
                        }

                        // Select the node
                        if (ndStartP.Index < ndEndP.Index)
                        {
                            // If the selected node is beneath the clicked node walk down
                            // selecting each Visible node until we reach the end.
                            while (ndStart != ndEnd)
                            {
                                ndStart = ndStart.NextVisibleNode;
                                if (ndStart == null) break;
                                SetNodeSelected(ndStart, true);
                            }
                        }
                        else if (ndStartP.Index == ndEndP.Index)
                        {
                            if (ndStart.Level < ndEnd.Level)
                            {
                                while (ndStart != ndEnd)
                                {
                                    ndStart = ndStart.NextVisibleNode;
                                    if (ndStart == null) break;
                                    SetNodeSelected(ndStart, true);
                                }
                            }
                            else
                            {
                                while (ndStart != ndEnd)
                                {
                                    ndStart = ndStart.PrevVisibleNode;
                                    if (ndStart == null) break;
                                    SetNodeSelected(ndStart, true);
                                }
                            }
                        }
                        else
                        {
                            // If the selected node is above the clicked node walk up
                            // selecting each Visible node until we reach the end.
                            while (ndStart != ndEnd)
                            {
                                ndStart = ndStart.PrevVisibleNode;
                                if (ndStart == null) break;
                                SetNodeSelected(ndStart, true);
                            }
                        }
                    }
                }
                else
                {
                    // Just clicked a node, select it
                    SelectSingleNode(node);
                }

                // August 22, 2011
                // Not sure why this
                // is using mSelectedNode,
                // but I think we should be
                // using node, because that's
                // the node that just got selected.
                //OnAfterSelect(new TreeViewEventArgs(mSelectedNode));
                if (shouldRaiseEvents)
                {
                    OnAfterSelect(new TreeViewEventArgs(node));
                }
            }
#if !DEBUG
			finally
			{

			}
#endif
		}

		private void SelectSingleNode(TreeNode node)
		{
			if (node == null)
				return;

			ClearSelectedNodes();
			SetNodeSelected(node, true);
			node.EnsureVisible();
		}

		private void SetNodeSelected(TreeNode node, bool selected)
		{
            if (node == null)
                return;

            if (selected)
                AddNodeToSelected(node);
            else
                Deselect(node);
		}


        public void UpdateForeColorFor(TreeNode treeNode, ref Color color)
        {
            if (mSelectedNodes.Contains(treeNode))
            {
                mOriginalColors[treeNode] = color;
                color = SystemColors.HighlightText;
            }
        }

		private void TimerElapsed(object source, ElapsedEventArgs e)
		{
			mSearchString = "";
			mNewKeyMatchesFirstKey = true;
		}

        #endregion

        #endregion

        #region IEnumerable<TreeNode> Members

        public IEnumerator<TreeNode> GetEnumerator()
        {
            if (Nodes.Count == 0)
            {
                yield break;
            }
            else
            {
                TreeNode currentNode = Nodes[0];

                while (currentNode != null)
                {
                    yield return currentNode;
                    currentNode = GetNextTreeNodeCrawling(currentNode);
                }
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (Nodes.Count == 0)
            {
                yield break;
            }
            else
            {
                TreeNode currentNode = Nodes[0];

                while (currentNode != null)
                {
                    yield return currentNode;
                    currentNode = GetNextTreeNodeCrawling(currentNode);
                }
            }
        }

        #endregion
    }

    #region Tree Node Extension Methods

    public static class TreeNodeExtensions
    {
        public static TreeNode NextNodeCrawlingTree(this TreeNode node)
        {
            // return child?
            if (node.Nodes.Count != 0)
            {
                return node.Nodes[0];
            }

            // return sibling?
            TreeNode nextSibling = node.NextNode;

            if (nextSibling != null)
            {
                return nextSibling;
            }

            TreeNode parentNode = node.Parent;

            while (parentNode != null)
            {
                if (parentNode.NextNode == null)
                {
                    parentNode = parentNode.Parent;
                }
                else
                {
                    return parentNode.NextNode;
                }

            }

            return null;

        }
    }

    #endregion

    public enum MultiSelectBehavior
    {
        CtrlDown,
        RegularClick
    }
}
