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

                    if (File.Exists(fileName))
                    {
                        try
                        {
                            var save = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);

                            return ElementAnimationsViewModel.FromSave(save, currentElement);
                        }
                        catch(Exception exception)
                        {
                            OutputManager.Self.AddError(exception.ToString());
                            ElementAnimationsViewModel toReturn = new ElementAnimationsViewModel();
                            return toReturn;

                        }
                    }
                    else
                    {
                        ElementAnimationsViewModel toReturn = new ElementAnimationsViewModel();

                        return toReturn;
                    }
                }

            }
        }

        public void Save(ElementAnimationsViewModel viewModel)
        {
            var currentElement = SelectedState.Self.SelectedElement;

            var fileName = GetAbsoluteAnimationFileNameFor(currentElement);

            var save = viewModel.ToSave();

            FileManager.XmlSerialize(save, fileName);
        }


        public string GetAbsoluteAnimationFileNameFor(ElementSave elementSave)
        {
            var fullPathXmlForElement = elementSave.GetFullPathXmlFile();

            var absoluteFileName = FileManager.RemoveExtension(fullPathXmlForElement) + "Animations.ganx";

            return absoluteFileName;
        }

        public ElementAnimationsSave GetElementAnimationsSave(ElementSave element)
        {
            string fileName = GetAbsoluteAnimationFileNameFor(element);

            return FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
        }
    }
}
