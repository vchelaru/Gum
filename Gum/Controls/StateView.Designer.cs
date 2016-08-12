namespace Gum.Controls
{
    partial class StateView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StateView));
            this.multiSelectTreeView1 = new CommonFormsAndControls.MultiSelectTreeView();
            this.TreeView = new CommonFormsAndControls.MultiSelectTreeView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SingleStateRadio = new System.Windows.Forms.RadioButton();
            this.StackStateRadio = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // multiSelectTreeView1
            // 
            this.multiSelectTreeView1.AlwaysHaveOneNodeSelected = false;
            this.multiSelectTreeView1.Location = new System.Drawing.Point(74, 77);
            this.multiSelectTreeView1.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.multiSelectTreeView1.Name = "multiSelectTreeView1";
            this.multiSelectTreeView1.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("multiSelectTreeView1.SelectedNodes")));
            this.multiSelectTreeView1.Size = new System.Drawing.Size(121, 97);
            this.multiSelectTreeView1.TabIndex = 0;
            // 
            // TreeView
            // 
            this.TreeView.AlwaysHaveOneNodeSelected = false;
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.Location = new System.Drawing.Point(0, 52);
            this.TreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.TreeView.Name = "TreeView";
            this.TreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("TreeView.SelectedNodes")));
            this.TreeView.Size = new System.Drawing.Size(150, 98);
            this.TreeView.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.StackStateRadio);
            this.panel1.Controls.Add(this.SingleStateRadio);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(150, 52);
            this.panel1.TabIndex = 2;
            // 
            // SingleStateRadio
            // 
            this.SingleStateRadio.AutoSize = true;
            this.SingleStateRadio.Checked = true;
            this.SingleStateRadio.Location = new System.Drawing.Point(4, 4);
            this.SingleStateRadio.Name = "SingleStateRadio";
            this.SingleStateRadio.Size = new System.Drawing.Size(82, 17);
            this.SingleStateRadio.TabIndex = 0;
            this.SingleStateRadio.TabStop = true;
            this.SingleStateRadio.Text = "Single State";
            this.SingleStateRadio.UseVisualStyleBackColor = true;
            this.SingleStateRadio.CheckedChanged += new System.EventHandler(this.SingleStateRadio_CheckedChanged);
            // 
            // StackStateRadio
            // 
            this.StackStateRadio.AutoSize = true;
            this.StackStateRadio.Location = new System.Drawing.Point(4, 27);
            this.StackStateRadio.Name = "StackStateRadio";
            this.StackStateRadio.Size = new System.Drawing.Size(99, 17);
            this.StackStateRadio.TabIndex = 1;
            this.StackStateRadio.Text = "Combine States";
            this.StackStateRadio.UseVisualStyleBackColor = true;
            this.StackStateRadio.CheckedChanged += new System.EventHandler(this.StackStateRadio_CheckedChanged);
            // 
            // StateView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TreeView);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.multiSelectTreeView1);
            this.Name = "StateView";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private CommonFormsAndControls.MultiSelectTreeView multiSelectTreeView1;
        public CommonFormsAndControls.MultiSelectTreeView TreeView;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton StackStateRadio;
        private System.Windows.Forms.RadioButton SingleStateRadio;
    }
}
