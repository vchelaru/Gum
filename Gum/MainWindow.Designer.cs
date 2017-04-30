namespace Gum
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadRecentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.componentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.instanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveElementMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveStateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveVariableMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearFontCacheToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findFileReferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managePluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LeftAndEverythingContainer = new System.Windows.Forms.SplitContainer();
            this.ObjectTreeView = new CommonFormsAndControls.MultiSelectTreeView();
            this.ElementMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ElementTreeImages = new System.Windows.Forms.ImageList(this.components);
            this.VariablesAndEverythingElse = new System.Windows.Forms.SplitContainer();
            this.StatesAndVariablesContainer = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.MiddleTabControl = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.VariableHost = new System.Windows.Forms.Integration.ElementHost();
            this.testWpfControl1 = new Gum.TestWpfControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.EventsHost = new System.Windows.Forms.Integration.ElementHost();
            this.testWpfControl2 = new Gum.TestWpfControl();
            this.PreviewSplitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ToolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.wireframeControl1 = new Gum.Wireframe.WireframeControl();
            this.WireframeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.WireframeEditControl = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl();
            this.panel2 = new System.Windows.Forms.Panel();
            this.RightTabControl = new System.Windows.Forms.TabControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.OutputTextBox = new System.Windows.Forms.RichTextBox();
            this.PropertyGridMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LeftAndEverythingContainer)).BeginInit();
            this.LeftAndEverythingContainer.Panel1.SuspendLayout();
            this.LeftAndEverythingContainer.Panel2.SuspendLayout();
            this.LeftAndEverythingContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.VariablesAndEverythingElse)).BeginInit();
            this.VariablesAndEverythingElse.Panel1.SuspendLayout();
            this.VariablesAndEverythingElse.Panel2.SuspendLayout();
            this.VariablesAndEverythingElse.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StatesAndVariablesContainer)).BeginInit();
            this.StatesAndVariablesContainer.Panel1.SuspendLayout();
            this.StatesAndVariablesContainer.Panel2.SuspendLayout();
            this.StatesAndVariablesContainer.SuspendLayout();
            this.MiddleTabControl.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).BeginInit();
            this.PreviewSplitContainer.Panel1.SuspendLayout();
            this.PreviewSplitContainer.Panel2.SuspendLayout();
            this.PreviewSplitContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.RightTabControl.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.contentToolStripMenuItem,
            this.pluginsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1076, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadProjectToolStripMenuItem,
            this.loadRecentToolStripMenuItem,
            this.saveProjectToolStripMenuItem,
            this.saveAllToolStripMenuItem,
            this.newProjectToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadProjectToolStripMenuItem
            // 
            this.loadProjectToolStripMenuItem.Name = "loadProjectToolStripMenuItem";
            this.loadProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.loadProjectToolStripMenuItem.Text = "Load Project...";
            this.loadProjectToolStripMenuItem.Click += new System.EventHandler(this.loadProjectToolStripMenuItem_Click);
            // 
            // loadRecentToolStripMenuItem
            // 
            this.loadRecentToolStripMenuItem.Name = "loadRecentToolStripMenuItem";
            this.loadRecentToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.loadRecentToolStripMenuItem.Text = "Load Recent";
            // 
            // saveProjectToolStripMenuItem
            // 
            this.saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            this.saveProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.saveProjectToolStripMenuItem.Text = "Save Project";
            this.saveProjectToolStripMenuItem.Click += new System.EventHandler(this.saveProjectToolStripMenuItem_Click);
            // 
            // saveAllToolStripMenuItem
            // 
            this.saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            this.saveAllToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.saveAllToolStripMenuItem.Text = "Save All";
            this.saveAllToolStripMenuItem.Click += new System.EventHandler(this.saveAllToolStripMenuItem_Click);
            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += new System.EventHandler(this.newProjectToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.toolStripSeparator1,
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.undoToolStripMenuItem.Text = "Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.screenToolStripMenuItem,
            this.componentToolStripMenuItem,
            this.instanceToolStripMenuItem});
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.addToolStripMenuItem.Text = "Add";
            // 
            // screenToolStripMenuItem
            // 
            this.screenToolStripMenuItem.Name = "screenToolStripMenuItem";
            this.screenToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.screenToolStripMenuItem.Text = "Screen";
            this.screenToolStripMenuItem.Click += new System.EventHandler(this.screenToolStripMenuItem_Click);
            // 
            // componentToolStripMenuItem
            // 
            this.componentToolStripMenuItem.Name = "componentToolStripMenuItem";
            this.componentToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.componentToolStripMenuItem.Text = "Component";
            this.componentToolStripMenuItem.Click += new System.EventHandler(this.componentToolStripMenuItem_Click);
            // 
            // instanceToolStripMenuItem
            // 
            this.instanceToolStripMenuItem.Name = "instanceToolStripMenuItem";
            this.instanceToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.instanceToolStripMenuItem.Text = "Object";
            this.instanceToolStripMenuItem.Click += new System.EventHandler(this.instanceToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveElementMenuItem,
            this.RemoveStateMenuItem,
            this.RemoveVariableMenuItem});
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            // 
            // RemoveElementMenuItem
            // 
            this.RemoveElementMenuItem.Name = "RemoveElementMenuItem";
            this.RemoveElementMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveElementMenuItem.Text = "Element";
            // 
            // RemoveStateMenuItem
            // 
            this.RemoveStateMenuItem.Name = "RemoveStateMenuItem";
            this.RemoveStateMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveStateMenuItem.Text = "State";
            // 
            // RemoveVariableMenuItem
            // 
            this.RemoveVariableMenuItem.Name = "RemoveVariableMenuItem";
            this.RemoveVariableMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveVariableMenuItem.Text = "Variable";
            // 
            // contentToolStripMenuItem
            // 
            this.contentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearFontCacheToolStripMenuItem,
            this.findFileReferencesToolStripMenuItem});
            this.contentToolStripMenuItem.Name = "contentToolStripMenuItem";
            this.contentToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.contentToolStripMenuItem.Text = "Content";
            // 
            // clearFontCacheToolStripMenuItem
            // 
            this.clearFontCacheToolStripMenuItem.Name = "clearFontCacheToolStripMenuItem";
            this.clearFontCacheToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.clearFontCacheToolStripMenuItem.Text = "Clear Font Cache";
            this.clearFontCacheToolStripMenuItem.Click += new System.EventHandler(this.clearFontCacheToolStripMenuItem_Click);
            // 
            // findFileReferencesToolStripMenuItem
            // 
            this.findFileReferencesToolStripMenuItem.Name = "findFileReferencesToolStripMenuItem";
            this.findFileReferencesToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.findFileReferencesToolStripMenuItem.Text = "Find file references...";
            this.findFileReferencesToolStripMenuItem.Click += new System.EventHandler(this.findFileReferencesToolStripMenuItem_Click);
            // 
            // pluginsToolStripMenuItem
            // 
            this.pluginsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.managePluginsToolStripMenuItem});
            this.pluginsToolStripMenuItem.Name = "pluginsToolStripMenuItem";
            this.pluginsToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.pluginsToolStripMenuItem.Text = "Plugins";
            // 
            // managePluginsToolStripMenuItem
            // 
            this.managePluginsToolStripMenuItem.Name = "managePluginsToolStripMenuItem";
            this.managePluginsToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.managePluginsToolStripMenuItem.Text = "Manage Plugins";
            this.managePluginsToolStripMenuItem.Click += new System.EventHandler(this.managePluginsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // stateToolStripMenuItem
            // 
            this.stateToolStripMenuItem.Name = "stateToolStripMenuItem";
            this.stateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.stateToolStripMenuItem.Text = "State";
            this.stateToolStripMenuItem.Click += new System.EventHandler(this.stateToolStripMenuItem_Click);
            // 
            // LeftAndEverythingContainer
            // 
            this.LeftAndEverythingContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LeftAndEverythingContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.LeftAndEverythingContainer.Location = new System.Drawing.Point(0, 24);
            this.LeftAndEverythingContainer.Name = "LeftAndEverythingContainer";
            // 
            // LeftAndEverythingContainer.Panel1
            // 
            this.LeftAndEverythingContainer.Panel1.Controls.Add(this.ObjectTreeView);
            // 
            // LeftAndEverythingContainer.Panel2
            // 
            this.LeftAndEverythingContainer.Panel2.Controls.Add(this.VariablesAndEverythingElse);
            this.LeftAndEverythingContainer.Size = new System.Drawing.Size(1076, 621);
            this.LeftAndEverythingContainer.SplitterDistance = 196;
            this.LeftAndEverythingContainer.TabIndex = 1;
            // 
            // ObjectTreeView
            // 
            this.ObjectTreeView.AllowDrop = true;
            this.ObjectTreeView.AlwaysHaveOneNodeSelected = false;
            this.ObjectTreeView.ContextMenuStrip = this.ElementMenuStrip;
            this.ObjectTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectTreeView.HotTracking = true;
            this.ObjectTreeView.ImageIndex = 0;
            this.ObjectTreeView.ImageList = this.ElementTreeImages;
            this.ObjectTreeView.Location = new System.Drawing.Point(0, 0);
            this.ObjectTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.ObjectTreeView.Name = "ObjectTreeView";
            this.ObjectTreeView.SelectedImageIndex = 0;
            this.ObjectTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("ObjectTreeView.SelectedNodes")));
            this.ObjectTreeView.Size = new System.Drawing.Size(196, 621);
            this.ObjectTreeView.TabIndex = 0;
            this.ObjectTreeView.AfterClickSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectTreeView_AfterClickSelect);
            this.ObjectTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.ObjectTreeView_ItemDrag);
            this.ObjectTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectTreeView_AfterSelect_1);
            this.ObjectTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ObjectTreeView_KeyDown);
            this.ObjectTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ObjectTreeView_MouseClick);
            this.ObjectTreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ObjectTreeView_MouseMove);
            // 
            // ElementMenuStrip
            // 
            this.ElementMenuStrip.Name = "ElementMenuStrip";
            this.ElementMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // ElementTreeImages
            // 
            this.ElementTreeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ElementTreeImages.ImageStream")));
            this.ElementTreeImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ElementTreeImages.Images.SetKeyName(0, "transparent.png");
            this.ElementTreeImages.Images.SetKeyName(1, "folder.png");
            this.ElementTreeImages.Images.SetKeyName(2, "Component.png");
            this.ElementTreeImages.Images.SetKeyName(3, "Instance.png");
            this.ElementTreeImages.Images.SetKeyName(4, "screen.png");
            this.ElementTreeImages.Images.SetKeyName(5, "StandardElement.png");
            this.ElementTreeImages.Images.SetKeyName(6, "redExclamation.png");
            this.ElementTreeImages.Images.SetKeyName(7, "state.png");
            this.ElementTreeImages.Images.SetKeyName(8, "behavior.png");
            // 
            // VariablesAndEverythingElse
            // 
            this.VariablesAndEverythingElse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VariablesAndEverythingElse.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.VariablesAndEverythingElse.Location = new System.Drawing.Point(0, 0);
            this.VariablesAndEverythingElse.Name = "VariablesAndEverythingElse";
            // 
            // VariablesAndEverythingElse.Panel1
            // 
            this.VariablesAndEverythingElse.Panel1.Controls.Add(this.StatesAndVariablesContainer);
            // 
            // VariablesAndEverythingElse.Panel2
            // 
            this.VariablesAndEverythingElse.Panel2.Controls.Add(this.PreviewSplitContainer);
            this.VariablesAndEverythingElse.Size = new System.Drawing.Size(876, 621);
            this.VariablesAndEverythingElse.SplitterDistance = 242;
            this.VariablesAndEverythingElse.TabIndex = 0;
            this.VariablesAndEverythingElse.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.VariablesAndEverythingElse_SplitterMoved);
            // 
            // StatesAndVariablesContainer
            // 
            this.StatesAndVariablesContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatesAndVariablesContainer.Location = new System.Drawing.Point(0, 0);
            this.StatesAndVariablesContainer.Name = "StatesAndVariablesContainer";
            this.StatesAndVariablesContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // StatesAndVariablesContainer.Panel1
            // 
            this.StatesAndVariablesContainer.Panel1.Controls.Add(this.tabControl1);
            // 
            // StatesAndVariablesContainer.Panel2
            // 
            this.StatesAndVariablesContainer.Panel2.Controls.Add(this.MiddleTabControl);
            this.StatesAndVariablesContainer.Size = new System.Drawing.Size(242, 621);
            this.StatesAndVariablesContainer.SplitterDistance = 119;
            this.StatesAndVariablesContainer.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(242, 119);
            this.tabControl1.TabIndex = 1;
            // 
            // MiddleTabControl
            // 
            this.MiddleTabControl.Controls.Add(this.tabPage2);
            this.MiddleTabControl.Controls.Add(this.tabPage1);
            this.MiddleTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MiddleTabControl.Location = new System.Drawing.Point(0, 0);
            this.MiddleTabControl.Name = "MiddleTabControl";
            this.MiddleTabControl.SelectedIndex = 0;
            this.MiddleTabControl.Size = new System.Drawing.Size(242, 498);
            this.MiddleTabControl.TabIndex = 3;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.VariableHost);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(234, 472);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Variables";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // VariableHost
            // 
            this.VariableHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VariableHost.Location = new System.Drawing.Point(3, 3);
            this.VariableHost.Name = "VariableHost";
            this.VariableHost.Size = new System.Drawing.Size(228, 466);
            this.VariableHost.TabIndex = 0;
            this.VariableHost.Text = "elementHost1";
            this.VariableHost.Child = this.testWpfControl1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.EventsHost);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(234, 472);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Events";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // EventsHost
            // 
            this.EventsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EventsHost.Location = new System.Drawing.Point(3, 3);
            this.EventsHost.Name = "EventsHost";
            this.EventsHost.Size = new System.Drawing.Size(228, 466);
            this.EventsHost.TabIndex = 3;
            this.EventsHost.Text = "elementHost1";
            this.EventsHost.Child = this.testWpfControl2;
            // 
            // PreviewSplitContainer
            // 
            this.PreviewSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PreviewSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.PreviewSplitContainer.Name = "PreviewSplitContainer";
            this.PreviewSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // PreviewSplitContainer.Panel1
            // 
            this.PreviewSplitContainer.Panel1.Controls.Add(this.panel1);
            // 
            // PreviewSplitContainer.Panel2
            // 
            this.PreviewSplitContainer.Panel2.Controls.Add(this.panel2);
            this.PreviewSplitContainer.Size = new System.Drawing.Size(630, 621);
            this.PreviewSplitContainer.SplitterDistance = 512;
            this.PreviewSplitContainer.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.ToolbarPanel);
            this.panel1.Controls.Add(this.wireframeControl1);
            this.panel1.Controls.Add(this.WireframeEditControl);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(630, 512);
            this.panel1.TabIndex = 0;
            // 
            // ToolbarPanel
            // 
            this.ToolbarPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ToolbarPanel.Location = new System.Drawing.Point(0, 22);
            this.ToolbarPanel.Name = "ToolbarPanel";
            this.ToolbarPanel.Size = new System.Drawing.Size(622, 31);
            this.ToolbarPanel.TabIndex = 2;
            // 
            // wireframeControl1
            // 
            this.wireframeControl1.AllowDrop = true;
            this.wireframeControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.wireframeControl1.ContextMenuStrip = this.WireframeContextMenuStrip;
            this.wireframeControl1.Cursor = System.Windows.Forms.Cursors.Default;
            this.wireframeControl1.DesiredFramesPerSecond = 30F;
            this.wireframeControl1.Location = new System.Drawing.Point(0, 52);
            this.wireframeControl1.Name = "wireframeControl1";
            this.wireframeControl1.Size = new System.Drawing.Size(622, 452);
            this.wireframeControl1.TabIndex = 0;
            this.wireframeControl1.Text = "wireframeControl1";
            this.wireframeControl1.DragDrop += new System.Windows.Forms.DragEventHandler(this.wireframeControl1_DragDrop);
            this.wireframeControl1.DragEnter += new System.Windows.Forms.DragEventHandler(this.wireframeControl1_DragEnter);
            this.wireframeControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.wireframeControl1_MouseClick);
            // 
            // WireframeContextMenuStrip
            // 
            this.WireframeContextMenuStrip.Name = "WireframeContextMenuStrip";
            this.WireframeContextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // WireframeEditControl
            // 
            this.WireframeEditControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WireframeEditControl.Location = new System.Drawing.Point(0, 0);
            this.WireframeEditControl.Margin = new System.Windows.Forms.Padding(4);
            this.WireframeEditControl.Name = "WireframeEditControl";
            this.WireframeEditControl.PercentageValue = 100;
            this.WireframeEditControl.Size = new System.Drawing.Size(622, 22);
            this.WireframeEditControl.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.RightTabControl);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(630, 105);
            this.panel2.TabIndex = 0;
            // 
            // RightTabControl
            // 
            this.RightTabControl.Controls.Add(this.tabPage3);
            this.RightTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightTabControl.Location = new System.Drawing.Point(0, 0);
            this.RightTabControl.Name = "RightTabControl";
            this.RightTabControl.SelectedIndex = 0;
            this.RightTabControl.Size = new System.Drawing.Size(630, 105);
            this.RightTabControl.TabIndex = 1;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.OutputTextBox);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(622, 79);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "Output";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // OutputTextBox
            // 
            this.OutputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OutputTextBox.Location = new System.Drawing.Point(3, 3);
            this.OutputTextBox.Name = "OutputTextBox";
            this.OutputTextBox.Size = new System.Drawing.Size(616, 73);
            this.OutputTextBox.TabIndex = 0;
            this.OutputTextBox.Text = "";
            // 
            // PropertyGridMenuStrip
            // 
            this.PropertyGridMenuStrip.Name = "PropertyGridMenuStrip";
            this.PropertyGridMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.PropertyGridMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.PropertyGridMenuStrip_Opening);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1076, 645);
            this.Controls.Add(this.LeftAndEverythingContainer);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Gum";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.LeftAndEverythingContainer.Panel1.ResumeLayout(false);
            this.LeftAndEverythingContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.LeftAndEverythingContainer)).EndInit();
            this.LeftAndEverythingContainer.ResumeLayout(false);
            this.VariablesAndEverythingElse.Panel1.ResumeLayout(false);
            this.VariablesAndEverythingElse.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.VariablesAndEverythingElse)).EndInit();
            this.VariablesAndEverythingElse.ResumeLayout(false);
            this.StatesAndVariablesContainer.Panel1.ResumeLayout(false);
            this.StatesAndVariablesContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.StatesAndVariablesContainer)).EndInit();
            this.StatesAndVariablesContainer.ResumeLayout(false);
            this.MiddleTabControl.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.PreviewSplitContainer.Panel1.ResumeLayout(false);
            this.PreviewSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).EndInit();
            this.PreviewSplitContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.RightTabControl.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.SplitContainer LeftAndEverythingContainer;
        private CommonFormsAndControls.MultiSelectTreeView ObjectTreeView;
        private System.Windows.Forms.SplitContainer VariablesAndEverythingElse;
        private System.Windows.Forms.SplitContainer StatesAndVariablesContainer;
        private System.Windows.Forms.SplitContainer PreviewSplitContainer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem screenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem componentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem instanceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RemoveStateMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RemoveElementMenuItem;
        private System.Windows.Forms.ContextMenuStrip ElementMenuStrip;
        private Wireframe.WireframeControl wireframeControl1;
        private System.Windows.Forms.ContextMenuStrip WireframeContextMenuStrip;
        private System.Windows.Forms.ContextMenuStrip PropertyGridMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem pluginsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem managePluginsToolStripMenuItem;
        private System.Windows.Forms.ImageList ElementTreeImages;
        private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl WireframeEditControl;
        private System.Windows.Forms.ToolStripMenuItem saveProjectToolStripMenuItem;
        public System.Windows.Forms.FlowLayoutPanel ToolbarPanel;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newProjectToolStripMenuItem;
        private System.Windows.Forms.RichTextBox OutputTextBox;
        private System.Windows.Forms.TabControl MiddleTabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Integration.ElementHost VariableHost;
        private TestWpfControl testWpfControl1;
        private System.Windows.Forms.ToolStripMenuItem contentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearFontCacheToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAllToolStripMenuItem;
        private System.Windows.Forms.Integration.ElementHost EventsHost;
        private TestWpfControl testWpfControl2;
        private System.Windows.Forms.ToolStripMenuItem loadRecentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findFileReferencesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TabControl RightTabControl;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.ToolStripMenuItem RemoveVariableMenuItem;
    }
}

