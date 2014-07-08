using CommonFormsAndControls;
using Gum.ToolStates;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Fields


        DispatcherTimer mPlayTimer;

        #endregion


        #region Properties

        ElementAnimationsViewModel ViewModel
        {
            get
            {
                return DataContext as ElementAnimationsViewModel;
            }
        }


        #endregion


        public MainWindow()
        {
            InitializeComponent();

            InitializeTimer();

            UpdatePlayStopButton();
        }

        private void InitializeTimer()
        {
            int timerFrequencyMs = 50;
            mPlayTimer = new DispatcherTimer();
            mPlayTimer.Interval = new TimeSpan(0, 0, 0, 0, timerFrequencyMs);
            mPlayTimer.Tick += delegate
            {
                ViewModel.DisplayedAnimationTime += timerFrequencyMs / 1000.0;
            };

        }

        private void UpdatePlayStopButton()
        {
            string imageName = "PlayIcon";

            bool isPlaying = mPlayTimer.IsEnabled;
            if(isPlaying)
            {
                imageName = "StopIcon";
            }

            Assembly thisassembly = Assembly.GetExecutingAssembly();
            System.IO.Stream imageStream = thisassembly.GetManifestResourceStream("StateAnimationPlugin.Resources." + imageName + ".png");
            BitmapFrame bmp = BitmapFrame.Create(imageStream);
            this.ButtonImage.Source = bmp;
        }

        private void AddAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                throw new NullReferenceException("The ViewModel for this is invalid - set the DataContext on this view before showing it.");
            }

            string whyIsntValid = null;

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new animation name:";

                var dialogResult = tiw.ShowDialog();

                if(dialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    var newAnimation = new AnimationViewModel() { Name = tiw.Result };

                    this.ViewModel.Animations.Add(newAnimation);

                    this.ViewModel.SelectedAnimation = newAnimation;

                }
            }
        }

        private void AddStateButton_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel == null)
            {
                throw new NullReferenceException("The ViewModel for this is invalid - set the DataContext on this view before showing it.");
            }

            string whyIsntValid = GetWhyAddingTimedStateIsInvalid();

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);

            }
            else
            {
                ListBoxMessageBox lbmb = new ListBoxMessageBox();
                lbmb.RequiresSelection = true;
                lbmb.Message = "Select a state";

                foreach (var state in SelectedState.Self.SelectedElement.AllStates)
                {
                    lbmb.Items.Add(state.Name);
                }

                var dialogResult = lbmb.ShowDialog();

                if (dialogResult.HasValue && dialogResult.Value)
                {
                    var item = lbmb.SelectedItem;
                    
                    ViewModel.SelectedAnimation.States.Add(new AnimatedStateViewModel() { StateName = (string)item });

                    ViewModel.SelectedAnimation.States.BubbleSort();
                }
            }
        }

        private string GetWhyAddingTimedStateIsInvalid()
        {
            string whyIsntValid = null;

            if (ViewModel.SelectedAnimation == null)
            {
                whyIsntValid = "You must first select an Animation";
            }

            if (SelectedState.Self.SelectedScreen == null && SelectedState.Self.SelectedComponent == null)
            {
                whyIsntValid = "You must first select a Screen or Component";
            }
            return whyIsntValid;
        }

        private void HandleDeleteAnimationPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null)
            {
                this.ViewModel.Animations.Remove(this.ViewModel.SelectedAnimation);
                this.ViewModel.SelectedAnimation = null;
            }
        }

        private void HandleDeleteAnimatedStatePressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null && this.ViewModel.SelectedAnimation.SelectedState != null)
            {
                this.ViewModel.SelectedAnimation.States.Remove(this.ViewModel.SelectedAnimation.SelectedState);
                this.ViewModel.SelectedAnimation.SelectedState = null;
            }
        }

        private void HandlePlayStopClicked(object sender, RoutedEventArgs e)
        {
            mPlayTimer.IsEnabled = !mPlayTimer.IsEnabled;

            UpdatePlayStopButton();
        }


    }
}
