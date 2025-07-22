using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.IO;
using ToolsUtilities;
using Gum.Wireframe;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;
using System.ComponentModel;
using Grid = System.Windows.Controls.Grid;
using Gum.Mvvm;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.Logic;
using System.Drawing;
using Gum.Commands;
using Gum.Dialogs;
using WpfInput = System.Windows.Input;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Managers;

#region ExpandedState class
class ExpandedState
{
    public bool ScreensExpanded { get; set; }
    public bool ComponentsExpanded { get; set; }
    public bool StandardsExpanded { get; set; }
    public bool BehaviorsExpanded { get; set; }

    public Dictionary<TreeNode, bool> ExpandedStates { get; set; } = new Dictionary<TreeNode, bool>();

    public void Record(TreeNode treeNode)
    {
        ExpandedStates[treeNode] = treeNode.IsExpanded;

        foreach(TreeNode subNode in treeNode.Nodes)
        {
            // we only care about directory nodes, as only those are going to be recorded.
            if (subNode.Nodes.Count > 0 && subNode.Tag == null)
            {
                // record this bad boy:
                Record(subNode);
            }
        }
    }
    public void Apply()
    {
        foreach(var kvp in ExpandedStates)
        {
            if(kvp.Value)
            {
                kvp.Key.Expand();
            }
            else// if(nodesToKeepExpanded.Contains(kvp.Key) == false)
            {
                kvp.Key.Collapse();
            }
        }
    }

    public void ExpandAll()
    {
        foreach(var kvp in ExpandedStates)
        {
            kvp.Key.Expand();
        }
    }
}
#endregion

#region TreeNodeWrapper Class
class TreeNodeWrapper : ITreeNode
{
    public TreeNode Node { get;  }
    public object Tag => Node.Tag;
    public string Text => Node.Text;

    public ITreeNode? Parent => Node.Parent != null
        ? new TreeNodeWrapper(Node.Parent)
        : null;

    public FilePath GetFullFilePath() => Node.GetFullFilePath();

    public string FullPath => Node.FullPath;

    public TreeNodeWrapper(TreeNode node)
    {
        if(node == null)
        {
            throw new ArgumentNullException();
        }
        Node = node;
    }

    public void Expand() => Node.Expand();
}

#endregion

public partial class ElementTreeViewManager
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly EditCommands _editCommands;
    private readonly GuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly FileCommands _fileCommands;


    public const int TransparentImageIndex = 0;
    public const int FolderImageIndex = 1;
    public const int ComponentImageIndex = 2;
    public const int InstanceImageIndex = 3;
    public const int ScreenImageIndex = 4;
    public const int StandardElementImageIndex = 5;
    public const int ExclamationIndex = 6;
    public const int StateImageIndex = 7;
    public const int BehaviorImageIndex = 8;
    public const int DerivedInstanceImageIndex = 9;

    static ElementTreeViewManager mSelf;
    ContextMenuStrip mMenuStrip;
    

    MultiSelectTreeView ObjectTreeView;
    private ImageList originalImageList;
    public ImageList unmodifiableImageList
    {
        get
        {
            return CloneImageList(originalImageList);
        }
        set
        {
            if (originalImageList == null)
            {
                originalImageList = value;
            }
        }
    }

    TreeNode mScreensTreeNode;
    TreeNode mComponentsTreeNode;
    TreeNode mStandardElementsTreeNode;
    TreeNode mBehaviorsTreeNode;
    TreeNode? mLastHoveredNode;
    private DateTime? hoverStartTime;



    FlatSearchListBox FlatList;
    System.Windows.Forms.Integration.WindowsFormsHost TreeViewHost;


    /// <summary>
    /// Used to store off what was previously selected
    /// when the tree view refreshes itself - so the user
    /// doesn't lose his selection.
    /// </summary>
    object mRecordedSelectedObject;

    System.Windows.Controls.TextBox searchTextBox;
    System.Windows.Controls.CheckBox deepSearchCheckBox;
    #endregion

    #region Properties

    public static ElementTreeViewManager Self
    {
        get 
        {
            if (mSelf == null)
            {
                mSelf = new ElementTreeViewManager();
            }
            return mSelf; 
        }
    }

    public ITreeNode SelectedNode
    {
        get
        {
            // This could be called before the tree is created:
            if (ObjectTreeView?.SelectedNode == null)
            {
                return null;
            }
            else
            {
                return new TreeNodeWrapper( ObjectTreeView.SelectedNode);
            }
        }
        set
        {
            ObjectTreeView.SelectedNode = (value as TreeNodeWrapper)?.Node;
        }
    }

    public List<ITreeNode> SelectedNodes
    {
        get
        {
            return ObjectTreeView.SelectedNodes.Select(item => new TreeNodeWrapper(item)).ToList<ITreeNode>();
        }
    }

    ExpandedState expandedStateBeforeFilter;
    string filterText;
    public string FilterText
    {
        get => filterText;
        set 
        {
            if(value != filterText)
            {
                filterText = value;
                ReactToFilterTextChanged();

            }
        }
    }


    private void SelectFirstElement()
    {
        TreeNode treeNode = 
            ObjectTreeView.Nodes.FirstOrDefault() as TreeNode;

        while(treeNode != null)
        {
            if (treeNode.Tag != null)
            {
                Select(treeNode);
                break;
            }
            else
            {
                treeNode = treeNode.NextVisibleNode;
            }
        }
    }

    public TreeNode RootScreensTreeNode => mScreensTreeNode;

    public TreeNode RootComponentsTreeNode => mComponentsTreeNode;

    public TreeNode RootStandardElementsTreeNode => mStandardElementsTreeNode;

    public TreeNode RootBehaviorsTreeNode => mBehaviorsTreeNode;

    private DragDropManager _dragDropManager;
    private CopyPasteLogic _copyPasteLogic;

    public bool HasMouseOver
    {
        get
        {
            var mousePosition = Control.MousePosition;
            var clientPoint = ObjectTreeView.PointToClient(mousePosition);
            return ObjectTreeView.ClientRectangle.Contains(clientPoint);
        }
    }

    #endregion

    public ElementTreeViewManager()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _editCommands = Locator.GetRequiredService<EditCommands>();
        _guiCommands = Locator.GetRequiredService<GuiCommands>();
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _fileCommands = Locator.GetRequiredService<FileCommands>();
        
        TreeNodeExtensionMethods.ElementTreeViewManager = this;
    }

    #region Methods


    #region Find/Get
    public TreeNode GetTreeNodeFor(ElementSave elementSave)
    {
        if (elementSave == null)
        {
            return null;
        }
        else if (elementSave is ScreenSave)
        {
            return GetTreeNodeFor(elementSave as ScreenSave);
        }
        else if (elementSave is ComponentSave)
        {
            return GetTreeNodeFor(elementSave as ComponentSave);
        }
        else if (elementSave is StandardElementSave)
        {
            return GetTreeNodeFor(elementSave as StandardElementSave);
        }

        return null;
    }




    public TreeNode GetTreeNodeFor(ScreenSave screenSave)
    {
        return GetTreeNodeForTag(screenSave, RootScreensTreeNode);
    }

    public TreeNode GetTreeNodeFor(ComponentSave componentSave)
    {
        return GetTreeNodeForTag(componentSave, RootComponentsTreeNode);
    }

    public TreeNode GetTreeNodeFor(StandardElementSave standardElementSave)
    {
        return GetTreeNodeForTag(standardElementSave, RootStandardElementsTreeNode);
    }

    public TreeNode GetTreeNodeFor(InstanceSave instanceSave, TreeNode container)
    {
        foreach (TreeNode node in container.Nodes)
        {
            if (node.Tag == instanceSave)
            {
                return node;
            }

            TreeNode childNode = GetTreeNodeFor(instanceSave, node);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    public TreeNode GetInstanceTreeNodeByName(string name, TreeNode container)
    {
        foreach (TreeNode node in container.Nodes)
        {
            if (node.Tag is InstanceSave instanceSave && instanceSave.Name == name)
            {
                return node;
            }

            TreeNode childNode = GetInstanceTreeNodeByName(name, node);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    public TreeNode GetTreeNodeFor(BehaviorSave behavior)
    {
        return GetTreeNodeForTag(behavior, RootBehaviorsTreeNode);
    }

    public TreeNode GetTreeNodeFor(string absoluteDirectory)
    {
        string relative = FileManager.MakeRelative(absoluteDirectory,
            FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName));


        relative = FileManager.Standardize(relative);
        // in the tool we use forward slashes:
        relative = relative.Replace("\\", "/");

        if (relative.StartsWith("screens/"))
        {
            string modifiedRelative = relative.Substring("screens/".Length);

            return GetTreeNodeFor(modifiedRelative, mScreensTreeNode);
        }
        else if (relative.StartsWith("components/"))
        {
            string modifiedRelative = relative.Substring("components/".Length);

            return GetTreeNodeFor(modifiedRelative, mComponentsTreeNode);
        }
        else if (relative.StartsWith("standards/"))
        {
            string modifiedRelative = relative.Substring("standards/".Length);

            return GetTreeNodeFor(modifiedRelative, mStandardElementsTreeNode);
        }
        else if(relative.StartsWith("behaviors/"))
        {
            string modifiedRelative = relative.Substring("behaviors/".Length);

            return GetTreeNodeFor(modifiedRelative, mBehaviorsTreeNode);
        }

        return null;

    }

    TreeNode GetTreeNodeFor(string relativeDirectory, TreeNode container)
    {
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            return container;
        }

        int indexOfSlash = relativeDirectory.IndexOf('/');
        string whatToLookFor = relativeDirectory;
        string sub = "";

        if (indexOfSlash != -1)
        {
            whatToLookFor = relativeDirectory.Substring(0, indexOfSlash);
            sub = relativeDirectory.Substring(indexOfSlash + 1, relativeDirectory.Length - (indexOfSlash + 1));
        }

        foreach (TreeNode node in container.Nodes)
        {
            if (node.Text.Equals(whatToLookFor, StringComparison.OrdinalIgnoreCase))
            {
                return GetTreeNodeFor(sub, node);
            }
        }

        return null;
    }

    TreeNode GetTreeNodeForTag(object tag, TreeNode container = null)
    {
        if (container == null)
        {
            if (tag is ScreenSave)
            {
                container = RootScreensTreeNode;
            }
            else if (tag is ComponentSave)
            {
                container = RootComponentsTreeNode;
            }
            else if (tag is StandardElementSave)
            {
                container = RootStandardElementsTreeNode;
            }
            else if(tag is BehaviorSave)
            {
                container = RootBehaviorsTreeNode;
            }
        }

        foreach (TreeNode treeNode in container.Nodes)
        {
            if (treeNode.Tag == tag)
            {
                return treeNode;
            }

            var found = GetTreeNodeForTag(tag, treeNode);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public ITreeNode? GetTreeNodeOver()
    {
        System.Drawing.Point point = ObjectTreeView.PointToClient(Cursor.Position);

        var nodeAtPoint = ObjectTreeView.GetNodeAt(point);

        if(nodeAtPoint == null)
        {
            return null;
        }
        else
        {
            return new TreeNodeWrapper(nodeAtPoint);
        }
    }

    #endregion
    

    public void Initialize(IContainer components, 
        ImageList ElementTreeImages)
    {
        _dragDropManager = Locator.GetRequiredService<DragDropManager>();
        _copyPasteLogic = CopyPasteLogic.Self;

        CreateObjectTreeView(ElementTreeImages);

        CreateContextMenuStrip(components);

        RefreshUi();

        InitializeMenuItems();

        //var panel = new Panel();

        var grid = new Grid();
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition() 
            { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition()
                { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(
            new System.Windows.Controls.RowDefinition() 
            { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        _guiCommands.AddControl(grid, "Project", TabLocation.Left);

        ObjectTreeView.Dock = DockStyle.Fill;
        //panel.Controls.Add(ObjectTreeView);
        TreeViewHost = new System.Windows.Forms.Integration.WindowsFormsHost();
        TreeViewHost.Child = ObjectTreeView;
        Grid.SetRow(TreeViewHost, 2);
        grid.Children.Add(TreeViewHost);


        var searchBarUi = CreateSearchBoxUi();
        Grid.SetRow(searchBarUi, 0);
        grid.Children.Add(searchBarUi);

        var checkBoxUi = CreateSearchCheckBoxUi();
        Grid.SetRow(checkBoxUi, 1);
        grid.Children.Add(checkBoxUi);

        FlatList = CreateFlatSearchList();
        FlatList.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        FlatList.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

        Grid.SetRow(FlatList, 2);
        grid.Children.Add(FlatList);

        //_guiCommands.AddControl(panel, "Project", TabLocation.Left);
    }


    internal void FocusSearch()
    {
        searchTextBox.Focus();
    }

    private void CreateContextMenuStrip(IContainer components)
    {
        this.mMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
        this.mMenuStrip.Name = "ElementMenuStrip";
        this.mMenuStrip.Size = new System.Drawing.Size(61, 4);
        this.ObjectTreeView.ContextMenuStrip = this.mMenuStrip;
    }

    private void CreateObjectTreeView(ImageList ElementTreeImages)
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
        this.ObjectTreeView = new CommonFormsAndControls.MultiSelectTreeView();
        this.ObjectTreeView.IsSelectingOnPush = false;
        this.ObjectTreeView.AllowDrop = true;
        this.ObjectTreeView.AlwaysHaveOneNodeSelected = false;
        this.ObjectTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ObjectTreeView.HotTracking = true;
        this.ObjectTreeView.ImageIndex = 0;
        this.ObjectTreeView.ImageList = ElementTreeImages;
        unmodifiableImageList = ElementTreeImages;
        this.ObjectTreeView.Location = new System.Drawing.Point(0, 0);
        this.ObjectTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
        this.ObjectTreeView.Name = "ObjectTreeView";
        this.ObjectTreeView.SelectedImageIndex = 0;
        this.ObjectTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("ObjectTreeView.SelectedNodes")));
        this.ObjectTreeView.Size = new System.Drawing.Size(196, 621);
        this.ObjectTreeView.TabIndex = 0;
        this.ObjectTreeView.AfterClickSelect += this.ObjectTreeView_AfterClickSelect;
        this.ObjectTreeView.AfterSelect += this.ObjectTreeView_AfterSelect_1;
        this.ObjectTreeView.KeyDown += this.ObjectTreeView_KeyDown;
        this.ObjectTreeView.KeyPress += this.ObjectTreeView_KeyPress;
        this.ObjectTreeView.PreviewKeyDown += this.ObjectTreeView_PreviewKeyDown;
        this.ObjectTreeView.MouseClick += this.ObjectTreeView_MouseClick;
        this.ObjectTreeView.MouseMove += (sender, e) => HandleMouseOver(e.X, e.Y);
        this.ObjectTreeView.FontChanged += (sender, _) =>
        {
            if (sender is MultiSelectTreeView { Font.Size: var fontSize})
            {
                const float defaultFontSize = 8.25f;
                UpdateTreeviewIconScale(fontSize/defaultFontSize);
            }
        };
        ObjectTreeView.DragDrop += HandleDragDropEvent;

        ObjectTreeView.ItemDrag += (sender, e) =>
        {
            var treeNode = (TreeNode)e.Item;

            _dragDropManager.OnItemDrag(new TreeNodeWrapper(treeNode));
            System.Diagnostics.Debug.WriteLine("ItemDrag");

            ObjectTreeView.DoDragDrop(e.Item, DragDropEffects.Move | DragDropEffects.Copy);
        };

        ObjectTreeView.DragEnter += (sender, e) =>
        {
            e.Effect = DragDropEffects.All;

        };

        ObjectTreeView.DragOver += HandleDragOverEvent;


        ObjectTreeView.GiveFeedback += (sender, e) =>
        {
            // Use custom cursors if the check box is checked.
            // Sets the custom cursor based upon the effect.
            //InputManager.
            if(InputLibrary.Cursor.Self.IsInWindow)
            {
                e.UseDefaultCursors = false;
                System.Windows.Forms.Cursor.Current = _guiCommands.AddCursor;
            }
        };
    }

    private ImageList CloneImageList(ImageList original)
    {
        // Create a new ImageList with matching properties
        ImageList copy = new ImageList
        {
            ImageSize = original.ImageSize,
            ColorDepth = original.ColorDepth,
            TransparentColor = original.TransparentColor
        };

        // Clone each image from the original list
        for (int i = 0; i < original.Images.Count; i++)
        {
            string key = original.Images.Keys[i];
            copy.Images.Add(key, (Image)original.Images[i].Clone());
        }

        return copy;
    }

    private void UpdateTreeviewIconScale(float scale = 1.0f)
    {
        int baseImageSize = 16;

        // Then we can re-scale the images
        ObjectTreeView.ImageList = ResizeImageListImages(
            unmodifiableImageList
            , new System.Drawing.Size(
                (int)(baseImageSize * scale)
                , (int)(baseImageSize * scale)));

        ImageList ResizeImageListImages(ImageList originalImageList, Size newSize)
        {
            ImageList resizedImageList = new ImageList
            {
                ImageSize = newSize,
                ColorDepth = originalImageList.ColorDepth // Preserve original color depth
            };

            foreach (string key in originalImageList.Images.Keys)
            {
                Image originalImage = originalImageList.Images[key];
                Image resizedImage = ResizeImageWithoutGraphics(originalImage, newSize); // Reuse ResizeImage method
                resizedImageList.Images.Add(key, resizedImage);
            }

            return resizedImageList;
        }

        Image ResizeImageWithoutGraphics(Image originalImage, Size newSize)
        {
            Bitmap resizedImage = new Bitmap(newSize.Width, newSize.Height);

            for (int x = 0; x < newSize.Width; x++)
            {
                for (int y = 0; y < newSize.Height; y++)
                {
                    // Calculate the position in the original image
                    int originalX = x * originalImage.Width / newSize.Width;
                    int originalY = y * originalImage.Height / newSize.Height;

                    // Copy the pixel from the original image
                    resizedImage.SetPixel(x, y, ((Bitmap)originalImage).GetPixel(originalX, originalY));
                }
            }

            return resizedImage;
        }
    }

    private void ObjectTreeView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        int m = 3;
    }

    private void ObjectTreeView_KeyPress(object sender, KeyPressEventArgs e)
    {
        _dragDropManager.HandleKeyPress(e);
    }

    private void HandleDragOverEvent(object sender, DragEventArgs e)
    {
        var treeview = (MultiSelectTreeView)sender;
        Point pointWithinTreeview = treeview.PointToClient(new Point(e.X, e.Y));
        const int scrollBufferZone = 20;

        if (pointWithinTreeview.Y < scrollBufferZone)
        {
            if (treeview.TopNode?.PrevVisibleNode != null)
            {
                treeview.TopNode = treeview.TopNode.PrevVisibleNode;
            }
        }
        else if (pointWithinTreeview.Y > treeview.ClientSize.Height - scrollBufferZone)
        {
            if (treeview.TopNode?.NextVisibleNode != null)
            {
                treeview.TopNode = treeview.TopNode.NextVisibleNode;
            }
        }
        else
        {
            var hoveredNode = treeview.GetNodeAt(pointWithinTreeview);
            if (hoveredNode != null)
            {
                // Can't do this, it seems to interfere with the Undo History
                //treeview.SelectedNode = hoveredNode;

                // So...lets fake it with backcolor/forecolor instead?
                if (mLastHoveredNode != hoveredNode)
                {
                    hoverStartTime = DateTime.Now;

                    // restore previous colors
                    if (mLastHoveredNode != null)
                    {
                        mLastHoveredNode.BackColor = treeview.BackColor;
                        mLastHoveredNode.ForeColor = treeview.ForeColor;
                    }

                    // apply highlight colors
                    mLastHoveredNode = hoveredNode;
                    if (mLastHoveredNode != null)
                    {
                        mLastHoveredNode.BackColor = SystemColors.Highlight;
                        mLastHoveredNode.ForeColor = SystemColors.HighlightText;
                    }

                    // If partially off the screen, make it visible
                    if (!hoveredNode.IsVisible)
                        hoveredNode.EnsureVisible();
                }
                else
                {
                    // Make it so that we can EXPAND folders or nodes/items if we hover for half a second
                    if (hoveredNode.Nodes.Count > 0 && !hoveredNode.IsExpanded)
                    {
                        if (hoverStartTime == null)
                        {
                            hoverStartTime = DateTime.Now;
                        }

                        TimeSpan duration = (TimeSpan)(DateTime.Now - hoverStartTime);
                        int hoverDelayMiliseconds = 500;
                        if (duration.TotalMilliseconds > hoverDelayMiliseconds)
                        {
                            hoveredNode.Expand();
                        }
                    }
                }

                // for selected nodes, we need to make sure we reset them 
                // back to show they are highlighted once we are off them
                foreach (var node in treeview.SelectedNodes)
                {
                    if (node == hoveredNode)
                    {
                        node.BackColor = Color.DarkBlue;
                        node.ForeColor = treeview.BackColor;
                    }
                    else
                    {
                        node.BackColor = SystemColors.Highlight;
                        node.ForeColor = SystemColors.HighlightText;
                    }
                }
            }
        }

        e.Effect = DragDropEffects.Move;
    }

    private void HandleDragDropEvent(object sender, DragEventArgs e)
    {
        // Make sure that we reset the Hovered items colors at the end of the drag
        if (mLastHoveredNode != null)
        {
            mLastHoveredNode.BackColor = ObjectTreeView.BackColor;
            mLastHoveredNode.ForeColor = ObjectTreeView.ForeColor;
            mLastHoveredNode = null;
            hoverStartTime = null;
        }

        if (e.Data != null)
        {
            _dragDropManager.HandleDragDropEvent(sender, e);
        }
        _dragDropManager.ClearDraggedItem();
    }

    private void AddAndRemoveFolderNodes()
    {
        if (ObjectFinder.Self.GumProjectSave != null && 
            
            !string.IsNullOrEmpty(ObjectFinder.Self.GumProjectSave.FullFileName))
        {
            string currentDirectory = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

            // Let's make sure these folders exist, they better!
            Directory.CreateDirectory(mStandardElementsTreeNode.GetFullFilePath().FullPath);
            Directory.CreateDirectory(mScreensTreeNode.GetFullFilePath().FullPath);
            Directory.CreateDirectory(mComponentsTreeNode.GetFullFilePath().FullPath);
            Directory.CreateDirectory(mBehaviorsTreeNode.GetFullFilePath().FullPath);


            // add folders to the screens, entities, and standard elements
            AddAndRemoveFolderNodesFromFileSystem(mStandardElementsTreeNode.GetFullFilePath().FullPath, mStandardElementsTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mScreensTreeNode.GetFullFilePath().FullPath, mScreensTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mComponentsTreeNode.GetFullFilePath().FullPath, mComponentsTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mBehaviorsTreeNode.GetFullFilePath().FullPath, mBehaviorsTreeNode.Nodes);


            AddNeededButMissingFromFileSystemFolderNodes();
            //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
        }
        else
        {
            RootScreensTreeNode.Nodes.Clear();
            RootComponentsTreeNode.Nodes.Clear();
            // maybe we support behavior folders in the future? If so:
            RootBehaviorsTreeNode.Nodes.Clear();
        }
    }

    private void AddNeededButMissingFromFileSystemFolderNodes()
    {
        var project = ObjectFinder.Self.GumProjectSave;

        HashSet<string> neededFolders = new HashSet<string>();

        foreach(var element in project.AllElements)
        {
            var rootDirectoryForElementType =
                element is ScreenSave ? FileLocations.Self.ScreensFolder
                : element is ComponentSave ? FileLocations.Self.ComponentsFolder
                : element is StandardElementSave ? FileLocations.Self.StandardsFolder
                : string.Empty;

            string fullPath = rootDirectoryForElementType + FileManager.GetDirectory(element.Name);

            if(!neededFolders.Contains(fullPath))
            {
                neededFolders.Add(fullPath);
            }
        }

        foreach(var item in neededFolders)
        {
            CreateNodeIfNecessary(item);
        }
    }

    private TreeNode CreateNodeIfNecessary(string directory)
    {
        var treeNode = GetTreeNodeFor(directory);

        if(treeNode == null)
        {
            TreeNode parentNode = null;
            string parentDirectory = string.Empty;
            try
            {
                parentDirectory = FileManager.GetDirectory(directory);
            }
            catch { }

            if(parentDirectory != string.Empty)
            {
                parentNode = CreateNodeIfNecessary(parentDirectory);
            }

            if(parentNode != null)
            {
                var treeNodeText = FileManager.RemovePath(directory);
                if(treeNodeText?.EndsWith("/") == true)
                {
                    treeNodeText = treeNodeText.Substring(0, treeNodeText.Length - 1);
                }
                treeNode = parentNode.Nodes.Add(treeNodeText);
                treeNode.ImageIndex = ExclamationIndex;
            }
        }

        return treeNode;
    }

    private void AddAndRemoveFolderNodesFromFileSystem(string currentDirectory, TreeNodeCollection nodesToAddTo)
    {
        // todo: removes
        var directories = Directory.EnumerateDirectories(currentDirectory).ToArray();

        foreach (string directory in directories)
        {
            TreeNode existingTreeNode = GetTreeNodeFor(directory);

            if (existingTreeNode == null)
            {
                existingTreeNode = nodesToAddTo.Add(FileManager.RemovePath(directory));
                existingTreeNode.ImageIndex = FolderImageIndex;
            }
            AddAndRemoveFolderNodesFromFileSystem(directory, existingTreeNode.Nodes);
        }

        for(int i = nodesToAddTo.Count - 1; i > -1; i--)
        {
            TreeNode node = nodesToAddTo[i];

            bool found = false;

            foreach (string directory in directories)
            {
                string directoryStripped = FileManager.RemovePath(directory);

                if (directoryStripped.Equals(node.Text, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            // only remove nodes if they are directory nodes (aka they have a null tag)
            if (!found && node.Tag == null)
            {
                nodesToAddTo.RemoveAt(i);
            }               
        }
    }

    bool ShouldShow(ScreenSave screen) => string.IsNullOrEmpty(filterText) || screen.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(ComponentSave component) => string.IsNullOrEmpty(filterText) || component.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(StandardElementSave standardElementSave) => string.IsNullOrEmpty(filterText) || standardElementSave.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(BehaviorSave behavior) => string.IsNullOrEmpty(filterText) || behavior.Name?.ToLower().Contains(filterText.ToLower()) == true;

    private void AddAndRemoveScreensComponentsStandardsAndBehaviors(TreeNode folderTreeNode)
    {
        /////////////Early Out////////////////
        if (ProjectManager.Self.GumProjectSave == null)
            return;
        ////////////End Early Out////////////

        // Save off old selected stuff
        InstanceSave selectedInstance = _selectedState.SelectedInstance;
        ElementSave selectedElement = _selectedState.SelectedElement;
        BehaviorSave selectedBehavior = _selectedState.SelectedBehavior;


        #region Add nodes that haven't been added yet

        foreach (ScreenSave screenSave in ProjectManager.Self.GumProjectSave.Screens)
        {
            var treeNode = GetTreeNodeFor(screenSave);
            if (treeNode == null && ShouldShow(screenSave))
            {
                string fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(screenSave.Name);
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                treeNode = AddTreeNodeForElement(screenSave, parentNode, ScreenImageIndex);
            }
        }

        foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
        {
            if (GetTreeNodeFor(componentSave) == null && ShouldShow(componentSave))
            {
                string fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                if(parentNode == null)
                {
                    throw new Exception($"Error trying to get parent node for component {fullPath}");
                }

                AddTreeNodeForElement(componentSave, parentNode, ComponentImageIndex);
            }
        }

        foreach (StandardElementSave standardSave in ProjectManager.Self.GumProjectSave.StandardElements)
        {
            if (standardSave.Name != "Component")
            {
                if (GetTreeNodeFor(standardSave) == null &&  ShouldShow(standardSave))
                {
                    AddTreeNodeForElement(standardSave, mStandardElementsTreeNode, StandardElementImageIndex);
                }
            }
        }

        foreach(BehaviorSave behaviorSave in ProjectManager.Self.GumProjectSave.Behaviors)
        {
            if(GetTreeNodeFor(behaviorSave) == null && ShouldShow(behaviorSave))
            {
                string fullPath = FileLocations.Self.BehaviorsFolder;
                
                if(behaviorSave.Name != null)
                {
                    fullPath = FileLocations.Self.BehaviorsFolder + FileManager.GetDirectory(behaviorSave.Name);
                }
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                AddTreeNodeForBehavior(behaviorSave, parentNode, BehaviorImageIndex);
            }
        }

        #endregion

        #region Remove nodes that are no longer needed

        void RemoveScreenRecursively(TreeNode treeNode, int i, TreeNode container)
        {
            ScreenSave screen = treeNode.Tag as ScreenSave;

            // If the screen is null, that means that it's a folder TreeNode, so we don't want to remove it
            if (screen != null)
            {
                if (!ProjectManager.Self.GumProjectSave.Screens.Contains(screen) || !ShouldShow(screen))
                {
                    container.Nodes.RemoveAt(i);
                }
            }
            else if(treeNode.Nodes != null)
            {
                for(int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                {
                    var subnode = treeNode.Nodes[subI];
                    RemoveScreenRecursively(subnode, subI, treeNode);
                }
            }
        }

        for (int i = mScreensTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            RemoveScreenRecursively(mScreensTreeNode.Nodes[i] as TreeNode, i, mScreensTreeNode);
        }

        void RemoveComponentRecursively(TreeNode treeNode, int i, TreeNode container)
        {
            ComponentSave component = treeNode.Tag as ComponentSave;

            // If the component is null, that means that it's a folder TreeNode, so we don't want to remove it
            if (component != null)
            {
                if (!ProjectManager.Self.GumProjectSave.Components.Contains(component) || !ShouldShow(component))
                {
                    container.Nodes.RemoveAt(i);
                }
            }
            else if (treeNode.Nodes != null)
            {
                for (int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                {
                    var subnode = treeNode.Nodes[subI];
                    RemoveComponentRecursively(subnode, subI, treeNode);
                }
            }
        }

        for (int i = mComponentsTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            RemoveComponentRecursively(mComponentsTreeNode.Nodes[i], i, mComponentsTreeNode);
        }

        for (int i = mStandardElementsTreeNode.Nodes.Count - 1; i > -1; i-- )
        {
            // Do we want to support folders here?
            StandardElementSave standardElement = mStandardElementsTreeNode.Nodes[i].Tag as StandardElementSave;

            if (!ProjectManager.Self.GumProjectSave.StandardElements.Contains(standardElement) || !ShouldShow(standardElement))
            {
                mStandardElementsTreeNode.Nodes.RemoveAt(i);
            }
        }

        for(int i = mBehaviorsTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            BehaviorSave behavior = mBehaviorsTreeNode.Nodes[i].Tag as BehaviorSave;

            if(behavior != null)
            {
                if(!ProjectManager.Self.GumProjectSave.Behaviors.Contains(behavior) || !ShouldShow(behavior))
                {
                    mBehaviorsTreeNode.Nodes.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Update the nodes

        // February 2, 2025
        // Not sure why exactly
        // but if we foreach here,
        // it can result in the enumerator
        // returning a null instance. This is
        // fixed by moving to a for-loop.
        var screenList = mScreensTreeNode.Nodes;
        for (int i = 0; i < screenList.Count; i++)
        {
            object treeNode = screenList[i];
            RefreshUi(treeNode as TreeNode);
        }

        // see above on why we use a for instead foreach
        var componentList = mComponentsTreeNode.Nodes;
        for (int i = 0; i < componentList.Count; i++)
        {
            object treeNode = componentList[i];
            RefreshUi(treeNode as TreeNode);
        }

        foreach (TreeNode treeNode in mStandardElementsTreeNode.Nodes)
        {
            RefreshUi(treeNode);
        }

        foreach(TreeNode treeNode in mBehaviorsTreeNode.Nodes)
        {
            RefreshUi(treeNode);
        }

        #endregion

        #region Sort everything

        mScreensTreeNode.Nodes.SortByName(recursive:true);

        mComponentsTreeNode.Nodes.SortByName(recursive: true);

        mStandardElementsTreeNode.Nodes.SortByName(recursive: true);

        mBehaviorsTreeNode.Nodes.SortByName(recursive: true);

        #endregion

        #region Re-select whatever was selected before

        try
        {
            if (selectedInstance != null)
            {
                _selectedState.SelectedInstance = selectedInstance;
            }
            if(selectedBehavior != null)
            {
                _selectedState.SelectedBehavior = selectedBehavior;
            }
        }
        catch
        {
            // This exception can happen if a user has an item selected, then loads a new 
            // project. In that case the previous selection will no longer be valid, so this
            // fails. That's okay.
        }
        #endregion
    }

    private static TreeNode AddTreeNodeForElement(ElementSave element, TreeNode parentNode, int defaultImageIndex)
    {
        if (parentNode == null)
        {
            throw new NullReferenceException($"{nameof(parentNode)} cannot be null");
        }
        TreeNode treeNode = new TreeNode();

        if (element.IsSourceFileMissing)
            treeNode.ImageIndex = ExclamationIndex;
        else
            treeNode.ImageIndex = defaultImageIndex;

        treeNode.Tag = element;
        
        parentNode.Nodes.Add(treeNode);

        return treeNode;
    }

    private static void AddTreeNodeForBehavior(BehaviorSave behavior, TreeNode parentNode, int defaultImageIndex)
    {
        TreeNode treeNode = new TreeNode();

        if (behavior.IsSourceFileMissing)
            treeNode.ImageIndex = ExclamationIndex;
        else
            treeNode.ImageIndex = defaultImageIndex;

        treeNode.Tag = behavior;

        parentNode.Nodes.Add(treeNode);
    }

    private void CreateRootTreeNodesIfNecessary()
    {
        if (mScreensTreeNode == null)
        {
            mScreensTreeNode = new TreeNode("Screens");
            mScreensTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mScreensTreeNode);

            mComponentsTreeNode = new TreeNode("Components");
            mComponentsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mComponentsTreeNode);

            mStandardElementsTreeNode = new TreeNode("Standard");
            mStandardElementsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mStandardElementsTreeNode);

            mBehaviorsTreeNode = new TreeNode("Behaviors");
            mBehaviorsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mBehaviorsTreeNode);
        }
    }


    public void RecordSelection()
    {
        mRecordedSelectedObject = _selectedState.SelectedInstance;

        if (mRecordedSelectedObject == null)
        {
            mRecordedSelectedObject = _selectedState.SelectedElement;
        }

        if(mRecordedSelectedObject == null)
        {
            mRecordedSelectedObject = _selectedState.SelectedBehavior;
        }
    }

    public void SelectRecordedSelection()
    {
        try
        {
            if (mRecordedSelectedObject != null)
            {
                if (mRecordedSelectedObject is InstanceSave)
                {
                    _selectedState.SelectedInstance = mRecordedSelectedObject as InstanceSave;
                }
                else if (mRecordedSelectedObject is ElementSave)
                {
                    _selectedState.SelectedElement = mRecordedSelectedObject as ElementSave;
                }
                else if(mRecordedSelectedObject is BehaviorSave)
                {
                    _selectedState.SelectedBehavior = mRecordedSelectedObject as BehaviorSave;
                }
            }
        }
        catch
        {
            // no big deal, this could have been re-loaded
        }
    }

    // Discussion about Selection
    // Selection is a rather complicated
    // system in Gum because tree nodes can
    // be selected in a number of ways:
    // 1.  The user can push/release (click)
    // 2.  The user can select an item in the
    //     wireframe window which in turn selects
    //     the appropriate tree node.
    // 3.  The user pushes on a tree node, but then
    //     drags off of it to do a drag+drop somewhere
    //     else.
    // We want the app to refresh what it is displaying
    // in scenario 1 and 2, but not in 3.  Therefore the
    // MultiSelectTreeView class has an event called AfterClickSelect
    // which only fires when the user actually clicks on an item (1) so
    // that #3 doesn't fire off an event.  However, this means that #2 will
    // no longer fire off the event either.  We need to then make sure that #2
    // does still fire off an event, so we'll do this by manually raising the event
    // in the Select methods where a Save object is selected.
    public void Select(InstanceSave instanceSave, ElementSave parent)
    {
        if (IsInUiInitiatedSelection) return;
        if (instanceSave != null)
        {
            TreeNode parentTreeNode = GetTreeNodeFor(parent);

            // This could be null if the user started a new project or loaded a different project.
            if (parentTreeNode != null)
            {
                Select(GetTreeNodeFor(instanceSave, parentTreeNode));
            }
        }
        else
        {
            Select((TreeNode)null);
        }
    }

    public void Select(BehaviorSave behavior)
    {
        if (IsInUiInitiatedSelection) return;

        if (behavior != null)
        {
            var treeNode = GetTreeNodeFor(behavior);

            Select(treeNode);
        }
    }

    public void Select(IEnumerable<InstanceSave> list)
    {
        if (IsInUiInitiatedSelection) return;

        if (list.Count() != 0)
        {
            var firstItem = list.First();

            TreeNode parentContainer = null;
            if(firstItem.ParentContainer != null)
            {
                parentContainer = GetTreeNodeFor(firstItem.ParentContainer);
            }
            else
            {
                var behavior = ObjectFinder.Self.GetBehaviorContainerOf(firstItem);
                if(behavior != null)
                {
                    parentContainer = GetTreeNodeFor(behavior);
                }
            }

            List<TreeNode> treeNodeList = new List<TreeNode>();

            foreach (var item in list)
            {
                if(parentContainer != null)
                {
                    TreeNode itemTreeNode = GetTreeNodeFor(item, parentContainer);
                    treeNodeList.Add(itemTreeNode);
                }
            }

            Select(treeNodeList);
        }
        else
        {
            Select((TreeNode)null);
        }
    }


    public void Select(ElementSave elementSave)
    {
        if (IsInUiInitiatedSelection) return;

        if (elementSave == null)
        {
            if (ObjectTreeView.SelectedNode != null && ObjectTreeView.SelectedNode.Tag != null && ObjectTreeView.SelectedNode.Tag is ElementSave)
            {
                // why do we explicitly set this here rather than calling Select? If we set it to null without calling that, we don't get the benefit of the 
                // plugins being notified of a null selection:
                //ObjectTreeView.SelectedNode = null;
                Select((TreeNode)null);

            }
        }
        else
        {
            var treeNode = GetTreeNodeFor(elementSave);

            if(treeNode == null && !string.IsNullOrEmpty(searchTextBox.Text))
            {
                searchTextBox.Text = null;
                treeNode = GetTreeNodeFor(elementSave);
            }

            Select(treeNode);
        }
    }

    private void Select(TreeNode treeNode)
    {
        if (IsInUiInitiatedSelection) return;

        if (ObjectTreeView.SelectedNode != treeNode)
        {
            // See comment above about why we have to manually raise the AfterClick

            ObjectTreeView.SelectedNode = treeNode;

            if (treeNode != null)
            {
                treeNode.EnsureVisible();
            }

            ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNode));
        }
    }

    private void Select(List<TreeNode> treeNodes)
    {
        if (IsInUiInitiatedSelection) return;

        ObjectTreeView.SelectedNodes = treeNodes;

        if (treeNodes.Count != 0)
        {
            treeNodes[0]?.EnsureVisible();
            ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNodes[0]));
        }
    }

    /// <summary>
    /// Refreshes the entirety of the tree view, preserving selection.
    /// </summary>
    public void RefreshUi()
    {
        RecordSelection();
        // brackets are used simply to indicate the recording and selection should
        // go around the rest of the function:
        {
            ObjectTreeView.SuspendLayout();
            CreateRootTreeNodesIfNecessary();

            AddAndRemoveFolderNodes();

            AddAndRemoveScreensComponentsStandardsAndBehaviors(null);
            ObjectTreeView.ResumeLayout(performLayout:true);

        }
        SelectRecordedSelection();
    }


    public void RefreshUi(IInstanceContainer instanceContainer)
    {
        var foundNode = GetTreeNodeForTag(instanceContainer);

        if(foundNode != null)
        {
            RecordSelection();
            RefreshUi(foundNode);
            SelectRecordedSelection();
        }
    }

    /// <summary>
    /// Refreshes the tree nodes for the argument stateContainer. This includes the displayed text and contained nodes, and the parent
    /// folder node.
    /// </summary>
    /// <param name="stateContainer">The StateContainer to refresh.</param>
    public void RefreshUi(IStateContainer stateContainer)
    {
        var foundNode = GetTreeNodeForTag(stateContainer);

        if(foundNode != null)
        {
            RecordSelection();
            RefreshUi(foundNode);
            SelectRecordedSelection();
        }
    }

    public void RefreshUi(TreeNode node)
    {
        if(node  == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Tag is ElementSave)
        {
            ElementSave elementSave = node.Tag as ElementSave;

            RefreshElementTreeNode(node, elementSave);
        }
        else if (node.Tag is InstanceSave)
        {
            InstanceSave instanceSave = node.Tag as InstanceSave;
            // this if check improves speed quite a bit!
            if(instanceSave.Name != node.Text)
            {
                node.Text = instanceSave.Name;
            }
        }
        else if(node.Tag is BehaviorSave behaviorSave)
        {
            var behavior = node.Tag as BehaviorSave;
            if(behavior.Name != node.Text)
            {
                node.Text = behavior.Name;
            }
            RefreshBehaviorTreeNode(node, behaviorSave);
        }

        foreach (TreeNode treeNode in node.Nodes)
        {
            if(treeNode != null)
            {
                RefreshUi(treeNode);
            }

        }
    }

    private void RefreshElementTreeNode(TreeNode node, ElementSave elementSave)
    {
        // This could be because of a corruption:
        if (string.IsNullOrEmpty(elementSave.Name))
        {
            throw new ArgumentException("ElementSave cannot have a null name");
        }
        List<InstanceSave> expandedInstances = new List<InstanceSave>();
        List<InstanceSave> allInstances = elementSave.Instances;

        if(elementSave is ScreenSave || elementSave is ComponentSave)
        {

            string fullPath = null;
            if(elementSave is ScreenSave)
            {
                fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(elementSave.Name);
            }
            else
            {
                fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(elementSave.Name);
            }
            TreeNode desiredNode = GetTreeNodeFor(fullPath);
            var parentNode = node.Parent;
            if(parentNode != desiredNode)
            {
                if (parentNode != null)
                {
                    parentNode.Nodes.Remove(node);
                }
                if(desiredNode != null)
                {
                    desiredNode.Nodes.Add(node);
                }
            }
        }

        foreach (InstanceSave instance in allInstances)
        {
            // use name because an undo can change references. Same with reloads if were called there
            var treeNode = GetInstanceTreeNodeByName(instance.Name, node);

            if (treeNode?.Nodes.Count > 0 && treeNode?.IsExpanded == true)
            {
                expandedInstances.Add(instance);
            }
        }

        var nodeText = FileManager.RemovePath(elementSave.Name);
        if(nodeText != node.Text)
        {
            var hadTextBefore = !string.IsNullOrEmpty(node.Text);
            node.Text = nodeText;

            if(hadTextBefore && node.Parent != null)
            {
                node.Parent.Nodes.SortByName();
            }
        }

        var allTreeNodesRecursively = node.GetAllChildrenNodesRecursively();
        
        // why do we clear? wouldn't this require re-creation of all nodes? that seems like it might be slow...
        //node.Nodes.Clear();
        // Let's be smart about removal...
        foreach(TreeNode instanceNode in allTreeNodesRecursively)
        {
            var instance = instanceNode.Tag as InstanceSave;

            if(!allInstances.Contains(instance))
            {
                instanceNode.Remove();
            }
        }

        List<List<InstanceSave>> siblingLists = new ();

        foreach (InstanceSave instance in allInstances)
        {
            TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes:false);
            }

            if(instance.DefinedByBase)
            {
                nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
            }

            // todo - do this after we have all the children created:
            if (expandedInstances.Any(item => item.Name == instance.Name))
            {
                nodeForInstance.Expand();
            }

            var siblingInstances = siblingLists.FirstOrDefault(item => item.Contains(instance));
            if (siblingInstances == null)
            {
                siblingInstances = instance.GetSiblingsIncludingThis();
                siblingLists.Add(siblingInstances);
            }
            var desiredIndex = siblingInstances.IndexOf(instance);

            var container = instance.ParentContainer ?? ObjectFinder.Self.GetElementContainerOf(instance);
            var defaultState = container.DefaultState;
            //var thisParentValue = defaultState.GetValueOrDefault<string>($"{instance.Name}.Parent");
            var thisParentValue = defaultState.GetValueRecursive($"{instance.Name}.Parent") as string;

            // If thisParentValue has a period, the instance is attached to an item inside the parent.
            if(thisParentValue?.Contains(".") == true)
            {
                thisParentValue = thisParentValue.Substring(0, thisParentValue.IndexOf('.'));
            }

            var desiredParentNode = node;
            if(!string.IsNullOrEmpty(thisParentValue))
            {
                var instanceParent = allInstances.FirstOrDefault(item => item.Name == thisParentValue);

                if(instanceParent != null)
                {
                    desiredParentNode = GetTreeNodeFor(instanceParent, node);
                }
            }
            if(desiredParentNode != nodeForInstance.Parent && desiredParentNode != null)
            {
                nodeForInstance.Remove();
                desiredParentNode.Nodes.Add(nodeForInstance);
            }

            var nodeParent = nodeForInstance.Parent;
            if (desiredIndex != nodeParent.Nodes.IndexOf(nodeForInstance))
            {
                nodeParent.Nodes.Remove(nodeForInstance);
                nodeParent.Nodes.Insert(desiredIndex, nodeForInstance);
            }

            var element = ObjectFinder.Self.GetElementSave(instance.BaseType);

            int desiredImageIndex = InstanceImageIndex;
            if (element == null || element.IsSourceFileMissing)
                desiredImageIndex = ExclamationIndex;

            if(nodeForInstance.ImageIndex != desiredImageIndex)
            {
                nodeForInstance.ImageIndex = desiredImageIndex;
            }
        }

        foreach(var expandedInstance in expandedInstances)
        {
            var toExpand = GetInstanceTreeNodeByName(expandedInstance.Name, node);
            toExpand?.Expand();
        }
    }

    private void RefreshBehaviorTreeNode(TreeNode node, BehaviorSave behavior)
    {
        var allInstances = behavior.RequiredInstances;
        var allTreeNodesRecursively = node.GetAllChildrenNodesRecursively();
        foreach (TreeNode instanceNode in allTreeNodesRecursively)
        {
            var instance = instanceNode.Tag as InstanceSave;

            if (!allInstances.Contains(instance))
            {
                instanceNode.Remove();
            }
        }


        foreach (InstanceSave instance in allInstances)
        {
            TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes:true);
            }
            if (instance.DefinedByBase)
            {
                nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
            }
            // screens have to worry about siblings and lists. We don't care about that here because behaviors do not
            // (currently) require instances to have a particular relationship with one another
        }
    }

    private TreeNode AddTreeNodeForInstance(InstanceSave instance, TreeNode parentContainerNode, bool tolerateMissingTypes, HashSet<InstanceSave> pendingAdditions = null)
    {
        TreeNode treeNode = new TreeNode();

        bool validBaseType = ObjectFinder.Self.GetElementSave(instance.BaseType) != null;

        if (validBaseType || tolerateMissingTypes)
            treeNode.ImageIndex = InstanceImageIndex;
        else
            treeNode.ImageIndex = ExclamationIndex;

        treeNode.Tag = instance;

        TreeNode parentNode = parentContainerNode;
        InstanceSave parentInstance = FindParentInstance(instance);

        if (parentInstance != null)
        {
            TreeNode parentInstanceNode = GetTreeNodeFor(parentInstance, parentContainerNode);

            // Make sure we are not already trying to add the parent (protects against stack overflow with invalid data)
            if (parentInstanceNode == null && (pendingAdditions == null || !pendingAdditions.Contains(parentInstance)))
            {
                if (pendingAdditions == null)
                {
                    pendingAdditions = new HashSet<InstanceSave>();
                }

                pendingAdditions.Add(parentInstance);
                parentInstanceNode = AddTreeNodeForInstance(parentInstance, parentContainerNode, tolerateMissingTypes, pendingAdditions);
            }

            if (parentInstanceNode != null)
            {
                parentNode = parentInstanceNode;
            }
        }

        parentNode.Nodes.Add(treeNode);

        return treeNode;
    }

    private InstanceSave FindParentInstance(InstanceSave instance)
    {
        if(instance is BehaviorInstanceSave)
        {
            // instances in behaviors cannot (currently) have parents
            return null;
        }
        else
        {
            ElementSave element = instance.ParentContainer ?? ObjectFinder.Self.GetElementContainerOf(instance);

            string name = instance.Name + ".Parent";
            VariableSave variable = element.DefaultState.Variables.FirstOrDefault(v => v.Name == name);

            if (variable != null && variable.SetsValue && variable.Value != null)
            {
                string parentName = (string) variable.Value;

                // This could be attached to a child inside the parent. Therefore, if ParentInstance contains a dot, return 
                // the instance with the name before the dot
                if (parentName.Contains('.'))
                {
                    parentName = parentName.Substring(0, parentName.IndexOf('.'));
                }

                return element.GetInstance(parentName);
            }
        }

        return null;
    }

    bool IsInUiInitiatedSelection = false;
    internal void OnSelect(TreeNode selectedTreeNode)
    {
        TreeNode treeNode = ObjectTreeView.SelectedNode;

        object selectedObject = null;

        if (treeNode != null)
        {
            selectedObject = treeNode.Tag;
        }


        try
        {
            IsInUiInitiatedSelection = true;
            if (selectedObject == null)
            {
                _selectedState.SelectedElement = null;
                _selectedState.SelectedBehavior = null;
                _selectedState.SelectedInstance = null;

                // do nothing
            }
            else if(selectedObject is ElementSave elementSave)
            {
                _selectedState.SelectedInstance = null;
                var elements = this.SelectedNodes
                    .Where(item => item.Tag is ElementSave)
                    .Select(item => item.Tag as ElementSave);

                _selectedState.SelectedElements = elements;
            }
            else if (selectedObject is InstanceSave selectedInstance)
            {
                var instances = this.SelectedNodes.Select(item => item.Tag)
                    .Where(item => item is InstanceSave)
                    .Select(item => item as InstanceSave);

                //_selectedState.SelectedInstance = selectedInstance;
                _selectedState.SelectedInstances = instances;
            }
            else if(selectedObject is BehaviorSave behavior)
            {
                _selectedState.SelectedBehavior = behavior;
            }

            PluginManager.Self.TreeNodeSelected(selectedTreeNode);

        }
        finally
        {
            IsInUiInitiatedSelection = false;
        }
    }

    public void VerifyComponentsAreInTreeView(GumProjectSave gumProject)
    {
        foreach (ComponentSave component in gumProject.Components)
        {
            if (GetTreeNodeFor(component) == null)
            {
                throw new Exception();
            }
        }
    }

    internal void HandleKeyDown(KeyEventArgs e)
    {
        var didTreeViewHaveFocus = ObjectTreeView.ContainsFocus;

        if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
        {
            OnSelect((SelectedNode as TreeNodeWrapper)?.Node);
        }

        HotkeyManager.Self.HandleKeyDownElementTreeView(e);

        if (didTreeViewHaveFocus)
        {
            // On a delete, the popup appears, which steals focus from the treeview.
            // If we had focus before, let's get it now.
            ObjectTreeView.Focus();
        }

    }

    private void ObjectTreeView_AfterSelect_1(object sender, TreeViewEventArgs e)
    {
        // If we use AfterClickSelect instead of AfterSelect then
        // we don't get notified when the user selects nothing.
        // Update - we only want to do this if it's null:
        // Otherwise we can't drag drop
        if (ObjectTreeView.SelectedNode == null)
        {
            OnSelect(ObjectTreeView.SelectedNode);
        }
    }

    private void ObjectTreeView_AfterClickSelect(object sender, TreeViewEventArgs e)
    {
        OnSelect(ObjectTreeView.SelectedNode);
    }

    private void ObjectTreeView_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            OnSelect(ObjectTreeView.SelectedNode);

            PopulateMenuStrip();
        }
    }

    private void ObjectTreeView_KeyDown(object sender, KeyEventArgs e)
    {
        HandleKeyDown(e);
        _dragDropManager.HandleKeyDown(e);
    }


    #endregion

    #region Searching

    private FlatSearchListBox CreateFlatSearchList()
    {
        var list = new FlatSearchListBox();
        list.SelectSearchNode += HandleSelectedSearchNode;
        return list;
    }


    private void ReactToFilterTextChanged()
    {
        var shouldExpand = false;

        if (!string.IsNullOrEmpty(filterText))
        {
            shouldExpand = true;
        }

        FlatList.Visibility = shouldExpand.ToVisibility();
        TreeViewHost.Visibility = (!shouldExpand).ToVisibility();

        //RefreshUi();

        if (!string.IsNullOrEmpty(filterText) && SelectedNode?.Tag == null)
        {
            //SelectFirstElement();
        }


        if (shouldExpand)
        {
            var filterTextLower = filterText?.ToLower();
            FlatList.FlatList.Items.Clear();

            var project = GumState.Self.ProjectState.GumProjectSave;
            foreach (var screen in project.Screens)
            {
                if (screen.Name.ToLower().Contains(filterTextLower))
                {
                    AddToFlatList(screen);
                }

                if (deepSearchCheckBox.IsChecked is true)
                {
                    SearchInstanceVariables(screen, filterTextLower);
                }
            }
            foreach (var component in project.Components)
            {
                if (component.Name.ToLower().Contains(filterTextLower))
                {
                    AddToFlatList(component);
                }

                foreach (var instance in component.Instances)
                {
                    if (instance.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(instance, $"{component.Name}/{instance.Name} ({instance.BaseType})");
                    }
                }

                if (deepSearchCheckBox.IsChecked is true)
                {
                    SearchInstanceVariables(component, filterTextLower);
                }
            }
            foreach (var standard in project.StandardElements)
            {
                if (standard.Name.ToLower().Contains(filterTextLower))
                {
                    AddToFlatList(standard);
                }

                if (deepSearchCheckBox.IsChecked is true)
                {
                    SearchInstanceVariables(standard, filterTextLower);
                }
            }

            foreach(var behavior in project.Behaviors)
            {
                // Feb 5, 2025 - at some point a behavior with an empty name
                // snuck into a FRB project. We shouldn't crash here because of it...
                if(behavior.Name?.ToLower().Contains(filterTextLower) == true)
                {
                    AddToFlatList(behavior);
                }
            }

            if(FlatList.FlatList.Items.Count > 0)
            {
                FlatList.FlatList.SelectedIndex = 0;
            }
        }
    }

    private void SearchInstanceVariables(ElementSave element, string filterTextLower )
    {
        foreach (var state in element.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable == null)
                {
                    continue;
                }

                if (variable.Value != null && (variable.Value is string str) && str.ToLower().Contains(filterTextLower))
                {
                    var instance = element.Instances.FirstOrDefault(item => item.Name == variable.SourceObject);
                    if(instance != null)
                    {
                        AddToFlatList(instance, $"{variable.Name}={variable.Value} on {element.Name}/{variable.SourceObject}");
                    }
                    else
                    {
                        AddToFlatList(element, $"{variable.Name}={variable.Value} on {element.Name}");
                    }
                }
            }
        }
    }

    private void AddToFlatList(object element, string customName = "")
    {
        if (element == null)
        {
            throw new ArgumentNullException($"{nameof(element)}");
        }
        var vm = new SearchItemViewModel();
        vm.BackingObject = element;
        vm.CustomText = customName;
        FlatList.FlatList.Items.Add(vm);
    }

    private Grid CreateSearchBoxUi()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition
            { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition
        { Width = System.Windows.GridLength.Auto});


        searchTextBox = new System.Windows.Controls.TextBox();
        searchTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        searchTextBox.TextChanged += (not, used) => FilterText = searchTextBox.Text;
        searchTextBox.KeyDown += (sender, args) =>
        {
            bool isCtrlDown = WpfInput.Keyboard.IsKeyDown(WpfInput.Key.LeftCtrl) || WpfInput.Keyboard.IsKeyDown(WpfInput.Key.RightCtrl);

            if (args.Key == WpfInput.Key.Escape)
            {
                searchTextBox.Text = null;
                args.Handled = true;
                ObjectTreeView.Focus();
            }
            else if (args.Key == WpfInput.Key.Back
             && isCtrlDown)
            {
                searchTextBox.Text = null;
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Down)
            {
                if(FlatList.FlatList.SelectedIndex < FlatList.FlatList.Items.Count -1)
                {
                    FlatList.FlatList.SelectedIndex++;
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Up)
            {
                if (FlatList.FlatList.SelectedIndex > 0)
                {
                    FlatList.FlatList.SelectedIndex--;
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Enter)
            {
                args.Handled = true;
                ObjectTreeView.Focus();

                var selectedItem = FlatList.FlatList.SelectedItem as SearchItemViewModel;
                if(selectedItem != null)
                {
                    HandleSelectedSearchNode(selectedItem);

                    searchTextBox.Text = null;
                }
            }

            HotkeyManager.Self.HandleKeyDownAppWide(args);
        };

        grid.Children.Add(searchTextBox);

        var xButton = new System.Windows.Controls.Button();
        xButton.Content = " X "; // Spaces to force some padding automatically based on font size of space
        xButton.Click += (not, used) => searchTextBox.Text = null;
        xButton.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        grid.Children.Add(xButton);

        Grid.SetColumn(searchTextBox, 0);
        Grid.SetColumn(xButton, 1);

        return grid;
    }

    private System.Windows.Controls.CheckBox CreateSearchCheckBoxUi()
    {
        deepSearchCheckBox = new System.Windows.Controls.CheckBox();
        deepSearchCheckBox.IsChecked = false;
        deepSearchCheckBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        deepSearchCheckBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        deepSearchCheckBox.Content = "Search variables";
        deepSearchCheckBox.Checked += (_, _) => ReactToFilterTextChanged();

        return deepSearchCheckBox;
    }

    private void HandleSelectedSearchNode(SearchItemViewModel vm)
    {
        var backingObject = vm?.BackingObject;
        if(backingObject != null)
        {
            if (backingObject is ScreenSave asScreen)
                _selectedState.SelectedElement = asScreen;
            else if (backingObject is ComponentSave asComponent)
                _selectedState.SelectedElement = asComponent;
            else if (backingObject is StandardElementSave asStandard)
                _selectedState.SelectedElement = asStandard;
            else if (backingObject is InstanceSave asInstance)
                _selectedState.SelectedInstance = asInstance;
            else if (backingObject is VariableSave asVariable)
                _selectedState.SelectedBehaviorVariable = asVariable;
            else if(backingObject is BehaviorSave asBehavior)
                _selectedState.SelectedBehavior = asBehavior;

            searchTextBox.Text = null;
            FilterText = null;
        }
    }


    #endregion


    internal void HandleMouseOver(int x, int y)
    {
        var objectOver = this.ObjectTreeView.GetNodeAt(x, y);

        ElementSave element = null;
        InstanceSave instance = null;

        if(objectOver != null && objectOver.Tag != null)
        {
            if(objectOver.Tag is ElementSave)
            {
                element = objectOver.Tag as ElementSave;
            }
            else if(objectOver.Tag is InstanceSave)
            {
                instance = objectOver.Tag as InstanceSave;
            }
        }

        GraphicalUiElement whatToHighlight = null;

        if(element != null)
        {
            whatToHighlight = WireframeObjectManager.Self.GetRepresentation(element);
        }
        else if(instance != null)
        {
            whatToHighlight = WireframeObjectManager.Self.GetRepresentation(instance, null);
        }

        PluginManager.Self.SetHighlightedIpso(whatToHighlight);
    }
}


#region TreeNodeExtensionMethods

public static class TreeNodeExtensionMethods
{
    public static ElementTreeViewManager ElementTreeViewManager { get; set; }
    public static bool IsScreenTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is ScreenSave;
    }

    public static bool IsComponentTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is ComponentSave;
    }

    public static bool IsBehaviorTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is BehaviorSave;
    }

    public static bool IsStandardElementTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is StandardElementSave;
    }

    public static bool IsInstanceTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is InstanceSave;
    }

    public static bool IsStateSaveTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is StateSave;
    }

    public static bool IsTopElementContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null;
    }

    public static bool IsTopScreenContainerTreeNode(this ITreeNode treeNode) =>
        treeNode is TreeNodeWrapper wrapper
        ? wrapper.Node.IsTopScreenContainerTreeNode()
        : false;

    public static bool IsTopScreenContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Screens";
    }

    public static bool IsTopBehaviorTreeNode(this ITreeNode treeNode) =>
        treeNode is TreeNodeWrapper wrapper
        ? wrapper.Node.IsTopBehaviorTreeNode()
        : false;

    public static bool IsTopBehaviorTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Behaviors";
    }

    public static bool IsTopComponentContainerTreeNode(this ITreeNode treeNode) =>
        treeNode is TreeNodeWrapper wrapper
        ? wrapper.Node.IsTopComponentContainerTreeNode()
        : false;

    public static bool IsTopComponentContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Components";
    }

    public static bool IsTopStandardElementTreeNode(this ITreeNode treeNode) => treeNode is TreeNodeWrapper wrapper 
        ? wrapper.Node.IsTopStandardElementTreeNode()
        : false;

    public static bool IsTopStandardElementTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Standard";
    }

    public static FilePath GetFullFilePath(this TreeNode treeNode)
    {
        if (treeNode.IsTopComponentContainerTreeNode() ||
            treeNode.IsTopStandardElementTreeNode() ||
            treeNode.IsTopScreenContainerTreeNode() ||
            treeNode.IsTopBehaviorTreeNode()
            )
        {
            if (ProjectManager.Self.GumProjectSave == null ||
                string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("Project isn't saved yet so the root of the project isn't known");
                return null;
            }
            else
            {
                string projectDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                if (treeNode.IsTopComponentContainerTreeNode())
                {
                    return projectDirectory + ElementReference.ComponentSubfolder + "\\";
                }
                else if (treeNode.IsTopStandardElementTreeNode())
                {
                    return projectDirectory + ElementReference.StandardSubfolder + "\\";
                }
                else if (treeNode.IsTopScreenContainerTreeNode())
                {
                    return projectDirectory + ElementReference.ScreenSubfolder + "\\";
                }
                else if(treeNode.IsTopBehaviorTreeNode())
                {
                    return projectDirectory + BehaviorReference.Subfolder + "\\";
                }
                throw new InvalidOperationException();
            }
        }
        else if (treeNode.IsStandardElementTreeNode() ||
            treeNode.IsComponentTreeNode() ||
            treeNode.IsScreenTreeNode())
        {
            ElementSave element = treeNode.Tag as ElementSave;
            return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + element.FileExtension;
        }
        else if(treeNode.IsBehaviorTreeNode())
        {
            var behavior = treeNode.Tag as BehaviorSave;
            return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + BehaviorReference.Extension;
        }
        else
        {
            var toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "\\";
            return toReturn;
        }
    }


    public static bool IsScreensFolderTreeNode(this ITreeNode treeNode) =>
        treeNode is TreeNodeWrapper wrapper
        ? wrapper.Node.IsScreensFolderTreeNode()
        : false;

    public static bool IsScreensFolderTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null &&
            treeNode.Parent != null &&
            (treeNode.Parent.IsScreensFolderTreeNode() || treeNode.Parent.IsTopScreenContainerTreeNode());
    }

    public static bool IsPartOfScreensFolderStructure(this ITreeNode treeNode) =>
        (treeNode as TreeNodeWrapper)?.Node.IsPartOfScreensFolderStructure() ?? false;

    public static bool IsPartOfScreensFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootScreensTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfScreensFolderStructure();
    }

    public static bool IsPartOfComponentsFolderStructure(this ITreeNode treeNode) =>
        (treeNode as TreeNodeWrapper)?.Node.IsPartOfComponentsFolderStructure() ?? false;

    public static bool IsPartOfComponentsFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootComponentsTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfComponentsFolderStructure();
    }

    public static bool IsPartOfStandardElementsFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootStandardElementsTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfStandardElementsFolderStructure();
    }

    public static bool IsComponentsFolderTreeNode(this ITreeNode treeNode) =>
        treeNode is TreeNodeWrapper wrapper
        ? wrapper.Node.IsComponentsFolderTreeNode()
        : false;

    public static bool IsComponentsFolderTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null &&
            treeNode.Parent != null &&
            (treeNode.Parent.IsComponentsFolderTreeNode() || treeNode.Parent.IsTopComponentContainerTreeNode());
    }

    public static void SortByName(this TreeNodeCollection treeNodeCollection, bool recursive = false)
    {
        int lastObjectExclusive = treeNodeCollection.Count;
        int whereObjectBelongs;
        for (int i = 0 + 1; i < lastObjectExclusive; i++)
        {
            TreeNode first = treeNodeCollection[i];
            TreeNode second = treeNodeCollection[i - 1];
            if (FirstComesBeforeSecond(first, second))
            {
                if (i == 1)
                {
                    TreeNode treeNode = treeNodeCollection[i];
                    treeNodeCollection.RemoveAt(i);

                    treeNodeCollection.Insert(0, treeNode);
                    continue;
                }

                for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                {
                    second = treeNodeCollection[whereObjectBelongs];
                    if (!FirstComesBeforeSecond(treeNodeCollection[i], second))
                    {
                        TreeNode treeNode = treeNodeCollection[i];

                        treeNodeCollection.RemoveAt(i);
                        treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                        break;
                    }
                    else if (whereObjectBelongs == 0 && FirstComesBeforeSecond(treeNodeCollection[i], treeNodeCollection[0]))
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);
                        treeNodeCollection.Insert(0, treeNode);
                        break;
                    }
                }
            }
        }

        if(recursive)
        {
            foreach(var node in treeNodeCollection)
            {
                var asTreeNode = node as TreeNode;
                if(asTreeNode != null)
                {
                    var sortInner = asTreeNode.IsScreenTreeNode() == false &&
                        asTreeNode.IsComponentTreeNode() == false &&
                        asTreeNode.IsStandardElementTreeNode() == false &&
                        asTreeNode.IsBehaviorTreeNode() == false;

                    if(sortInner)
                    {
                        asTreeNode.Nodes.SortByName(recursive);
                    }
                }
            }
        }
    }

    private static bool FirstComesBeforeSecond(TreeNode first, TreeNode second)
    {
        bool isFirstDirectory = first.IsComponentsFolderTreeNode() || first.IsScreensFolderTreeNode();
        bool isSecondDirectory = second.IsComponentsFolderTreeNode() || second.IsScreensFolderTreeNode();

        if (isFirstDirectory && !isSecondDirectory)
        {
            return true;
        }
        else if (!isFirstDirectory && isSecondDirectory)
        {
            return false;
        }
        else
        {
            return first.Text.CompareTo(second.Text) < 0;
        }
    }

    public static List<TreeNode> GetAllChildrenNodesRecursively(this TreeNode treeNode)
    {
        List<TreeNode> toReturn = new List<TreeNode>();

        void Fill(TreeNode parent)
        {
            foreach(TreeNode child in parent.Nodes)
            {
                toReturn.Add(child);
                Fill(child);
            }
        }

        Fill(treeNode);

        return toReturn;
    }
}

#endregion
