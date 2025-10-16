using System.Drawing;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Services;

namespace FlatRedBall.AnimationEditorForms.Controls;

partial class WireframeEditControl
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
        this.ComboBox = new System.Windows.Controls.ComboBox();

        this.SuspendLayout();
        // 
        // ComboBox
        // 
        this.ComboBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        this.ComboBox.IsEditable = false;
        this.ComboBox.Margin = new System.Windows.Thickness(0, 0, 0, 0);
        this.ComboBox.Name = "ComboBox";
        this.ComboBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        this.ComboBox.TabIndex = 0;
        this.ComboBox.SelectionChanged += this.ComboBox_SelectedIndexChanged;

        // Allow us to add the WPF to the FORMS control
        var elementHost = new ElementHost
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            Child = this.ComboBox,
            AutoSize = true
        };

        
        // 
        // WireframeEditControl
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(elementHost);
        this.Name = "WireframeEditControl";
        this.Size = new System.Drawing.Size(215, 21);
        this.ResumeLayout(false);
        this.Load += (_, _) =>
        {
            ComboBox.Resources = Application.Current.Resources;
        };
    }

    #endregion

    private System.Windows.Controls.ComboBox ComboBox;
}
