using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CommonFormsAndControls
{
    public partial class TextInputWindow : Form
    {
        #region Properties

        public string Message
        {
            get
            {
                return MessageLabel.Text;
            }
            set
            {
                MessageLabel.Text = value;
            }
        }

        public string Result
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        #endregion

        public TextInputWindow()
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            FormUtilities.Self.PositionTopLeftToCursor(this);

            //StartPosition = FormStartPosition.Manual;
            //Location = new Point(TextInputWindow.MousePosition.X, TextInputWindow.MousePosition.Y);

            this.Shown += OnShown;
        }

        void OnShown(object sender, EventArgs e)
        {
            this.textBox1.Focus();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
