using System;
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

            FormUtilities.Self.PositionCenterToCursor(this);

            this.textBox1.KeyDown += HandleTextBoxKeyDown;

            //StartPosition = FormStartPosition.Manual;
            //Location = new Point(TextInputWindow.MousePosition.X, TextInputWindow.MousePosition.Y);

            this.Shown += OnShown;
        }

        private void HandleTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
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
