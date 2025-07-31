using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Linq;

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
                // This could be done to satisfy a missing dependency? Or perhaps a refactor by moving
                // a derived instance to base? Therefore, only see if there is not already an instance here
                var existingInInheriting = inheritingElement.GetInstance(instance.Name);
                if(existingInInheriting == null)
                {
                    var clone = instance.Clone();
                    clone.DefinedByBase = true;
                    clone.ParentContainer = inheritingElement;
                    inheritingElement.Instances.Add(clone);

                    // inheritingElement could be a derived of derived, in which case we
                    // need to go just one up the inheritance tree:
                    var directBase = ObjectFinder.Self.GetElementSave(inheritingElement.BaseType);

                    AdjustInstance(directBase, inheritingElement, clone.Name);

                    _fileCommands.TryAutoSaveElement(inheritingElement);
                    _guiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }

        private void HandleInstanceDeleted(ElementSave container, InstanceSave instance)
        {
            if(container != null)
            {
                var elementsInheritingFromContainer =
                    ObjectFinder.Self.GetElementsInheritingFrom(container);

                foreach(var inheritingElement in elementsInheritingFromContainer)
                {
                    inheritingElement.Instances.RemoveAll(item => item.Name == instance.Name);

                    _fileCommands.TryAutoSaveElement(inheritingElement);
                    _guiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }

        private void HandleInstanceRenamed(ElementSave container, InstanceSave instance, string oldName)
        {
            var elementsInheritingFromContainer =
                ObjectFinder.Self.GetElementsInheritingFrom(container);
            foreach (var inheritingElement in elementsInheritingFromContainer)
            {
                var toRename = inheritingElement.GetInstance(oldName);

                if(toRename != null)
                {
                    toRename.Name = instance.Name;
                    _fileCommands.TryAutoSaveElement(inheritingElement);
                    _guiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }

        private void HandleVariableSet(ElementSave container, InstanceSave instance,
            string variableName, object oldValue)
        {

            if (variableName == "BaseType" && container != null)
            {
                if(instance != null)
                {
                    HandleInstanceVariableBaseTypeSet(container, instance);
                }
                else
                {
                    HandleElementBaseType(container);
                }
            }
        }


        private void HandleInstanceVariableBaseTypeSet(ElementSave container, InstanceSave instance)
        {
            var elementsInheritingFromContainer =
                                    ObjectFinder.Self.GetElementsInheritingFrom(container);

            foreach (var inheritingElement in elementsInheritingFromContainer)
            {
                // Changed the base type on an instance, so find all instances in derived elements and change their base too
                var toChange = inheritingElement.GetInstance(instance.Name);

                if (toChange != null)
                {
                    toChange.BaseType = instance.BaseType;

                    _fileCommands.TryAutoSaveElement(inheritingElement);
                }

            }
        }

        private void HandleElementBaseType(ElementSave asElementSave)
        {
            string newValue = asElementSave.BaseType;

            // kill the old instances:
            asElementSave.Instances.RemoveAll(item => item.DefinedByBase);

            if(!string.IsNullOrEmpty(newValue))
            {
                if (StandardElementsManager.Self.IsDefaultType(newValue))
                {

                    StateSave defaultStateSave = StandardElementsManager.Self.GetDefaultStateFor(newValue);

                    // July 17, 2025
                    // Calling this method
                    // results in all of the
                    // default values being assigned
                    // on this instance. Doing so overwrites
                    // the default values inherited from the base
                    // StandardElementSave. Instead, we should inherit
                    // from the base:
                    //asElementSave.Initialize(defaultStateSave);
                    //StandardElementsManagerGumTool.Self.FixCustomTypeConverters(asElementSave);
                }
                else
                {
                    var baseElement = ObjectFinder.Self.GetElementSave(asElementSave.BaseType);

                    StateSave stateSave = new StateSave();
                    if (baseElement != null)
                    {
                        // This copies the values to this explicitly, which we don't want
                        //FillWithDefaultRecursively(baseElement, stateSave);

                        foreach (var instance in baseElement.Instances)
                        {
                            var instanceName = instance.Name;
                            var alreadyExists = asElementSave.Instances.FirstOrDefault(item => item.Name == instanceName);
                            if (alreadyExists != null)
                            {
                                alreadyExists.DefinedByBase = true;
                            }
                            else
                            {
                                var derivedInstance = instance.Clone();
                                derivedInstance.DefinedByBase = true;
                                asElementSave.Instances.Add(derivedInstance);
                            }
                        }
                        asElementSave.Initialize(stateSave);
                        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(asElementSave);
                    }
                }
            }

            const bool fullRefresh = true;
            // since the type might change:
            _guiCommands.RefreshElementTreeView(asElementSave);
            _guiCommands.RefreshVariables(fullRefresh);
            _guiCommands.RefreshStateTreeView();
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
                    _fileCommands.TryAutoSaveElement(inheritingElement);
                    _guiCommands.RefreshElementTreeView(inheritingElement);
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
