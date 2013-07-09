namespace Gum.Gui.Forms
{
    partial class ProjectPropertyGridWindow
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
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.GuideListDisplay = new Gum.Gui.Controls.GuideListDisplay();
            this.AutoSaveCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.HelpVisible = false;
            this.propertyGrid1.Location = new System.Drawing.Point(12, 35);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(262, 72);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            // 
            // GuideListDisplay
            // 
            this.GuideListDisplay.GumProjectSave = null;
            this.GuideListDisplay.Location = new System.Drawing.Point(12, 118);
            this.GuideListDisplay.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.GuideListDisplay.Name = "GuideListDisplay";
            this.GuideListDisplay.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.GuideListDisplay.Size = new System.Drawing.Size(262, 142);
            this.GuideListDisplay.TabIndex = 1;
            // 
            // AutoSaveCheckBox
            // 
            this.AutoSaveCheckBox.AutoSize = true;
            this.AutoSaveCheckBox.Location = new System.Drawing.Point(12, 12);
            this.AutoSaveCheckBox.Name = "AutoSaveCheckBox";
            this.AutoSaveCheckBox.Size = new System.Drawing.Size(76, 17);
            this.AutoSaveCheckBox.TabIndex = 2;
            this.AutoSaveCheckBox.Text = "Auto Save";
            this.AutoSaveCheckBox.UseVisualStyleBackColor = true;
            this.AutoSaveCheckBox.CheckedChanged += new System.EventHandler(this.AutoSaveCheckBox_CheckedChanged);
            // 
            // ProjectPropertyGridWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(286, 272);
            this.Controls.Add(this.AutoSaveCheckBox);
            this.Controls.Add(this.GuideListDisplay);
            this.Controls.Add(this.propertyGrid1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProjectPropertyGridWindow";
            this.Text = "Project Properties";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ProjectPropertyGridWindow_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private Controls.GuideListDisplay GuideListDisplay;
        private System.Windows.Forms.CheckBox AutoSaveCheckBox;
    }
}