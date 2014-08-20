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
        AnimationContainerViewModel mSelectedContainer;

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
                    string fileName = null;
                    if(SelectedContainer.InstanceSave == null)
                    {
                        // Get all animations on "this" container
                        fileName = 
                            AnimationCollectionViewModelManager.Self.GetAbsoluteAnimationFileNameFor(SelectedContainer.ElementSave);
                    }
                    else
                    {
                        var instance = SelectedContainer.InstanceSave;

                        var instanceElement = ObjectFinder.Self.GetElementSave(instance);

                        if(instanceElement != null)
                        {
                            fileName = AnimationCollectionViewModelManager.Self.GetAbsoluteAnimationFileNameFor(
                                instanceElement);
                            
                        }
                    }

                    
                    if (!string.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
                    {
                        ElementAnimationsSave save = null;

                        try
                        {
                            save = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
                        }
                        catch (Exception exception)
                        {
                            OutputManager.Self.AddError(exception.ToString());

                        }

                        if (save != null)
                        {
                            foreach (var item in save.Animations)
                            {
                                yield return new AnimationViewModel()
                                {
                                    Name = item.Name
                                };
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

        public SubAnimationSelectionWindow()
        {
            InitializeComponent();

            AnimationContainers = new List<AnimationContainerViewModel>();

            var acvm = new AnimationContainerViewModel(
                SelectedState.Self.SelectedComponent, null
                );
            AnimationContainers.Add(acvm);

            foreach(var instance in SelectedState.Self.SelectedComponent.Instances)
            {
                acvm = new AnimationContainerViewModel(SelectedState.Self.SelectedComponent, instance);

                AnimationContainers.Add(acvm);
            }


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


    }
}
