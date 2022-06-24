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
            this.WireframeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PropertyGridMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
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
            this.ElementTreeImages.Images.SetKeyName(9, "InheritedInstance.png");
            // 
            // WireframeContextMenuStrip
            // 
            this.WireframeContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.WireframeContextMenuStrip.Name = "WireframeContextMenuStrip";
            this.WireframeContextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // PropertyGridMenuStrip
            // 
            this.PropertyGridMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.PropertyGridMenuStrip.Name = "PropertyGridMenuStrip";
            this.PropertyGridMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.PropertyGridMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.PropertyGridMenuStrip_Opening);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1435, 794);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "MainWindow";
            this.Text = "Gum";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip WireframeContextMenuStrip;
        private System.Windows.Forms.ContextMenuStrip PropertyGridMenuStrip;

        private System.Windows.Forms.ImageList ElementTreeImages;

    }
}

