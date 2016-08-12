using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.ToolStates;

namespace Gum.Controls
{
    public partial class StateView : UserControl
    {
        public event EventHandler StateStackingModeChange;

        public StateStackingMode StateStackingMode
        {
            get
            {
                if (SingleStateRadio.Checked)
                {
                    return StateStackingMode.SingleState;
                }
                else
                {
                    return StateStackingMode.CombineStates;
                }
            }
            set
            {
                if (value == StateStackingMode.SingleState)
                {
                    SingleStateRadio.Checked = true;
                }
                else
                {
                    StackStateRadio.Checked = true;
                }
            }
        }

        public StateView()
        {
            InitializeComponent();
        }

        private void SingleStateRadio_CheckedChanged(object sender, EventArgs e)
        {
            StateStackingModeChange?.Invoke(this, null);
        }

        private void StackStateRadio_CheckedChanged(object sender, EventArgs e)
        {
            StateStackingModeChange?.Invoke(this, null);
        }
    }
}
