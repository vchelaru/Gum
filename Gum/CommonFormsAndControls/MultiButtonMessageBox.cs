using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CommonFormsAndControls.Forms
{
    /// <summary>
    /// Form used to show multiple options to a user.
    /// </summary>
    /// <remarks>
    /// This was mostly pulled from Glue.
    /// </remarks>
    public partial class MultiButtonMessageBox : Form
    {
        #region Fields

        List<Button> mButtons = new List<Button>();

        #endregion

        public string MessageText
        {
            set
            {
                label1.Text = value;
            }
        }

        void CloseThisWindow(object sender, EventArgs args)
        {
            this.Close();
        }


        public MultiButtonMessageBox()
        {
            InitializeComponent();



        }

        public void AddButton(string text, DialogResult result)
        {
            Button button = new Button();
            button.Size = new System.Drawing.Size(FlowLayoutPanel.Width, 40);
            button.TabIndex = mButtons.Count;
            button.Text = text;
            button.DialogResult = result;
            button.UseVisualStyleBackColor = true;
            button.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            mButtons.Add(button);

            this.FlowLayoutPanel.Controls.Add(button);

            this.Size = new Size(
                this.Size.Width, System.Math.Max(Size.Height, button.Location.Y + button.Size.Height + 30));
        }
    }
}
