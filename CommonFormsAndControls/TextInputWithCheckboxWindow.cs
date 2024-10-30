using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommonFormsAndControls
{
    public partial class TextInputWithCheckboxWindow : Form
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

        public string Option
        {
            get
            {
                return OptionCheckbox.Text;
            }
            set
            {
                OptionCheckbox.Text = value;
            }
        }

        public string Result
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public bool Checked
        {
            get
            {
                return OptionCheckbox.Checked;
            }
            set
            {
                OptionCheckbox.Checked = value;
            }
        }

        #endregion
        public TextInputWithCheckboxWindow()
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            FormUtilities.Self.PositionCenterToCursor(this);

            this.textBox1.KeyDown += HandleTextBoxKeyDown;
            this.Shown += OnShown;
        }

        private void HandleTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CancelButton_Click(this, null);
                    break;
            }
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
