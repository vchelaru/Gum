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
            this.saveProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.componentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.instanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveElementMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveStateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managePluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LeftAndEverythingContainer = new System.Windows.Forms.SplitContainer();
            this.ObjectTreeView = new CommonFormsAndControls.MultiSelectTreeView();
            this.ElementMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ElementTreeImages = new System.Windows.Forms.ImageList(this.components);
            this.VariablesAndEverythingElse = new System.Windows.Forms.SplitContainer();
            this.StatesAndVariablesContainer = new System.Windows.Forms.SplitContainer();
            this.StateTreeView = new CommonFormsAndControls.MultiSelectTreeView();
            this.StateContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.VariablePropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.PropertyGridMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PreviewSplitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ToolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.wireframeControl1 = new Gum.Wireframe.WireframeControl();
            this.WireframeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.WireframeEditControl = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl();
            this.panel2 = new System.Windows.Forms.Panel();
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
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).BeginInit();
            this.PreviewSplitContainer.Panel1.SuspendLayout();
            this.PreviewSplitContainer.Panel2.SuspendLayout();
            this.PreviewSplitContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
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
            this.saveProjectToolStripMenuItem,
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
            // saveProjectToolStripMenuItem
            // 
            this.saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            this.saveProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.saveProjectToolStripMenuItem.Text = "Save Project";
            this.saveProjectToolStripMenuItem.Click += new System.EventHandler(this.saveProjectToolStripMenuItem_Click);
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
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem,
            this.projectPropertiesToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.screenToolStripMenuItem,
            this.componentToolStripMenuItem,
            this.instanceToolStripMenuItem,
            this.stateToolStripMenuItem});
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
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
            // stateToolStripMenuItem
            // 
            this.stateToolStripMenuItem.Name = "stateToolStripMenuItem";
            this.stateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.stateToolStripMenuItem.Text = "State";
            this.stateToolStripMenuItem.Click += new System.EventHandler(this.stateToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveElementMenuItem,
            this.RemoveStateMenuItem});
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            // 
            // RemoveElementMenuItem
            // 
            this.RemoveElementMenuItem.Name = "RemoveElementMenuItem";
            this.RemoveElementMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveElementMenuItem.Text = "Element";
            this.RemoveElementMenuItem.Click += new System.EventHandler(this.RemoveElementMenuItem_Click);
            // 
            // RemoveStateMenuItem
            // 
            this.RemoveStateMenuItem.Name = "RemoveStateMenuItem";
            this.RemoveStateMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveStateMenuItem.Text = "State";
            this.RemoveStateMenuItem.Click += new System.EventHandler(this.RemoveStateMenuItem_Click);
            // 
            // projectPropertiesToolStripMenuItem
            // 
            this.projectPropertiesToolStripMenuItem.Name = "projectPropertiesToolStripMenuItem";
            this.projectPropertiesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.projectPropertiesToolStripMenuItem.Text = "Project Properties";
            this.projectPropertiesToolStripMenuItem.Click += new System.EventHandler(this.projectPropertiesToolStripMenuItem_Click);
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
            // LeftAndEverythingContainer
            // 
            this.LeftAndEverythingContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
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
            this.ObjectTreeView.ImageIndex = 0;
            this.ObjectTreeView.ImageList = this.ElementTreeImages;
            this.ObjectTreeView.Location = new System.Drawing.Point(0, 0);
            this.ObjectTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.ObjectTreeView.Name = "ObjectTreeView";
            this.ObjectTreeView.SelectedImageIndex = 0;
            this.ObjectTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("ObjectTreeView.SelectedNodes")));
            this.ObjectTreeView.Size = new System.Drawing.Size(192, 617);
            this.ObjectTreeView.TabIndex = 0;
            this.ObjectTreeView.AfterClickSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectTreeView_AfterSelect);
            this.ObjectTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.ObjectTreeView_ItemDrag);
            this.ObjectTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ObjectTreeView_KeyDown);
            this.ObjectTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ObjectTreeView_MouseClick);
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
            this.VariablesAndEverythingElse.Size = new System.Drawing.Size(872, 617);
            this.VariablesAndEverythingElse.SplitterDistance = 242;
            this.VariablesAndEverythingElse.TabIndex = 0;
            this.VariablesAndEverythingElse.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.VariablesAndEverythingElse_SplitterMoved);
            // 
            // StatesAndVariablesContainer
            // 
            this.StatesAndVariablesContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.StatesAndVariablesContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatesAndVariablesContainer.Location = new System.Drawing.Point(0, 0);
            this.StatesAndVariablesContainer.Name = "StatesAndVariablesContainer";
            this.StatesAndVariablesContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // StatesAndVariablesContainer.Panel1
            // 
            this.StatesAndVariablesContainer.Panel1.Controls.Add(this.StateTreeView);
            // 
            // StatesAndVariablesContainer.Panel2
            // 
            this.StatesAndVariablesContainer.Panel2.Controls.Add(this.VariablePropertyGrid);
            this.StatesAndVariablesContainer.Size = new System.Drawing.Size(242, 617);
            this.StatesAndVariablesContainer.SplitterDistance = 120;
            this.StatesAndVariablesContainer.TabIndex = 0;
            // 
            // StateTreeView
            // 
            this.StateTreeView.AlwaysHaveOneNodeSelected = false;
            this.StateTreeView.ContextMenuStrip = this.StateContextMenuStrip;
            this.StateTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StateTreeView.Location = new System.Drawing.Point(0, 0);
            this.StateTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.StateTreeView.Name = "StateTreeView";
            this.StateTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("StateTreeView.SelectedNodes")));
            this.StateTreeView.Size = new System.Drawing.Size(238, 116);
            this.StateTreeView.TabIndex = 0;
            this.StateTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.StateTreeView_AfterSelect);
            this.StateTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StateTreeView_KeyDown);
            this.StateTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.StateTreeView_MouseClick);
            // 
            // StateContextMenuStrip
            // 
            this.StateContextMenuStrip.Name = "StateContextMenuStrip";
            this.StateContextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // VariablePropertyGrid
            // 
            this.VariablePropertyGrid.ContextMenuStrip = this.PropertyGridMenuStrip;
            this.VariablePropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VariablePropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.VariablePropertyGrid.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.VariablePropertyGrid.Name = "VariablePropertyGrid";
            this.VariablePropertyGrid.Size = new System.Drawing.Size(238, 489);
            this.VariablePropertyGrid.TabIndex = 2;
            this.VariablePropertyGrid.ToolbarVisible = false;
            this.VariablePropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.VariablePropertyGrid_PropertyValueChanged);
            // 
            // PropertyGridMenuStrip
            // 
            this.PropertyGridMenuStrip.Name = "PropertyGridMenuStrip";
            this.PropertyGridMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.PropertyGridMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.PropertyGridMenuStrip_Opening);
            // 
            // PreviewSplitContainer
            // 
            this.PreviewSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
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
            this.PreviewSplitContainer.Size = new System.Drawing.Size(626, 617);
            this.PreviewSplitContainer.SplitterDistance = 584;
            this.PreviewSplitContainer.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.ToolbarPanel);
            this.panel1.Controls.Add(this.wireframeControl1);
            this.panel1.Controls.Add(this.WireframeEditControl);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(622, 580);
            this.panel1.TabIndex = 0;
            // 
            // ToolbarPanel
            // 
            this.ToolbarPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ToolbarPanel.Location = new System.Drawing.Point(0, 22);
            this.ToolbarPanel.Name = "ToolbarPanel";
            this.ToolbarPanel.Size = new System.Drawing.Size(618, 31);
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
            this.wireframeControl1.Size = new System.Drawing.Size(618, 524);
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
            this.WireframeEditControl.Name = "WireframeEditControl";
            this.WireframeEditControl.PercentageValue = 100;
            this.WireframeEditControl.Size = new System.Drawing.Size(618, 22);
            this.WireframeEditControl.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(622, 25);
            this.panel2.TabIndex = 0;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1076, 645);
            this.Controls.Add(this.LeftAndEverythingContainer);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Gum";
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
            this.PreviewSplitContainer.Panel1.ResumeLayout(false);
            this.PreviewSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).EndInit();
            this.PreviewSplitContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
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
        private CommonFormsAndControls.MultiSelectTreeView StateTreeView;
        private System.Windows.Forms.PropertyGrid VariablePropertyGrid;
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
        private System.Windows.Forms.ToolStripMenuItem projectPropertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pluginsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem managePluginsToolStripMenuItem;
        private System.Windows.Forms.ImageList ElementTreeImages;
        private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl WireframeEditControl;
        private System.Windows.Forms.ToolStripMenuItem saveProjectToolStripMenuItem;
        public System.Windows.Forms.FlowLayoutPanel ToolbarPanel;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newProjectToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip StateContextMenuStrip;
    }
}

