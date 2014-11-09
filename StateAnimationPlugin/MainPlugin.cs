using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using StateAnimationPlugin.Views;
using StateAnimationPlugin.Managers;
using System.Windows.Forms.Integration;
using StateAnimationPlugin.ViewModels;
using Gum.ToolStates;
using System.Windows;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes; 

namespace StateAnimationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields

        ElementAnimationsViewModel mCurrentViewModel;

        MainWindow mMainWindow;

        #endregion

        #region Properties

        public override string FriendlyName
        {
            get { return "State Animation Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(0, 0, 0, 1); }
        }

        #endregion

        #region StartUp/ShutDown

        public override void StartUp()
        {
            CreateMenuItems();

            CreateEvents();
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }

        #endregion

        private void CreateEvents()
        {
            this.ElementSelected += delegate
            {
                RefreshViewModel();
            };

            this.InstanceSelected += delegate
            {
                RefreshViewModel();
            };

        }

        private void CreateMenuItems()
        {
            var menuItem = AddMenuItem(new List<string> { "State Animation", "View Animations" });

            menuItem.Click += HandleViewAnimationsClick;
        }

        private void HandleViewAnimationsClick(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedScreen == null && SelectedState.Self.SelectedElement == null)
            {
                MessageBox.Show("You need to select a Screen or Component first");
            }
            else
            {
                if(mMainWindow == null || mMainWindow.IsVisible == false)
                {
                    mMainWindow = new MainWindow();
                    ElementHost.EnableModelessKeyboardInterop(mMainWindow);
                }



                RefreshViewModel();
            }
        }

        private void RefreshViewModel()
        {
            ElementSave currentlyReferencedElement = null;
            if(mCurrentViewModel != null)
            {
                currentlyReferencedElement = mCurrentViewModel.Element;
            }

            if (currentlyReferencedElement != SelectedState.Self.SelectedElement)
            {
                mCurrentViewModel = AnimationCollectionViewModelManager.Self.CurrentAnimationCollectionViewModel;



                if (mCurrentViewModel != null)
                {
                    mCurrentViewModel.PropertyChanged += HandlePropertyChanged;
                    mCurrentViewModel.AnyChange += HandleDataChange;
                }

                if (mMainWindow != null)
                {
                    mMainWindow.DataContext = mCurrentViewModel;
                }
            }
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var variableName = e.PropertyName;

            if (sender is ElementAnimationsViewModel)
            {
                if(variableName == "DisplayedAnimationTime")
                {
                    SetWireframeStateFromDisplayedAnimTime();
                }
            }
        }

        private void SetWireframeStateFromDisplayedAnimTime()
        {
            //////////////////////// EARLY OUT
            if(mCurrentViewModel.SelectedAnimation == null)
            {
                return;
            }
            ////////////////////// END EARLY OUT

            var animationTime = mCurrentViewModel.DisplayedAnimationTime;

            mCurrentViewModel.SelectedAnimation.SetStateAtTime(animationTime, SelectedState.Self.SelectedElement, defaultIfNull:true);
        }


        private void HandleDataChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var variableName = e.PropertyName;

            bool shouldSave = true;

            if(sender is ElementAnimationsViewModel)
            {
                if(variableName == "SelectedAnimation")
                {
                    shouldSave = false;
                }
                else if(variableName == "DisplayedAnimationTime")
                {
                    shouldSave = false;
                }
            }

            if (sender is AnimationViewModel)
            {
                if(variableName == "SelectedState")
                {
                    shouldSave = false;
                }
            }

            if( sender is AnimatedKeyframeViewModel)
            {
                if(variableName == "DisplayString")
                {
                    shouldSave = false;
                }
            }

            if (shouldSave)
            {
                AnimationCollectionViewModelManager.Self.Save(mCurrentViewModel);
            }
        }




    }
}
