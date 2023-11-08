using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using ToolsUtilities;

namespace Gum.Managers
{
    #region ReferenceType Enum
    public enum ReferenceType
    {
        InstanceOfType,
        ElementOfType,
        ContainedTypeInList,
        VariableReference
    }
    #endregion

    #region TypedElementReference

    public class TypedElementReference
    {
        public ElementSave OwnerOfReferencingObject { get; set; }
        public StateSave StateSave { get; set; }

        /// <summary>
        /// The object that is doing the referencing. This could be an InstanceSave, a VariableSave, a VariableListSave, or a BehaviorReference.
        /// </summary>
        public object ReferencingObject { get; set; }
        public ReferenceType ReferenceType { get; set; }

        public TypedElementReference(object referencingObject, ReferenceType referenceType)
        {
            ReferencingObject = referencingObject;
            ReferenceType = referenceType;
        }

        public override string ToString()
        {
            if(OwnerOfReferencingObject == null)
            {
                return $"{ReferenceType} {ReferencingObject}";
            }
            else
            {
                return $"{ReferenceType} {ReferencingObject} in {OwnerOfReferencingObject}";
            }
        }
    }

    #endregion

    public class ObjectFinder
    {
        #region Fields/Properties

        static ObjectFinder mObjectFinder;

        Dictionary<string, ElementSave> cachedDictionary;

        public static ObjectFinder Self
        {
            get
            {
                if (mObjectFinder == null)
                {
                    mObjectFinder = new ObjectFinder();
                }
                return mObjectFinder;
            }
        }

        public GumProjectSave GumProjectSave
        {
            get;
            set;
        }

        #endregion

        #region Cache enable/disable

        public void EnableCache()
        {
            cachedDictionary = new Dictionary<string, ElementSave>();

            var gumProject = GumProjectSave;

            // Although it's not valid, we want to prevent a dupe from breaking the plugin, so we
            // need to do ContainsKey checks

            foreach (var screen in gumProject.Screens)
            {
                var name = screen.Name.ToLowerInvariant();
                if(!cachedDictionary.ContainsKey(name))
                {
                    cachedDictionary.Add(name, screen);
                }
            }

            foreach(var component in gumProject.Components)
            {
                var name = component.Name.ToLowerInvariant();
                if (!cachedDictionary.ContainsKey(name))
                {
                    cachedDictionary.Add(name, component);
                }
            }

            foreach (var standard in gumProject.StandardElements)
            {
                var name = standard.Name.ToLowerInvariant();
                if (!cachedDictionary.ContainsKey(name))
                {
                    cachedDictionary.Add(name, standard);
                }
            }
        }

        public void DisableCache()
        {
            cachedDictionary = null;
        }

        #endregion

        #region Get Element (Screen/Component/StandardElement)

        /// <summary>
        /// Returns the ScreenSave with matching name in the current glue project. Case is ignored when making name comparisons
        /// </summary>
        /// <param name="screenName"></param>
        /// <returns></returns>
        public ScreenSave GetScreen(string screenName)
        {
            if(cachedDictionary != null)
            {
                var nameInvariant = screenName.ToLowerInvariant();
                if (nameInvariant != null && cachedDictionary.ContainsKey(nameInvariant))
                {
                    return cachedDictionary[nameInvariant] as ScreenSave;
                }
            }
            else
            {
                GumProjectSave gps = GumProjectSave;

                if (gps != null)
                {
                    foreach (ScreenSave screenSave in gps.Screens)
                    {
                        // Since the screen name may come from a file we want to ignore case:
                        //if (screenSave.Name == screenName)
                        if (screenSave.Name.Equals(screenName, StringComparison.OrdinalIgnoreCase))
                        {
                            return screenSave;
                        }
                    }

                }
            }

            return null;
        }

        public ComponentSave GetComponent(InstanceSave instance) => GetComponent(instance.BaseType);

        public ComponentSave GetComponent(string componentName)
        {
            if (cachedDictionary != null)
            {
                var nameInvariant = componentName.ToLowerInvariant();
                if (nameInvariant != null && cachedDictionary.ContainsKey(nameInvariant))
                {
                    return cachedDictionary[nameInvariant] as ComponentSave;
                }
            }
            else
            {
                GumProjectSave gps = GumProjectSave;

                if (gps != null)
                {
                    foreach (ComponentSave componentSave in gps.Components)
                    {
                        // Since the component name may come from a file name we want
                        // to ignore case:
                        //if (componentSave.Name == componentName)
                        if (componentSave.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                        {
                            return componentSave;
                        }
                    }

                }
            }

            return null;
        }

        public StandardElementSave GetStandardElement(string elementName)
        {
            if (cachedDictionary != null)
            {
                var nameInvariant = elementName.ToLowerInvariant();
                if (nameInvariant != null && cachedDictionary.ContainsKey(nameInvariant))
                {
                    return cachedDictionary[nameInvariant] as StandardElementSave;
                }
            }
            else
            {
                GumProjectSave gps = GumProjectSave;

                if (gps != null)
                {
                    foreach (StandardElementSave elementSave in gps.StandardElements)
                    {
                        if (elementSave.Name == elementName)
                        {
                            return elementSave;
                        }
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// Returns the ElementSave (Screen, Component, or Standard Element) for the argument instance
        /// </summary>
        /// <param name="instance">The instance to find the matching element for</param>
        /// <returns>The matching ElementSave, or null if none is found</returns>
        public ElementSave GetElementSave(InstanceSave instance)
        {
            return GetElementSave(instance.BaseType);
        }

        /// <summary>
        /// Returns the ElementSave (Screen, Component, or Standard Element) for the argument elementName
        /// </summary>
        /// <param name="elementName">The name of the ElementSave to search for</param>
        /// <returns>The matching ElementSave, or null if none is found</returns>
        public ElementSave GetElementSave(string elementName)
        {
            if(cachedDictionary != null)
            {
                var nameInvariant = elementName?.ToLowerInvariant();

                if(nameInvariant != null && cachedDictionary.ContainsKey(nameInvariant))
                {
                    return cachedDictionary[nameInvariant];
                }
            }
            else
            {
                ScreenSave screenSave = GetScreen(elementName);
                if (screenSave != null)
                {
                    return screenSave;
                }

                ComponentSave componentSave = GetComponent(elementName);
                if (componentSave != null)
                {
                    return componentSave;
                }

                StandardElementSave standardElementSave = GetStandardElement(elementName);
                if (standardElementSave != null)
                {
                    return standardElementSave;
                }

            }

            // If we got here there's nothing by the argument name
            return null;

        }

        /// <summary>
        /// Returns a list of Elements that include InstanceSaves that use the argument
        /// elementSave as their BaseType, or that use an ElementSave deriving from elementSave
        /// as their BaseType.
        /// </summary>
        /// <param name="elementSave">The ElementSave to search for.</param>
        /// <returns>A List containing all Elements</returns>
        public List<ElementSave> GetElementsReferencing(ElementSave elementSave, List<ElementSave> list = null, List<InstanceSave> foundInstances = null)
        {
            if (list == null)
            {
                list = new List<ElementSave>();
            }

            foreach (ElementSave screen in this.GumProjectSave.Screens)
            {
                foreach (InstanceSave instanceSave in screen.Instances)
                {
                    ElementSave elementForInstance = this.GetElementSave(instanceSave.BaseType);

                    if (elementForInstance != null && elementForInstance.IsOfType(elementSave.Name))
                    {
                        list.Add(screen);

                        // If we want a list of instances
                        // then we don't want to break on a
                        // found instance - we want to continue
                        // to find all of them.
                        if (foundInstances != null)
                        {
                            foundInstances.Add(instanceSave);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            foreach (ComponentSave component in this.GumProjectSave.Components)
            {
                foreach (InstanceSave instanceSave in component.Instances)
                {
                    ElementSave elementForInstance = this.GetElementSave(instanceSave.BaseType);
                    
                    if (elementForInstance != null && elementForInstance.IsOfType(elementSave.Name))
                    {
                        list.Add(component);

                        // If we want a list of instances
                        // then we don't want to break on a
                        // found instance - we want to continue
                        // to find all of them.
                        if (foundInstances != null)
                        {
                            foundInstances.Add(instanceSave);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }



            return list;
        }

        public List<ElementSave> GetElementsReferencing(string partialFileName)
        {
            partialFileName = partialFileName.ToLower().Replace("\\", "/");

            List<ElementSave> referencingElements = new List<ElementSave>();
            foreach(var item in GumProjectSave.Screens)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            foreach (var item in GumProjectSave.Components)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            foreach (var item in GumProjectSave.StandardElements)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            return referencingElements;
        }

        public List<ElementSave> GetElementsReferencingRecursively(ElementSave elementSave)
        {
            List<ElementSave> typesToGoThrough = new List<ElementSave>();
            List<ElementSave> toReturn = new List<ElementSave>();
            List<ElementSave> typesFoundOnLastPass = new List<ElementSave>();

            typesToGoThrough.Add(elementSave);

            while (typesToGoThrough.Count != 0)
            {
                typesFoundOnLastPass.Clear();

                GetElementsReferencing(typesToGoThrough[0], typesFoundOnLastPass);

                foreach (var type in typesFoundOnLastPass)
                {
                    if (!toReturn.Contains(type))
                    {

                        toReturn.Add(type);
                        typesToGoThrough.Add(type);
                    }
                }


                typesToGoThrough.RemoveAt(0);
            }

            return toReturn;
        }

        public ElementSave GetContainerOf(StateSaveCategory category)
        {
            if(GumProjectSave != null)
            {
                foreach(var screen in GumProjectSave.Screens)
                {
                    if(screen.Categories.Contains(category))
                    {
                        return screen;
                    }
                }
                foreach (var component in GumProjectSave.Components)
                {
                    if (component.Categories.Contains(category))
                    {
                        return component;
                    }
                }
                foreach (var standardElement in GumProjectSave.StandardElements)
                {
                    if (standardElement.Categories.Contains(category))
                    {
                        return standardElement;
                    }
                }
            }

            return null;
        }

        public ElementSave GetContainerOf(InstanceSave instance)
        {
            if(GumProjectSave != null)
            {
                foreach(var screen in GumProjectSave.Screens)
                {
                    if(screen.Instances.Contains(instance))
                    {
                        return screen;
                    }
                }
                foreach (var component in GumProjectSave.Components)
                {
                    if (component.Instances.Contains(instance))
                    {
                        return component;
                    }
                }
                // Standard elements don't have instances... for now?
                //foreach (var standardElement in GumProjectSave.StandardElements)
                //{
                //    if (standardElement.Instances.Contains(instance))
                //    {
                //        return standardElement;
                //    }
                //}
            }
            return null;
        }

        public List<ElementSave> GetElementsReferencing(BehaviorSave behavior)
        {
            List<ElementSave> referencingElements = new List<ElementSave>();
            foreach(var component in GumProjectSave.Components)
            {
                if(component.Behaviors.Any(item => item.BehaviorName == behavior.Name))
                {
                    referencingElements.Add(component);
                }
            }

            foreach (var screen in GumProjectSave.Screens)
            {
                if (screen.Behaviors.Any(item => item.BehaviorName == behavior.Name))
                {
                    referencingElements.Add(screen);
                }
            }


            return referencingElements;
        }


        #endregion

        #region Get Elements by inheritance

        public StandardElementSave GetRootStandardElementSave(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                return null;
            }

            if (elementSave is ScreenSave)
            {
                // This will be null at the time of this writing, but may change in the future so we'll leave it here to do a proper check.

                return ObjectFinder.Self.GetElementSave("Screen") as StandardElementSave;
            }
            while (!(elementSave is StandardElementSave) && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                elementSave = GetElementSave(elementSave.BaseType);
            }

            return elementSave as StandardElementSave;
        }

        public StandardElementSave GetRootStandardElementSave(InstanceSave instanceSave)
        {
            return GetRootStandardElementSave(instanceSave.GetBaseElementSave());
        }

        public ICollection<ElementSave> GetElementsInheritingFrom(ElementSave element)
        {
            List<ElementSave> toReturn = new List<ElementSave>();

            FillListWithElementsInheriting(element, toReturn);

            return toReturn;
        }

        private void FillListWithElementsInheriting(ElementSave element, List<ElementSave> listToAddTo)
        {
            if(element is ScreenSave)
            {
                var screensInheriting = GumProjectSave.Screens
                    .Where(item => item.BaseType == element.Name)
                    .ToArray();

                listToAddTo.AddRange(screensInheriting);

                foreach(var screen in screensInheriting)
                {
                    FillListWithElementsInheriting(screen, listToAddTo);
                }
            }
            else
            {
                var componentsInheriting = GumProjectSave.Components
                    .Where(item => item.BaseType == element.Name)
                    .ToArray();

                listToAddTo.AddRange(componentsInheriting);

                foreach(var component in componentsInheriting)
                {
                    FillListWithElementsInheriting(component, listToAddTo);
                }
            }

        }

        private void FillListWithBaseElements(ElementSave element, List<ElementSave> listToAddTo)
        {
            var baseElement = !string.IsNullOrWhiteSpace(element.BaseType) ? GetElementSave(element.BaseType) : null;

            if (baseElement != null)
            {
                listToAddTo.Add(baseElement);

                FillListWithBaseElements(baseElement, listToAddTo);
            }
            
        }

        #endregion


        /// <summary>
        /// Returns a list of ElementSaves inheriting from the argument elementSave, with the most derived first in the list, and the most base last in the list
        /// </summary>
        /// <param name="elementSave">The element for which to get the inheritance list.</param>
        /// <returns>The list, with the most derived (direct inheritance) first.</returns>
        public List<ElementSave> GetBaseElements(ElementSave elementSave)
        {
            var toReturn = new List<ElementSave>();

            FillListWithBaseElements(elementSave, toReturn);

            return toReturn;
        }

        #region Get Files

        public IEnumerable<string> GetAllFilesInProject()
        {
            List<string> toReturn = new List<string>();

            FillListWithReferencedFiles(toReturn, GumProjectSave.Screens);
            FillListWithReferencedFiles(toReturn, GumProjectSave.Components);
            FillListWithReferencedFiles(toReturn, GumProjectSave.StandardElements);

            return toReturn.Distinct();
        }

        public List<string> GetFilesReferencedBy(ElementSave element)
        {
            List<string> toReturn = new List<string>();

            FillListWithReferencedFiles(toReturn, element);

            return toReturn.Distinct().ToList();
        }

        private void FillListWithReferencedFiles<T>(List<string> files, IList<T> elements) where T : ElementSave
        {
            // These files are all relative to the project, so we don't have to worry
            // about making them absolute/relative again.  It should just work.
            foreach (ElementSave element in elements)
            {
                FillListWithReferencedFiles(files, element);
            }
        }

        private void FillListWithReferencedFiles(List<string> files, ElementSave element)
        {
            RecursiveVariableFinder rvf;
            string value;

            foreach (var state in element.AllStates)
            {
                rvf = new RecursiveVariableFinder(element.DefaultState);

                value = rvf.GetValue<string>("SourceFile");
                if (!string.IsNullOrEmpty(value))
                {
                    files.Add(value);
                }

                value = rvf.GetValue<string>("CustomFontFile");
                if (!string.IsNullOrEmpty(value))
                {
                    files.Add(value);
                }

                List<Gum.Wireframe.ElementWithState> elementStack = new List<Wireframe.ElementWithState>();
                var elementWithState = new Gum.Wireframe.ElementWithState(element);
                elementWithState.StateName = state.Name;
                elementStack.Add(elementWithState);

                foreach (InstanceSave instance in element.Instances)
                {
                    // August 5, 2018 - why are we not considering
                    // the file name of the element? Isn't that a referenced
                    // file?
                    var instanceElement = GetElementSave(instance);
                    if(instanceElement != null)
                    {
                        string prefix = FileManager.GetDirectory(GumProjectSave.FullFileName);
                        if(instanceElement is ComponentSave)
                        {
                            prefix += "Components/";
                        }
                        else // standard element
                        {
                            prefix += "Standards/";
                        }
                        files.Add(prefix + instanceElement.Name + "." + instanceElement.FileExtension);
                    }


                    rvf = new RecursiveVariableFinder(instance, elementStack);

                    value = rvf.GetValue<string>("SourceFile");
                    if (!string.IsNullOrEmpty(value))
                    {
                        files.Add(value);
                    }

                    value = rvf.GetValue<string>("CustomFontFile");
                    if (!string.IsNullOrEmpty(value))
                    {
                        files.Add(value);
                    }
                }
            }
        }

        #endregion

        #region Get Instance

        public InstanceSave GetInstanceRecursively(ElementSave element, string instanceName)
        {
            var strippedInstanceName = instanceName;
            if(strippedInstanceName.Contains("."))
            {
                strippedInstanceName = strippedInstanceName.Substring(0, strippedInstanceName.LastIndexOf('.'));
            }
            InstanceSave instance = null;
            instance = element.Instances.FirstOrDefault(item => item.Name == strippedInstanceName);
            if (instance != null && instanceName.Contains("."))
            {
                var instanceElement = GetElementSave(instance);

                if(instanceElement != null)
                {
                    var instanceNameAfterDot = instanceName.Substring(instanceName.LastIndexOf('.') + 1);
                    instance = GetInstanceRecursively(instanceElement, instanceNameAfterDot);
                }
            }
            return instance;
        }

        public string GetDefaultChildName(InstanceSave targetInstance, StateSave stateSave = null)
        {
            string defaultChild = null;
            // check if the target instance is a ComponentSave. If so, use the RecursiveVariableFinder to get its DefaultChildContainer property
            var targetInstanceComponent = ObjectFinder.Self.GetComponent(targetInstance);
            if (targetInstanceComponent != null)
            {
                var instanceContainer = ObjectFinder.Self.GetContainerOf(targetInstance);

                var recursiveVariableFinder = new RecursiveVariableFinder(stateSave ?? instanceContainer?.DefaultState);
                defaultChild = recursiveVariableFinder.GetValue<string>($"{targetInstance.Name}.{nameof(ComponentSave.DefaultChildContainer)}");

                if (defaultChild != null)
                {
                    var instanceInComponent = targetInstanceComponent.GetInstance(defaultChild);

                    if (instanceInComponent != null)
                    {
                        var innerChild = GetDefaultChildName(instanceInComponent, targetInstanceComponent.DefaultState);

                        if (!string.IsNullOrEmpty(innerChild))
                        {
                            defaultChild += "." + innerChild;
                        }
                    }
                }

            }

            return defaultChild;
        }

        #endregion

        #region Get BehaviorSave

        public BehaviorSave GetBehavior(ElementBehaviorReference behaviorReference)
        {
            var behaviors = GumProjectSave.Behaviors;

            foreach(var behavior in behaviors)
            {
                if(behavior.Name == behaviorReference.BehaviorName)
                {
                    return behavior;
                }
            }

            return null;
        }

        #endregion

        public List<TypedElementReference> GetElementReferences(ElementSave element)
        {
            var elementName = element.Name;
            var prefix =
                element is ScreenSave ? "Screens/" :
                element is ComponentSave ? "Components/" :
                "Standards/";

            var elementQualifiedName = prefix + elementName;

            List<TypedElementReference> references = new List<TypedElementReference>();
            foreach (var screen in GumProjectSave.Screens)
            {
                foreach (var instanceInScreen in screen.Instances)
                {
                    if (instanceInScreen.BaseType == elementName)
                    {
                        references.Add(new TypedElementReference(instanceInScreen, ReferenceType.InstanceOfType)
                        {
                            OwnerOfReferencingObject = screen
                        });
                    }
                }

                foreach (var variable in screen.DefaultState.Variables.Where(item => item.GetRootName() == "Contained Type"))
                {
                    if (variable.Value as string == elementName)
                    {
                        references.Add(new TypedElementReference(variable, ReferenceType.ContainedTypeInList)
                        {
                            OwnerOfReferencingObject = screen
                        });
                    }
                }

                AddVariableReferences(screen);
            }

            foreach (var component in GumProjectSave.Components)
            {
                if (component.BaseType == elementName)
                {
                    references.Add(new TypedElementReference(component, ReferenceType.ElementOfType));
                }

                foreach (var instanceInScreen in component.Instances)
                {
                    if (instanceInScreen.BaseType == elementName)
                    {
                        references.Add(new TypedElementReference(instanceInScreen, ReferenceType.InstanceOfType)
                        {
                            OwnerOfReferencingObject = component
                        });
                    }
                }

                foreach (var variable in component.DefaultState.Variables.Where(item => item.GetRootName() == "Contained Type"))
                {
                    if (variable.Value as string == elementName)
                    {
                        references.Add(new TypedElementReference(variable, ReferenceType.ContainedTypeInList)
                        {
                            OwnerOfReferencingObject = component
                        });
                    }
                }

                AddVariableReferences(component);
            }

            foreach(var standard in GumProjectSave.StandardElements)
            {
                AddVariableReferences(standard);
            }

            void AddVariableReferences(ElementSave ownerElement)
            {
                foreach (var state in ownerElement.AllStates)
                {
                    foreach (var variableList in state.VariableLists)
                    {
                        if (variableList.GetRootName() == "VariableReferences")
                        {
                            foreach (string reference in variableList.ValueAsIList)
                            {
                                if (reference?.Contains("=") == true)
                                {
                                    var indexOfEquals = reference.IndexOf("=");
                                    var rightSide = reference.Substring(indexOfEquals + 1).Trim();
                                    if (rightSide.StartsWith(elementQualifiedName))
                                    {
                                        var newReference = new TypedElementReference(variableList, ReferenceType.VariableReference);
                                        newReference.OwnerOfReferencingObject = ownerElement;
                                        newReference.StateSave = state;
                                        references.Add(newReference);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return references;
        }

        /// <summary>
        /// Returns the root variable (defined on the standard element usually) for the argument instance.
        /// </summary>
        /// <param name="name">The name, including the period such as "InstanceName.X"</param>
        /// <param name="instance">The instance, which should match the instance in the variable name.</param>
        /// <returns>The root VariableSave</returns>
        public VariableSave GetRootVariable(string name, InstanceSave instance)
        {
            var instanceElement = GetElementSave(instance.BaseType);
            var afterDot = name.Substring(name.IndexOf('.') + 1);

            return GetRootVariable(afterDot, instanceElement);
        }

        public VariableSave GetRootVariable(string name, ElementSave element)
        {
            var exposedVariable = element.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == name);

            var effectiveName = exposedVariable?.Name ?? name;

            VariableSave toReturn = null;

            if(effectiveName.Contains('.'))
            {
                var beforeDot = effectiveName.Substring(0, effectiveName.IndexOf('.'));
                var instance = element.GetInstance(beforeDot);

                if(instance != null)
                {
                    return GetRootVariable(effectiveName, instance);
                }
                // this has a dot, but no instance, so it's a bad variable...
            }
            else
            {
                var baseElement = GetElementSave(element.BaseType);

                if(baseElement != null)
                {
                    toReturn = GetRootVariable(effectiveName, baseElement);
                }
                else
                {
                    toReturn = element.DefaultState.Variables.FirstOrDefault(item => item.Name == effectiveName);
                }
            }

            return toReturn;
        }

        public ElementSave GetContainerOf(StateSave stateSave)
        {
            if (GumProjectSave != null)
            {
                foreach (var screen in GumProjectSave.Screens)
                {
                    if (screen.AllStates.Contains(stateSave))
                    {
                        return screen;
                    }
                }
                foreach (var component in GumProjectSave.Components)
                {
                    if (component.AllStates.Contains(stateSave))
                    {
                        return component;
                    }
                }
                foreach (var standardElement in GumProjectSave.StandardElements)
                {
                    if (standardElement.AllStates.Contains(stateSave))
                    {
                        return standardElement;
                    }
                }
            }

            return null;
        }

        public bool IsVariableOrphaned(VariableSave variable, StateSave defaultState)
        {
            var container = GetContainerOf(defaultState);

            if(!string.IsNullOrEmpty(variable.SourceObject))
            {
                var instance = container.GetInstance(variable.SourceObject);
                if(instance == null)
                {
                    return true;
                }
                else
                {
                    var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                    variable = instanceElement?.DefaultState.GetVariableSave(variable.GetRootName());

                    if(variable == null)
                    {
                        return true;
                    }
                    else
                    {
                        return IsVariableOrphaned(variable, instanceElement.DefaultState);
                    }
                }
            }

            if(container is StandardElementSave)
            {
                // If it's a standard element, then check if the default state contains this variable name
                var standardelementDefault = StandardElementsManager.Self.GetDefaultStateFor(container.Name);
                return standardelementDefault.Variables.Any(item => item.Name == variable.Name) == false;
            }
            else
            {
                // See if this is an exposed variable that is not DefindByBase
                var exposedVariable = defaultState.Variables.FirstOrDefault(item =>
                    item.ExposedAsName == variable.Name);

                if(exposedVariable != null)
                {
                    return IsVariableOrphaned(exposedVariable, defaultState);
                }
                else
                {
                    var baseElement = ObjectFinder.Self.GetElementSave(container.BaseType);
                    if(baseElement == null)
                    {
                        return false;
                    }
                    else
                    {
                        return IsVariableOrphaned(variable, baseElement.DefaultState);
                    }
                }
            }
        }

    }




    public static class InstanceExtensionMethods
    {
        public static ElementSave GetBaseElementSave(this InstanceSave instanceSave)
        {
            if (string.IsNullOrEmpty(instanceSave.BaseType))
            {
                throw new InvalidOperationException("The instance with the name " + instanceSave.Name + " doesn't have a BaseType");
            }
            else
            {
                return ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            }
        }

        public static InstanceSave GetParentInstance(this InstanceSave instanceSave)
        {
            var container = instanceSave.ParentContainer;
            var defaultState = container.DefaultState;
            var thisParentValue = defaultState.GetValueOrDefault<string>($"{instanceSave.Name}.Parent");

            return container.Instances.FirstOrDefault(item => item.Name == thisParentValue);

        }

        public static List<InstanceSave> GetSiblingsIncludingThis(this InstanceSave thisInstance)
        {
            var container = thisInstance.ParentContainer;

            List<InstanceSave> toReturn = new List<InstanceSave>();

            var defaultState = container.DefaultState;
            var rfv = new RecursiveVariableFinder(defaultState);

            var thisParentValueIgnoringInnerParents = rfv.GetValue($"{thisInstance.Name}.Parent") as string;

            if(thisParentValueIgnoringInnerParents?.Contains(".") == true)
            {
                thisParentValueIgnoringInnerParents = 
                    thisParentValueIgnoringInnerParents.Substring(0, thisParentValueIgnoringInnerParents.IndexOf("."));
            }

            // Need to only consider the parent if it actually exists (also done below)
            if(container.GetInstance(thisParentValueIgnoringInnerParents) == null)
            {
                thisParentValueIgnoringInnerParents = null;
            }

            foreach (var instance in container.Instances)
            {
                var parentVariableName = $"{instance.Name}.Parent";
                //var instanceParentVariable = defaultState.GetValueOrDefault<string>(parentVariableName);
                var instanceParentVariable = rfv.GetValue(parentVariableName) as string;
                if(instanceParentVariable?.Contains(".") == true)
                {
                    instanceParentVariable = instanceParentVariable.Substring(0, instanceParentVariable.IndexOf("."));
                }
                if(container.GetInstance(instanceParentVariable) == null)
                {
                    instanceParentVariable = null;
                }

                if (thisParentValueIgnoringInnerParents == instanceParentVariable)
                {
                    toReturn.Add(instance);
                }
            }

            return toReturn;
        }
    }
}
