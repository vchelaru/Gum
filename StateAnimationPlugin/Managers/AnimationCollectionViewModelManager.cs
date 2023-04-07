using Gum.DataTypes;
using Gum.Logic.FileWatch;
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

        public ElementAnimationsViewModel GetAnimationCollectionViewModel(ElementSave element)
        {
            if(element == null)
            {
                return null;
            }
            else
            {
                var model = GetElementAnimationsSave(element);

                ElementAnimationsViewModel toReturn;
                if (model != null)
                {
                    toReturn = ElementAnimationsViewModel.FromSave(model, element);
                }
                else
                {
                    toReturn = new ElementAnimationsViewModel();

                }

                toReturn.Element = element;

                return toReturn;
            }

        }

        public ElementAnimationsSave GetElementAnimationsSave(ElementSave element)
        {
            ElementAnimationsSave model = null;
            var fileName = GetAbsoluteAnimationFileNameFor(element);


            if (FileManager.FileExists(fileName))
            {
                try
                {
                    model = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);

                }
                catch (Exception exception)
                {
                    OutputManager.Self.AddError(exception.ToString());
                }
            }

            return model;
        }

        public void Save(ElementAnimationsViewModel viewModel)
        {
            var currentElement = SelectedState.Self.SelectedElement;

            var fileName = GetAbsoluteAnimationFileNameFor(currentElement);

            if(fileName != null)
            {
                var save = viewModel.ToSave();

                FileWatchLogic.Self.IgnoreNextChangeOn(fileName);
                FileManager.XmlSerialize(save, fileName);
            }
        }

        public string GetAbsoluteAnimationFileNameFor(string elementName)
        {
            var fullPathXmlForElement = ElementSaveExtensionMethods.GetFullPathXmlFile(SelectedState.Self.SelectedElement, elementName);

            if (fullPathXmlForElement == null)
            {
                return null;
            }
            else
            {
                var absoluteFileName = fullPathXmlForElement.RemoveExtension() + "Animations.ganx";

                return absoluteFileName;
            }
        }

        public string GetAbsoluteAnimationFileNameFor(ElementSave elementSave)
        {
            var fullPathXmlForElement = elementSave.GetFullPathXmlFile();

            if (fullPathXmlForElement == null)
            {
                return null;
            }
            else
            {
                var absoluteFileName = fullPathXmlForElement.RemoveExtension() + "Animations.ganx";

                return absoluteFileName;
            }
        }
    }
}
