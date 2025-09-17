using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Gum.Plugins.Output
{
    [Export(typeof(PluginBase))]
    class MainOutputPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            InitializeOutputTextBox();

        }

        private void InitializeOutputTextBox()
        {
            var outputTextBox = new TextBox();

            // 
            // OutputTextBox
            // 
            outputTextBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            outputTextBox.VerticalAlignment = VerticalAlignment.Stretch;
            outputTextBox.VerticalContentAlignment = VerticalAlignment.Top;
            outputTextBox.IsReadOnly = true;
            outputTextBox.Name = "OutputTextBox";
            outputTextBox.TabIndex = 0;
            outputTextBox.Margin = new Thickness(4);
            outputTextBox.Text = "";
            outputTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            outputTextBox.TextWrapping = TextWrapping.Wrap;
            outputTextBox.Style = Application.Current.TryFindResource("Frb.Styles.Textbox.Readonly") as Style;

            PluginTab tab = _tabManager.AddControl(outputTextBox, "Output", TabLocation.RightBottom);
            tab.GotFocus += () => outputTextBox.ScrollToEnd();
            OutputManager.Self.Initialize(outputTextBox);
        }
    }
}
