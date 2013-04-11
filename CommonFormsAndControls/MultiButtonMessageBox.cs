using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            button.Location = new System.Drawing.Point(12, 116 + 53 * mButtons.Count);
            button.Size = new System.Drawing.Size(this.Size.Width - 30, 47);
            button.TabIndex = mButtons.Count;
            button.Text = text;
            button.DialogResult = result;
            button.UseVisualStyleBackColor = true;

            mButtons.Add(button);

            this.Controls.Add(button);

            this.Size = new Size(
                this.Size.Width, System.Math.Max(Size.Height, button.Location.Y + button.Size.Height + 30));
        }
    }
}
