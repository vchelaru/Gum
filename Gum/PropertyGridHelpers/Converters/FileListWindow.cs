using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gum.PropertyGridHelpers.Converters
{
    public partial class FileListWindow : Form
    {
        public FileListWindow()
        {
            InitializeComponent();

            removeFileToolStripMenuItem.Visible = false;
        }

        public List<string> GetList()
        {
            List<string> toReturn = new List<string>();
            foreach (TreeNode treeNode in treeView1.Nodes)
            {
                toReturn.Add(treeNode.Text);
            }

            return toReturn;
        }

        private void treeView1_MouseClick(object? sender, MouseEventArgs e)
        {

        }

        private void addFileToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "PNG File (*.png)|*.png";
            openFileDialog.Title = "Select file";

            DialogResult result = openFileDialog.ShowDialog();



            if (result == DialogResult.OK)
            {
                treeView1.Nodes.Add(openFileDialog.FileName);
            }
        }

        private void removeFileToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                treeView1.Nodes.Remove(treeView1.SelectedNode);
            }
        }

        private void treeView1_MouseDown(object? sender, MouseEventArgs e)
        {
            removeFileToolStripMenuItem.Visible = treeView1.SelectedNode != null;
        }

        public void FillFrom(List<string> listToFillFrom)
        {
            if (listToFillFrom != null)
            {
                foreach (var item in listToFillFrom)
                {
                    treeView1.Nodes.Add(item);
                }
            }
        }
    }
}
