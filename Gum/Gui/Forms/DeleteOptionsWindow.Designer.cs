namespace Gum.Gui.Forms
{
    partial class DeleteOptionsWindow
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
            this.MainFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.MessageFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.OptionsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.OkCancelButtonPanel = new System.Windows.Forms.Panel();
            this.NoButton = new System.Windows.Forms.Button();
            this.YesButton = new System.Windows.Forms.Button();
            this.MainFlowLayoutPanel.SuspendLayout();
            this.MessageFlowLayoutPanel.SuspendLayout();
            this.OkCancelButtonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainFlowLayoutPanel
            // 
            this.MainFlowLayoutPanel.AutoSize = true;
            this.MainFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainFlowLayoutPanel.Controls.Add(this.MessageFlowLayoutPanel);
            this.MainFlowLayoutPanel.Controls.Add(this.OptionsFlowLayoutPanel);
            this.MainFlowLayoutPanel.Controls.Add(this.OkCancelButtonPanel);
            this.MainFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.MainFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.MainFlowLayoutPanel.Name = "MainFlowLayoutPanel";
            this.MainFlowLayoutPanel.Size = new System.Drawing.Size(344, 121);
            this.MainFlowLayoutPanel.TabIndex = 0;
            // 
            // MessageFlowLayoutPanel
            // 
            this.MessageFlowLayoutPanel.Controls.Add(this.label1);
            this.MessageFlowLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.MessageFlowLayoutPanel.Name = "MessageFlowLayoutPanel";
            this.MessageFlowLayoutPanel.Size = new System.Drawing.Size(328, 38);
            this.MessageFlowLayoutPanel.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(179, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Are you sure you want to delete {0}?";
            // 
            // OptionsFlowLayoutPanel
            // 
            this.OptionsFlowLayoutPanel.AutoSize = true;
            this.OptionsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.OptionsFlowLayoutPanel.Location = new System.Drawing.Point(3, 47);
            this.OptionsFlowLayoutPanel.MinimumSize = new System.Drawing.Size(338, 32);
            this.OptionsFlowLayoutPanel.Name = "OptionsFlowLayoutPanel";
            this.OptionsFlowLayoutPanel.Size = new System.Drawing.Size(338, 32);
            this.OptionsFlowLayoutPanel.TabIndex = 1;
            // 
            // OkCancelButtonPanel
            // 
            this.OkCancelButtonPanel.Controls.Add(this.NoButton);
            this.OkCancelButtonPanel.Controls.Add(this.YesButton);
            this.OkCancelButtonPanel.Location = new System.Drawing.Point(3, 85);
            this.OkCancelButtonPanel.Name = "OkCancelButtonPanel";
            this.OkCancelButtonPanel.Size = new System.Drawing.Size(328, 33);
            this.OkCancelButtonPanel.TabIndex = 2;
            // 
            // NoButton
            // 
            this.NoButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.NoButton.Location = new System.Drawing.Point(250, 3);
            this.NoButton.Name = "NoButton";
            this.NoButton.Size = new System.Drawing.Size(75, 23);
            this.NoButton.TabIndex = 1;
            this.NoButton.Text = "No";
            this.NoButton.UseVisualStyleBackColor = true;
            // 
            // YesButton
            // 
            this.YesButton.Location = new System.Drawing.Point(169, 3);
            this.YesButton.Name = "YesButton";
            this.YesButton.Size = new System.Drawing.Size(75, 23);
            this.YesButton.TabIndex = 0;
            this.YesButton.Text = "Yes";
            this.YesButton.UseVisualStyleBackColor = true;
            this.YesButton.Click += new System.EventHandler(this.YesButton_Click);
            // 
            // DeleteOptionsWindow
            // 
            this.AcceptButton = this.YesButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.NoButton;
            this.ClientSize = new System.Drawing.Size(343, 249);
            this.Controls.Add(this.MainFlowLayoutPanel);
            this.Name = "DeleteOptionsWindow";
            this.Text = "DeleteOptionsWindow";
            this.MainFlowLayoutPanel.ResumeLayout(false);
            this.MainFlowLayoutPanel.PerformLayout();
            this.MessageFlowLayoutPanel.ResumeLayout(false);
            this.MessageFlowLayoutPanel.PerformLayout();
            this.OkCancelButtonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel MainFlowLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel MessageFlowLayoutPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel OptionsFlowLayoutPanel;
        private System.Windows.Forms.Panel OkCancelButtonPanel;
        private System.Windows.Forms.Button NoButton;
        private System.Windows.Forms.Button YesButton;
    }
}