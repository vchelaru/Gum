using System;
using System.Windows.Forms;

namespace Gum.Gui.Forms
{
    public partial class DeleteOptionsWindow : Form
    {

        public object ObjectToDelete
        {
            get;
            set;
        }

        public string Message
        {
            get { return this.label1.Text; }
            set { this.label1.Text = value; }
        }

        public DeleteOptionsWindow()
        {
            InitializeComponent();




        }

        protected override void OnShown(EventArgs e)
        {
            if (ObjectToDelete == null)
            {
                throw new Exception("You must set ObjectToDelete before showing the DeleteOptionsWindow");
            }
        }



        public void AddUi(Control control)
        {
            this.OptionsFlowLayoutPanel.Controls.Add(control);


        }

        private void YesButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.Close();
        }
    }
}
