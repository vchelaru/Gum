using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gum.Gui.Controls
{
    public partial class UndoList : Form
    {
        public UndoList()
        {
            InitializeComponent();
        }

        void UpdateList()
        {
            this.TreeView.Nodes.Clear();


            // Maybe we'll fill this in at some point


        }
        
    }
}
