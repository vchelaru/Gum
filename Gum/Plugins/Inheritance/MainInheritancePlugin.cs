using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolCommands;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.Inheritance
{
    [Export(typeof(PluginBase))]
    public class MainInheritancePlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.InstanceAdd += HandleInstanceAdded;
            this.InstanceDelete += HandleInstanceDeleted;
            this.InstanceRename += HandleInstanceRenamed;
            this.InstanceReordered += HandleInstanceReordered;
            this.VariableSet += HandleVariableSet;
        }

        private void HandleInstanceAdded(ElementSave container, InstanceSave instance)
        {
            var elementsInheritingFromContainer = 
                ObjectFinder.Self.GetElementsInheritingFrom(container);

            foreach(var inheritingElement in elementsInheritingFromContainer)
            {
                var clone = instance.Clone();
                clone.DefinedByBase = true;
                clone.ParentContainer = inheritingElement;
                inheritingElement.Instances.Add(clone);

                // inheritingElement could be a derived of derived, in which case we
                // need to go just one up the inheritance tree:
                var directBase = ObjectFinder.Self.GetElementSave(inheritingElement.BaseType);

                AdjustInstance(directBase, inheritingElement, clone.Name);

                GumCommands.Self.FileCommands.TryAutoSaveElement(inheritingElement);
                GumCommands.Self.GuiCommands.RefreshElementTreeView(inheritingElement);
            }
        }

        private void HandleInstanceDeleted(ElementSave container, InstanceSave instance)
        {
            var elementsInheritingFromContainer =
                ObjectFinder.Self.GetElementsInheritingFrom(container);

            foreach(var inheritingElement in elementsInheritingFromContainer)
            {
                inheritingElement.Instances.RemoveAll(item => item.Name == instance.Name);

                GumCommands.Self.FileCommands.TryAutoSaveElement(inheritingElement);
                GumCommands.Self.GuiCommands.RefreshElementTreeView(inheritingElement);
            }
        }

        private void HandleInstanceRenamed(InstanceSave instance, string oldName)
        {
            var container = instance.ParentContainer;

            var elementsInheritingFromContainer =
                ObjectFinder.Self.GetElementsInheritingFrom(container);
            foreach (var inheritingElement in elementsInheritingFromContainer)
            {
                var toRename = inheritingElement.GetInstance(oldName);

                if(toRename != null)
                {
                    toRename.Name = instance.Name;
                    GumCommands.Self.FileCommands.TryAutoSaveElement(inheritingElement);
                    GumCommands.Self.GuiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }

        private void HandleVariableSet(ElementSave container, InstanceSave instance,
            string variableName, object oldValue)
        {

            if (variableName == "Base Type")
            {
                var elementsInheritingFromContainer =
                    ObjectFinder.Self.GetElementsInheritingFrom(container);

                foreach (var inheritingElement in elementsInheritingFromContainer)
                {
                    var toChange = inheritingElement.GetInstance(instance.Name);

                    if(toChange != null)
                    {
                        toChange.BaseType = instance.BaseType;

                        GumCommands.Self.FileCommands.TryAutoSaveElement(inheritingElement);
                    }

                }
            }
        }

        private void HandleInstanceReordered(InstanceSave instance)
        {
            var baseElement = instance.ParentContainer;

            var elementsInheritingFromContainer =
                    ObjectFinder.Self.GetElementsInheritingFrom(baseElement);

            foreach (var inheritingElement in elementsInheritingFromContainer)
            {
                var didShiftIndex = 
                    AdjustInstance(baseElement, inheritingElement, instance.Name);

                if(didShiftIndex)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveElement(inheritingElement);
                    GumCommands.Self.GuiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }

        private bool AdjustInstance(ElementSave baseElement, ElementSave derivedElement, string instanceName)
        {
            var instanceInBase = baseElement.GetInstance(instanceName);
            var instanceInDerived = derivedElement.GetInstance(instanceName);

            var indexInBase = baseElement.Instances.IndexOf(instanceInBase);
            string nameOfObjectBefore = null;
            if(indexInBase > 0)
            {
                nameOfObjectBefore = baseElement.Instances[indexInBase - 1].Name;
            }

            string nameOfObjectAfter = null;
            if(indexInBase < baseElement.Instances.Count-1)
            {
                nameOfObjectAfter = baseElement.Instances[indexInBase + 1].Name;
            }

            int exclusiveLowerIndexInDerived = -1;
            if(nameOfObjectBefore != null)
            {
                var instanceBefore = derivedElement.GetInstance(nameOfObjectBefore);
                exclusiveLowerIndexInDerived = derivedElement.Instances.IndexOf(instanceBefore);
            }

            int exclusiveUpperIndexInDerived = derivedElement.Instances.Count;

            if(nameOfObjectAfter != null)
            {
                var instanceAfter = derivedElement.GetInstance(nameOfObjectAfter);
                exclusiveUpperIndexInDerived = derivedElement.Instances.IndexOf(instanceAfter);
            }

            int currentDerivedIndex = derivedElement.Instances.IndexOf(instanceInDerived);

            var desiredIndex = System.Math.Min(currentDerivedIndex, exclusiveUpperIndexInDerived - 1);
            desiredIndex = System.Math.Max(desiredIndex, exclusiveLowerIndexInDerived + 1);

            bool didAdjust = false;
            if(desiredIndex != currentDerivedIndex)
            {
                didAdjust = true;
                derivedElement.Instances.Remove(instanceInDerived);
                if(currentDerivedIndex < desiredIndex)
                {
                    // we'll remove it from instances, which shifts everything above it up by one, so we have to -1 it
                    derivedElement.Instances.Insert(desiredIndex - 1, instanceInDerived);
                }
                else
                {
                    derivedElement.Instances.Insert(desiredIndex, instanceInDerived);
                }
            }

            return didAdjust;
        }

    }
}
