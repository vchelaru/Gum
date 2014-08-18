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

namespace StateAnimationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields

        ElementAnimationsViewModel mCurrentViewModel;

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
                MainWindow mainWindow = new MainWindow();

                ElementHost.EnableModelessKeyboardInterop(mainWindow);
                mainWindow.Show();

                mCurrentViewModel = AnimationCollectionViewModelManager.Self.CurrentAnimationCollectionViewModel;
                mCurrentViewModel.PropertyChanged += HandlePropertyChanged;

                mCurrentViewModel.AnyChange += HandleDataChange;

                mainWindow.DataContext = mCurrentViewModel;
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

            var value = mCurrentViewModel.DisplayedAnimationTime;

            var stateVmBefore = mCurrentViewModel.SelectedAnimation.States.LastOrDefault(item => item.Time <= value);
            var stateVmAfter = mCurrentViewModel.SelectedAnimation.States.FirstOrDefault(item => item.Time >= value);

            if (stateVmBefore == null && stateVmAfter != null)
            {
                SelectedState.Self.CustomCurrentStateSave = stateVmAfter.CachedCumulativeState;
                WireframeObjectManager.Self.RootGue.ApplyState(SelectedState.Self.CustomCurrentStateSave);
            }
            else if (stateVmBefore != null && stateVmAfter == null)
            {
                SelectedState.Self.CustomCurrentStateSave = stateVmBefore.CachedCumulativeState;
                WireframeObjectManager.Self.RootGue.ApplyState(SelectedState.Self.CustomCurrentStateSave);
            }
            else if (stateVmBefore != null && stateVmAfter != null)
            {
                if(stateVmAfter.CachedCumulativeState == null || 
                    stateVmAfter.CachedCumulativeState == null)
                {
                    if (mCurrentViewModel.SelectedAnimation != null && SelectedState.Self.SelectedElement != null)
                    {
                        mCurrentViewModel.SelectedAnimation.RefreshCombinedStates(SelectedState.Self.SelectedElement);
                    }
                }
                double linearRatio = GetLinearRatio(value, stateVmBefore, stateVmAfter);
                var stateBefore = stateVmBefore.CachedCumulativeState;
                var stateAfter = stateVmAfter.CachedCumulativeState;

                if (stateBefore != null && stateAfter != null)
                {
                    double processedRatio = ProcessRatio(stateVmBefore.InterpolationType, stateVmBefore.Easing, linearRatio);


                    var combined = stateBefore.Clone();
                    combined.MergeIntoThis(stateAfter, (float)processedRatio);

                    SelectedState.Self.CustomCurrentStateSave = combined;

                    // for performance we will only update wireframe:
                    //SelectedState.Self.UpdateToSelectedStateSave();
                    //WireframeObjectManager.Self.RefreshAll(true);
                    WireframeObjectManager.Self.RootGue.ApplyState(combined);
                }
            }
        }

        private double ProcessRatio(FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing, double linearRatio)
        {
            var interpolationFunction = Tweener.GetInterpolationFunction(interpolationType, easing);

            return interpolationFunction.Invoke((float)linearRatio, 0, 1, 1);
        }

        private static double GetLinearRatio(double value, AnimatedStateViewModel stateVmBefore, AnimatedStateViewModel stateVmAfter)
        {
            double valueBefore = stateVmBefore.Time;
            double valueAfter = stateVmAfter.Time;

            double range = valueAfter - valueBefore;
            double timeIn = value - valueBefore;

            double ratio = 0;

            if (valueAfter != valueBefore)
            {
                ratio = timeIn / range;
            }
            return ratio;
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

            if( sender is AnimatedStateViewModel)
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
