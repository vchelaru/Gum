using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using StateAnimationPlugin.SaveClasses;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    public class AnimationCollectionViewModelManager : Singleton<AnimationCollectionViewModelManager>
    {

        public ElementAnimationsViewModel CurrentAnimationCollectionViewModel
        {
        
            get
            {



                var currentElement = SelectedState.Self.SelectedElement;

                if(currentElement == null)
                {
                    return null;
                }
                else
                {
                    var fileName = GetAbsoluteAnimationFileNameFor(currentElement);

                    ElementAnimationsViewModel toReturn = null;

                    if (FileManager.FileExists(fileName))
                    {
                        try
                        {
                            var save = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);

                            toReturn = ElementAnimationsViewModel.FromSave(save, currentElement);
                        }
                        catch(Exception exception)
                        {
                            OutputManager.Self.AddError(exception.ToString());
                            toReturn = new ElementAnimationsViewModel();

                        }
                    }
                    else
                    {
                        toReturn = new ElementAnimationsViewModel();

                    }

                    toReturn.Element = currentElement;

                    return toReturn;
                }

            }
        }

        public void Save(ElementAnimationsViewModel viewModel)
        {
            var currentElement = SelectedState.Self.SelectedElement;

            var fileName = GetAbsoluteAnimationFileNameFor(currentElement);

            var save = viewModel.ToSave();

            FileWatchManager.Self.IgnoreNextChangeOn(fileName);
            FileManager.XmlSerialize(save, fileName);
        }

        public string GetAbsoluteAnimationFileNameFor(string elementName)
        {
            string fullPathXmlForElement = ElementSaveExtensionMethods.GetFullPathXmlFile(SelectedState.Self.SelectedElement, elementName);

            if (string.IsNullOrEmpty(fullPathXmlForElement))
            {
                return null;
            }
            else
            {
                var absoluteFileName = FileManager.RemoveExtension(fullPathXmlForElement) + "Animations.ganx";

                return absoluteFileName;
            }
        }

        public string GetAbsoluteAnimationFileNameFor(ElementSave elementSave)
        {
            var fullPathXmlForElement = elementSave.GetFullPathXmlFile();

            if (string.IsNullOrEmpty(fullPathXmlForElement))
            {
                return null;
            }
            else
            {
                var absoluteFileName = FileManager.RemoveExtension(fullPathXmlForElement) + "Animations.ganx";

                return absoluteFileName;
            }
        }

        public ElementAnimationsSave GetElementAnimationsSave(ElementSave element)
        {
            string fileName = GetAbsoluteAnimationFileNameFor(element);
            if (FileManager.FileExists(fileName))
            {
                return FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
            }
            else
            {
                return null;
            }
        }
    }
}
