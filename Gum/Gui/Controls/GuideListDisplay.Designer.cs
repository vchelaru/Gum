namespace Gum.Gui.Controls
{
    partial class GuideListDisplay
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
            this.GuidesComboBox = new System.Windows.Forms.ComboBox();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // GuidesComboBox
            // 
            this.GuidesComboBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.GuidesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.GuidesComboBox.FormattingEnabled = true;
            this.GuidesComboBox.Location = new System.Drawing.Point(0, 0);
            this.GuidesComboBox.Name = "GuidesComboBox";
            this.GuidesComboBox.Size = new System.Drawing.Size(269, 21);
            this.GuidesComboBox.TabIndex = 0;
            this.GuidesComboBox.SelectedIndexChanged += new System.EventHandler(this.GuidesComboBox_SelectedIndexChanged);
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.HelpVisible = false;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 21);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGrid1.Size = new System.Drawing.Size(269, 146);
            this.propertyGrid1.TabIndex = 1;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            // 
            // GuideListDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.GuidesComboBox);
            this.Name = "GuideListDisplay";
            this.Size = new System.Drawing.Size(269, 167);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox GuidesComboBox;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
    }
}
