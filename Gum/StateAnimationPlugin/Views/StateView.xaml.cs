using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for StateView.xaml
    /// </summary>
    public partial class StateView : UserControl
    {
        public StateView()
        {
            InitializeComponent();

            PopulateComboBoxes();
        }



        private void PopulateComboBoxes()
        {
            var interpolationValues = Enum.GetValues(typeof(FlatRedBall.Glue.StateInterpolation.InterpolationType));
            foreach (var value in interpolationValues)
            {
                InterpolationTypeComboBox.Items.Add(value);
            }

            var easingValues = Enum.GetValues(typeof(FlatRedBall.Glue.StateInterpolation.Easing));
            foreach (var value in easingValues)
            {
                EasingTypeComboBox.Items.Add(value);
            }

        }

         //Next step:  Make animations vs. state interpolations visibly different in the tree view

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // your event handler here
                e.Handled = true;

                // Make it lose and regain focus to apply the databainding
                FocusableTextBox.Focus();

                //InterpolationTypeComboBox.Focus();
                TimeTextBox.Focus();
            }
        }
    }
}
