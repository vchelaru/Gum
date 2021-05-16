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
            this.ElementTreeImages = new System.Windows.Forms.ImageList(this.components);
            this.LeftAndEverythingContainer = new System.Windows.Forms.SplitContainer();
            this.LeftTabControl = new System.Windows.Forms.TabControl();
            this.VariablesAndEverythingElse = new System.Windows.Forms.SplitContainer();
            this.StatesAndVariablesContainer = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.MiddleTabControl = new System.Windows.Forms.TabControl();
            this.PreviewSplitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.RightTopTabControl = new System.Windows.Forms.TabControl();
            this.panel2 = new System.Windows.Forms.Panel();
            this.RightBottomTabControl = new System.Windows.Forms.TabControl();
            this.WireframeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PropertyGridMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
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
            this.panel2.SuspendLayout();
            this.RightBottomTabControl.SuspendLayout();
            this.SuspendLayout();
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
            // LeftAndEverythingContainer
            // 
            this.LeftAndEverythingContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LeftAndEverythingContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.LeftAndEverythingContainer.Location = new System.Drawing.Point(0, 0);
            this.LeftAndEverythingContainer.Name = "LeftAndEverythingContainer";
            // 
            // LeftAndEverythingContainer.Panel1
            // 
            this.LeftAndEverythingContainer.Panel1.Controls.Add(this.LeftTabControl);
            // 
            // LeftAndEverythingContainer.Panel2
            // 
            this.LeftAndEverythingContainer.Panel2.Controls.Add(this.VariablesAndEverythingElse);
            this.LeftAndEverythingContainer.Size = new System.Drawing.Size(1076, 645);
            this.LeftAndEverythingContainer.SplitterDistance = 196;
            this.LeftAndEverythingContainer.TabIndex = 1;
            // 
            // LeftTabControl
            // 
            this.LeftTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LeftTabControl.Location = new System.Drawing.Point(0, 0);
            this.LeftTabControl.Name = "LeftTabControl";
            this.LeftTabControl.SelectedIndex = 0;
            this.LeftTabControl.Size = new System.Drawing.Size(196, 645);
            this.LeftTabControl.TabIndex = 4;
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
            this.VariablesAndEverythingElse.Size = new System.Drawing.Size(876, 645);
            this.VariablesAndEverythingElse.SplitterDistance = 332;
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
            this.StatesAndVariablesContainer.Size = new System.Drawing.Size(332, 645);
            this.StatesAndVariablesContainer.SplitterDistance = 123;
            this.StatesAndVariablesContainer.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(332, 123);
            this.tabControl1.TabIndex = 1;
            // 
            // MiddleTabControl
            // 
            this.MiddleTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MiddleTabControl.Location = new System.Drawing.Point(0, 0);
            this.MiddleTabControl.Name = "MiddleTabControl";
            this.MiddleTabControl.SelectedIndex = 0;
            this.MiddleTabControl.Size = new System.Drawing.Size(332, 518);
            this.MiddleTabControl.TabIndex = 3;
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
            this.PreviewSplitContainer.Size = new System.Drawing.Size(540, 645);
            this.PreviewSplitContainer.SplitterDistance = 531;
            this.PreviewSplitContainer.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.RightTopTabControl);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(540, 531);
            this.panel1.TabIndex = 0;
            // 
            // RightTopTabControl
            // 
            this.RightTopTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightTopTabControl.Location = new System.Drawing.Point(0, 0);
            this.RightTopTabControl.Name = "RightTopTabControl";
            this.RightTopTabControl.SelectedIndex = 0;
            this.RightTopTabControl.Size = new System.Drawing.Size(536, 527);
            this.RightTopTabControl.TabIndex = 2;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.RightBottomTabControl);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(540, 110);
            this.panel2.TabIndex = 0;
            // 
            // RightBottomTabControl
            // 
            this.RightBottomTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightBottomTabControl.Location = new System.Drawing.Point(0, 0);
            this.RightBottomTabControl.Name = "RightBottomTabControl";
            this.RightBottomTabControl.SelectedIndex = 0;
            this.RightBottomTabControl.Size = new System.Drawing.Size(540, 110);
            this.RightBottomTabControl.TabIndex = 1;
            // 
            // WireframeContextMenuStrip
            // 
            this.WireframeContextMenuStrip.Name = "WireframeContextMenuStrip";
            this.WireframeContextMenuStrip.Size = new System.Drawing.Size(61, 4);
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
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainWindow";
            this.Text = "Gum";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
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
            this.panel2.ResumeLayout(false);
            this.RightBottomTabControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.SplitContainer StatesAndVariablesContainer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;

        private System.Windows.Forms.ContextMenuStrip WireframeContextMenuStrip;
        private System.Windows.Forms.ContextMenuStrip PropertyGridMenuStrip;
        private System.Windows.Forms.TabControl RightBottomTabControl;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabControl MiddleTabControl;
        private System.Windows.Forms.TabControl LeftTabControl;
        private System.Windows.Forms.TabControl RightTopTabControl;
        private System.Windows.Forms.ImageList ElementTreeImages;
        public System.Windows.Forms.SplitContainer LeftAndEverythingContainer;
        public System.Windows.Forms.SplitContainer VariablesAndEverythingElse;
        public System.Windows.Forms.SplitContainer PreviewSplitContainer;
    }
}

