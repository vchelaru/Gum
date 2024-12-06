using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using ToolsUtilities;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for SubAnimationSelectionWindow.xaml
    /// </summary>
    public partial class SubAnimationSelectionWindow : INotifyPropertyChanged
    {
        #region Fields

        AnimationContainerViewModel mSelectedContainer;
        private readonly AnimationFilePathService _animationFilePathService;

        #endregion

        #region Properties

        public List<AnimationContainerViewModel> AnimationContainers
        {
            get;
            set;
        }

        public IEnumerable<AnimationViewModel> Animations
        {
            get
            {
                if(SelectedContainer != null)
                {
                    ElementSave elementSave;
                    var fileName = GetFileNameForSelectedContainerAnimations(out elementSave);


                    if (fileName?.Exists() == true)
                    {
                        ElementAnimationsSave save = null;

                        try
                        {
                            save = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName.FullPath);
                        }
                        catch (Exception exception)
                        {
                            OutputManager.Self.AddError(exception.ToString());

                        }

                        if (save != null)
                        {
                            foreach (var item in save.Animations)
                            {
                                AnimationViewModel toReturn = AnimationViewModel.FromSave(
                                    item, elementSave);

                                toReturn.Name = item.Name;
                                toReturn.ContainingInstance = SelectedContainer.InstanceSave;

                                bool shouldSkip = false;

                                // Right now we're just checking to make sure an animation doesn't
                                // reference itself, but that doesn't prevent A referending B referencing A
                                // Eventually we need a deeper reursive check.

                                // skip if...
                                shouldSkip =
                                    // we selected an animation that isn't on an instance (if it is, then
                                    // there is no chance of it being recursive)...
                                    SelectedContainer.InstanceSave == null &&
                                    // And there is something to exclude...
                                    AnimationToExclude != null &&
                                    // and the names match
                                    toReturn.Name == AnimationToExclude.Name;

                                if(!shouldSkip)
                                {
                                    yield return toReturn;
                                }
                            }
                        }
                        
                    }


                }

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public AnimationContainerViewModel SelectedContainer
        {
            get
            {
                return mSelectedContainer;
            }
            set
            {
                mSelectedContainer = value;

                OnPropertyChange("SelectedContainer");
                OnPropertyChange("Animations");
            }
        }

        public AnimationViewModel SelectedAnimation
        {
            get;
            set;
        }
        public AnimationViewModel AnimationToExclude { get; internal set; }

        #endregion

        #region Methods

        public SubAnimationSelectionWindow()
        {
            InitializeComponent();

            _animationFilePathService = new AnimationFilePathService();

            this.ContainersListBox.DataContext = this;
            this.AnimationsListBox.DataContext = this;

        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OnPropertyChange(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private FilePath GetFileNameForSelectedContainerAnimations(out ElementSave element)
        {
            FilePath fileName = null;
            if (SelectedContainer.InstanceSave == null)
            {
                element = SelectedContainer.ElementSave;

                // Get all animations on "this" container
                fileName =
                    _animationFilePathService.GetAbsoluteAnimationFileNameFor(SelectedContainer.ElementSave);
            }
            else
            {
                var instance = SelectedContainer.InstanceSave;

                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                element = instanceElement;

                if (instanceElement != null)
                {
                    fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(
                        instanceElement);

                }
            }
            return fileName;
        }

        #endregion


    }
}
