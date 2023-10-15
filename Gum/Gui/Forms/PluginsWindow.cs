using System;
using System.Windows.Forms;
using Gum.Plugins;


namespace Gum.Gui.Forms
{
    public partial class PluginsWindow : Form
    {
        public PluginsWindow()
        {
            InitializeComponent();

            RefreshCheckBoxes();
        }


        private void RefreshCheckBoxes()
        {

            this.checkedListBox1.Items.Clear();

            foreach (PluginContainer pluginContainer in PluginManager.AllPluginContainers)
            {

                int index = checkedListBox1.Items.Add(pluginContainer);

                checkedListBox1.SetItemChecked(index, pluginContainer.IsEnabled);
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            PluginContainer container = checkedListBox1.Items[e.Index] as PluginContainer;
            bool isShuttingDown = e.CurrentValue == CheckState.Checked &&
                e.NewValue == CheckState.Unchecked;

            if (isShuttingDown)
            {
                if (PluginManager.ShutDownPlugin(container.Plugin, PluginShutDownReason.UserDisabled))
                {
                    e.NewValue = CheckState.Unchecked;
                }


            }
            else
            {
                bool shouldBeEnabled = e.NewValue == CheckState.Checked;

                if (shouldBeEnabled && !container.IsEnabled)
                {
                    DialogResult result = System.Windows.Forms.DialogResult.Yes;

                    if (!string.IsNullOrEmpty(container.FailureDetails))
                    {
                        result = MessageBox.Show("The plugin " + container.Name + " has crashed so " +
                            " it was disabled.  Are you sure you want to re-enable it?",
                            "Re-enable crashed plugin?",
                            MessageBoxButtons.YesNo);
                    }

                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {

                        container.IsEnabled = true;
                        try
                        {
                            container.Plugin.StartUp();
                            PluginManager.ReenablePlugin(container.Plugin);
                        }
                        catch (Exception exception)
                        {
                            container.Fail(exception, "Failed in StartUp");
                            RefreshCheckBoxes();
                        }
                    }
                    else
                    {
                        e.NewValue = CheckState.Unchecked;
                    }
                }
            }
        }
    }
}
