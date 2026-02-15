using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using ToolsUtilities;

namespace Gum.Managers;

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
    /// <summary>
    /// The owner of the reference. This may be the owner of the StateSave that has a variable referending the type, 
    /// or the owner of the instance. If the element is an instance of the type, then this is the element that is referenced.
    /// </summary>
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

public interface IObjectFinder
{
    GumProjectSave? GumProjectSave { get; }
    ElementSave? GetElementSave(InstanceSave instance);
    ElementSave? GetElementSave(string elementName);
    List<ElementSave> GetBaseElements(ElementSave elementSave);
}

public class ObjectFinder : IObjectFinder
{
    #region Fields/Properties


    /// <summary>
    /// Provides quick access to Gum objects by name. Elements do not prefix their type
    /// so a Screen would be "MainScreen" rather than "Screens/MainScreen"
    /// </summary>
    Dictionary<string, ElementSave>? cachedDictionary;

    public static ObjectFinder Self { get; private set; } = new ObjectFinder();

    /// <summary>
    /// The currently-loaded GumProjectSave.
    /// </summary>
    public GumProjectSave? GumProjectSave
    {
        get;
        set;
    }

    #endregion

    #region Cache enable/disable

    private int cacheEnableCount;

    public void ForceResetCache()
    {
        cacheEnableCount = 0;
        EnableCache();
    }

    /// <summary>
    /// Enables caching of all elements in the project. If cache was previously disabled,
    /// then this creates the cache and holds it until DisableCache is called. If cache was
    /// already enabled, this increments the count. Any call to enable cache should be paired
    /// with a DisableCache call.
    /// </summary>
    public void EnableCache()
    {
        cacheEnableCount++;
        if (cacheEnableCount == 1)
        {
            cachedDictionary = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);

            var gumProject = GumProjectSave;

            // Although it's not valid, we want to prevent a dupe from breaking the plugin, so we
            // need to do ContainsKey checks
            if(gumProject != null)
            {
                foreach (var screen in gumProject.Screens)
                {
                    var name = screen.Name;
                    cachedDictionary[name] = screen;
                }

                foreach(var component in gumProject.Components)
                {
                    var name = component.Name;
                    cachedDictionary[name] = component;
                }

                foreach (var standard in gumProject.StandardElements)
                {
                    var name = standard.Name;
                    cachedDictionary[name] = standard;
                }
            }

        }
    }

    /// <summary>
    /// Disables the in-memory cache for dictionary lookups, releasing any cached data if no other cache enables
    /// remain active. This should be called every time EnableCache is called.
    /// </summary>
    /// <remarks>If multiple cache enable calls have been made, the cache will only be disabled after
    /// all corresponding disables have been called. Once disabled, subsequent lookups will not use cached data
    /// until the cache is re-enabled.</remarks>
    public void DisableCache()
    {
        if(cacheEnableCount > 0)
        {
            cacheEnableCount--;
            if (cacheEnableCount == 0)
            {
                cachedDictionary = null;
            }
        }
        else
        {
            cachedDictionary = null;
        }
    }

    #endregion

    #region Get Element (Screen/Component/StandardElement)

    /// <summary>
    /// Returns the ScreenSave with matching name in the current glue project. Case is ignored when making name comparisons
    /// </summary>
    /// <param name="screenName"></param>
    /// <returns></returns>
    public ScreenSave? GetScreen(string screenName)
    {
        if(cachedDictionary != null)
        {
            if (screenName != null && cachedDictionary.ContainsKey(screenName))
            {
                return cachedDictionary[screenName] as ScreenSave;
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

    public ComponentSave? GetComponent(string componentName)
    {
        if (cachedDictionary != null)
        {
            if (componentName != null && cachedDictionary.ContainsKey(componentName))
            {
                return cachedDictionary[componentName] as ComponentSave;
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

    public StandardElementSave? GetStandardElement(string? elementName)
    {
        if (elementName == null)
        {
            return null;
        }
        if (cachedDictionary != null)
        {
            if (elementName != null && cachedDictionary.ContainsKey(elementName))
            {
                return cachedDictionary[elementName] as StandardElementSave;
            }
        }
        else
        {
            GumProjectSave? gps = GumProjectSave;

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
    public ElementSave? GetElementSave(InstanceSave instance)
    {
        return GetElementSave(instance.BaseType);
    }

    /// <summary>
    /// Returns the ElementSave (Screen, Component, or Standard Element) for the argument elementName. The name should be relative to its directory, so
    /// if looking for MainMenu, then this should pass "MainMenu" and not "Screens/MainMenu"
    /// </summary>
    /// <param name="elementName">The name of the ElementSave to search for</param>
    /// <returns>The matching ElementSave, or null if none is found</returns>
    public ElementSave? GetElementSave(string? elementName)
    {
        if(elementName == null)
        {
            return null;
        }
        if(cachedDictionary != null)
        {
            if(elementName != null && cachedDictionary.ContainsKey(elementName))
            {
                return cachedDictionary[elementName];
            }
        }
        else
        {
            var screenSave = GetScreen(elementName);
            if (screenSave != null)
            {
                return screenSave;
            }

            var componentSave = GetComponent(elementName);
            if (componentSave != null)
            {
                return componentSave;
            }

            var standardElementSave = GetStandardElement(elementName);
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
    /// <param name="list">An optional list to fill. If null, a new list will be created. This can help reduce allocation if this method is called frequently.</param>
    /// <param name="foundInstances">An optional list of InstanceSaves to fill with the instances that reference the argument elementSave. If null, instances will not be recorded.</param>
    /// <returns>A List containing all Elements</returns>
    public List<ElementSave> GetElementsReferencing(ElementSave elementSave, List<ElementSave>? list = null, List<InstanceSave>? foundInstances = null)
    {
        System.Diagnostics.Debug.Assert(this.GumProjectSave != null, "GumProjectSave cannot be null when calling GetElementsReferencing");
        if(elementSave == null)
        {
            throw new ArgumentNullException(nameof(elementSave));
        }
        if (list == null)
        {
            list = new List<ElementSave>();
        }

        foreach (ElementSave screen in this.GumProjectSave.Screens)
        {
            foreach (InstanceSave instanceSave in screen.Instances)
            {
                ElementSave? elementForInstance = this.GetElementSave(instanceSave.BaseType);

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

    public BehaviorSave GetBehaviorContainerOf(InstanceSave instance)
    {
        if (GumProjectSave != null)
        {
            foreach (var behavior in GumProjectSave.Behaviors)
            {
                if (behavior.RequiredInstances.Contains(instance))
                {
                    return behavior;
                }
            }
        }
        return null;
    }

    public ElementSave GetElementContainerOf(InstanceSave instanceSave)
    {
        if (GumProjectSave != null)
        {
            foreach (var screen in GumProjectSave.Screens)
            {
                if (screen.Instances.Contains(instanceSave))
                {
                    return screen;
                }
            }
            foreach (var component in GumProjectSave.Components)
            {
                if (component.Instances.Contains(instanceSave))
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

    [Obsolete("GetElementContainerOf to clearly indicate that the method does not return behaviors. ")]
    public ElementSave? GetContainerOf(InstanceSave instance) =>
        GetElementContainerOf(instance);

    public ElementSave? GetContainerOf(VariableSave variable)
    {
        foreach(var element in GumProjectSave.AllElements)
        {
            foreach(var state in element.AllStates)
            {
                if (state.Variables.Contains(variable))
                {
                    return element;
                }
            }
        }
        return null;
    }

    public ElementSave? GetElementContainerOf(VariableListSave variableList)
    {
        foreach (var element in GumProjectSave.AllElements)
        {
            foreach (var state in element.AllStates)
            {
                if (state.VariableLists.Contains(variableList))
                {
                    return element;
                }
            }
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
        if(element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

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

    #endregion

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

    [Obsolete("Use FillListWithReferencedFilePaths")]
    private void FillListWithReferencedFiles(List<string> absoluteFiles, ElementSave element)
    {
        List<FilePath> files = new List<FilePath>();

        FillWithReferencedFilePaths(files, element);

        absoluteFiles.AddRange(files.Select(item => item.StandardizedCaseSensitive));
    }

    private void FillWithReferencedFilePaths(List<FilePath> absoluteFiles, ElementSave element)
    { 
        RecursiveVariableFinder rvf;
        string value;

        foreach (var state in element.AllStates)
        {
            rvf = new RecursiveVariableFinder(element.DefaultState);

            value = rvf.GetValue<string>("SourceFile");
            if (!string.IsNullOrEmpty(value))
            {
                absoluteFiles.Add(value);
            }

            value = rvf.GetValue<string>("CustomFontFile");
            if (!string.IsNullOrEmpty(value))
            {
                absoluteFiles.Add(value);
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
                string gumProjectDirectory = FileManager.GetDirectory(GumProjectSave.FullFileName);
                if(instanceElement != null)
                {
                    var prefix = gumProjectDirectory;
                    if(instanceElement is ComponentSave)
                    {
                        prefix += "Components/";
                    }
                    else // standard element
                    {
                        prefix += "Standards/";
                    }
                    absoluteFiles.Add(prefix + instanceElement.Name + "." + instanceElement.FileExtension);
                }


                rvf = new RecursiveVariableFinder(instance, elementStack);

                value = rvf.GetValue<string>("SourceFile");
                if (!string.IsNullOrEmpty(value))
                {
                    absoluteFiles.Add(value);
                }

                if(rvf.GetValue("UseCustomFont") is bool asBool)
                {

                    if(asBool == true)
                    {
                        value = rvf.GetValue<string>("CustomFontFile");
                        if (!string.IsNullOrEmpty(value))
                        {
                            absoluteFiles.Add(value);
                        }
                    }
                    else
                    {
                        var fontSize = rvf.GetValue<int?>("FontSize");
                        var font = rvf.GetValue<string?>("Font");
                        var outlineThickness = rvf.GetValue<int?>("OutlineThickness");
                        var useFontSmoothing = rvf.GetValue<bool?>("UseFontSmoothing");
                        var isItalic = rvf.GetValue<bool?>("IsItalic");
                        var isBold = rvf.GetValue<bool?>("IsBold");

                        if(fontSize != null && font != null && outlineThickness != null && useFontSmoothing != null &&
                            isItalic != null && isBold != null)
                        {
                            string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                                fontSize.Value,
                                font,
                                outlineThickness.Value,
                                useFontSmoothing.Value,
                                isItalic.Value,
                                isBold.Value);

                            absoluteFiles.Add(
                                gumProjectDirectory + fontName);

                        }
                    }
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

    public string GetDefaultChildName(InstanceSave targetInstance, StateSave? stateSave = null)
    {
        string defaultChild = null;
        // check if the target instance is a ComponentSave. If so, use the RecursiveVariableFinder to get its DefaultChildContainer property
        var targetInstanceComponent = ObjectFinder.Self.GetComponent(targetInstance);
        if (targetInstanceComponent != null)
        {
            var instanceContainer = ObjectFinder.Self.GetContainerOf(targetInstance);

            var recursiveVariableFinder = new RecursiveVariableFinder(stateSave ?? instanceContainer?.DefaultState);
            defaultChild = recursiveVariableFinder.GetValue<string>($"{targetInstance.Name}.DefaultChildContainer");


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

    public BehaviorSave? GetBehavior(ElementBehaviorReference behaviorReference)
    {
        var behaviorName = behaviorReference.BehaviorName;
        if(behaviorName != null)
        {
            return GetBehavior(behaviorName);
        }
        else
        {
            return null;
        }
    }

    public BehaviorSave? GetBehavior(string behaviorName)
    {
        var behaviors = GumProjectSave!.Behaviors;

        foreach (var behavior in behaviors)
        {
            if (behavior.Name == behaviorName)
            {
                return behavior;
            }
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Returns a list of TypedElementReferences which include all items that reference the argument element.
    /// </summary>
    /// <param name="element">The argument element.</param>
    /// <returns>All referenced to this.</returns>
    public List<TypedElementReference> GetElementReferencesToThis(ElementSave element)
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

            foreach (var variable in screen.DefaultState.Variables.Where(item => item.GetRootName() == "ContainedType" || item.GetRootName() == "Contained Type"))
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

            foreach (var variable in component.DefaultState.Variables.Where(item => item.GetRootName() == "ContainedType" || item.GetRootName() == "Contained Type"))
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

    public List<ElementSave> GetElementsReferencedByThis(ElementSave elementSave)
    {
        Dictionary<string, ElementSave> elementNames = new();
        GetElementReferencesByThisInternal(elementSave, elementNames);

        return elementNames.Values.ToList();
    }

    private void GetElementReferencesByThisInternal(ElementSave elementSave, Dictionary<string, ElementSave> elementNames)
    {
        if (!string.IsNullOrEmpty(elementSave.BaseType) && elementNames.ContainsKey(elementSave.BaseType) == false)
        {
            elementNames.Add(elementSave.BaseType, null);
        }

        foreach (var instance in elementSave.Instances)
        {
            if(elementNames.ContainsKey(instance.BaseType) == false)
            {
                elementNames.Add(instance.BaseType, null);
            }
        }

        var names = elementNames.Keys.ToList();

        foreach (var name in names)
        {
            if (elementNames[name] == null)
            {
                var element = GetElementSave(name);

                elementNames[name] = element;

                if(element != null)
                {
                    GetElementReferencesByThisInternal(element, elementNames);
                }
            }
        }
    }

    /// <summary>
    /// Returns the root variable (defined on the standard element usually) for the argument instance.
    /// </summary>
    /// <param name="name">The name, including the period such as "InstanceName.X"</param>
    /// <param name="instance">The instance, which should match the instance in the variable name.</param>
    /// <returns>The root VariableSave</returns>
    public VariableSave? GetRootVariable(string name, InstanceSave instance)
    {
        // This could be referencing an invalid type
        var instanceElement = GetElementSave(instance.BaseType);
        var afterDot = name.Substring(name.IndexOf('.') + 1);
        if(instanceElement == null)
        {
            return null;
        }
        else
        {
            return GetRootVariable(afterDot, instanceElement);
        }
    }

    public VariableListSave GetRootVariableList(string name, ElementSave element)
    {
        var effectiveName = name;

        VariableListSave toReturn = null;

        if (effectiveName.Contains('.'))
        {
            var beforeDot = effectiveName.Substring(0, effectiveName.IndexOf('.'));
            var instance = element.GetInstance(beforeDot);

            if (instance != null)
            {
                return GetRootVariableList(effectiveName, instance);
            }
            // this has a dot, but no instance, so it's a bad variable...
        }
        else
        {
            var baseElement = GetElementSave(element.BaseType);

            if (baseElement != null)
            {
                toReturn = GetRootVariableList(effectiveName, baseElement);
            }
            else
            {
                toReturn = element.DefaultState.VariableLists.FirstOrDefault(item => item.Name == effectiveName);
            }
        }

        return toReturn;
    }

    public VariableListSave GetRootVariableList(string name, InstanceSave instance)
    {
        // This could be referencing an invalid type
        var instanceElement = GetElementSave(instance.BaseType);
        var afterDot = name.Substring(name.IndexOf('.') + 1);
        if (instanceElement == null)
        {
            return null;
        }
        else
        {
            return GetRootVariableList(afterDot, instanceElement);
        }
    }

    /// <summary>
    /// Returns the root variable (defined on the standard element usually) for the argument instance.
    /// </summary>
    /// <param name="name">Fully qualified variable name</param>
    /// <param name="element">The element containing the variable</param>
    /// <returns>The root variable if found</returns>
    public VariableSave? GetRootVariable(string name, ElementSave element)
    {
        var exposedVariable = element.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == name);

        var effectiveName = exposedVariable?.Name ?? name;

        VariableSave? toReturn = null;

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
            // Is this a custom variable?
            var customVariable = element.DefaultState.Variables.FirstOrDefault(item => item.Name == name && item.IsCustomVariable);

            if(customVariable != null)
            {
                toReturn = customVariable;
            }
            else
            {
                var baseElement = GetElementSave(element.BaseType);

                // Give the base a chance first since we want the root-most
                if(baseElement != null)
                {
                    toReturn = GetRootVariable(effectiveName, baseElement);
                }
                
                if(toReturn == null)
                {
                    toReturn = element.DefaultState.Variables.FirstOrDefault(item => item.Name == effectiveName);
                }
            }
        }

        return toReturn;
    }

    public ElementSave? GetElementContainerOf(StateSave? stateSave)
    {
        if(stateSave == null) return null;

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

    public IStateContainer? GetStateContainerOf(StateSave? stateSave)
    {
        if (stateSave == null) return null;

        var element = GetElementContainerOf(stateSave);

        if(element != null)
        {
            return element;
        }
        else
        {
            if(GumProjectSave != null)
            {
                foreach(var behavior in GumProjectSave.Behaviors)
                {
                    if (behavior.AllStates.Contains(stateSave))
                    {
                        return behavior;
                    }
                }
            }
        }
        return null;
    }

    [Obsolete("GetElementContainerOf to clearly indicate that the method does return behaviors. ")]
    public ElementSave GetContainerOf(StateSave stateSave) => GetElementContainerOf(stateSave);

    public bool IsVariableOrphaned(VariableSave variable, StateSave defaultState)
    {
        var container = GetElementContainerOf(defaultState);

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
                var variableRootName = variable.GetRootName();
                var foundVariable = instanceElement?.DefaultState.GetVariableRecursive(variableRootName);

                if(foundVariable == null)
                {
                    return true;
                }
                else
                {
                    return IsVariableOrphaned(foundVariable, instanceElement.DefaultState);
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

    public bool IsInstanceRecursivelyReferencingElement(InstanceSave instance, ElementSave element)
    {
        if(instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }
        if(element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if(instance.BaseType == element.Name)
        {
            return true;
        }
        else
        {
            var baseType = element.BaseType;

            var baseElement = GetElementSave(baseType);

            if(baseElement is StandardElementSave)
            {
                return false;
            }
            else if(baseType == null)
            {
                // this would be a screen...
                return false;
            }
            else
            {
                var baseElementSave = GetElementSave(baseType);
                if(baseElementSave != null)
                {
                    return IsInstanceRecursivelyReferencingElement(instance, baseElement);

                }
                else
                {
                    return false;
                }
            }
        }
    }
}




public static class InstanceExtensionMethods
{
    public static ElementSave? GetBaseElementSave(this InstanceSave instanceSave)
    {
        if (string.IsNullOrEmpty(instanceSave.BaseType))
        {
            // We tolerate instances with empty base types in behaviors...but should we?
            //throw new InvalidOperationException("The instance with the name " + instanceSave.Name + " doesn't have a BaseType");
            return null;
        }
        else
        {
            return ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
        }
    }

    public static InstanceSave GetParentInstance(this InstanceSave instanceSave)
    {
        var container = instanceSave.ParentContainer;
        if(container == null)
        {
            // do something with behaviors?
            return null;
        }
        else
        {
            var defaultState = container.DefaultState;
            var thisParentValue = defaultState.GetValueOrDefault<string>($"{instanceSave.Name}.Parent");

            return container.Instances.FirstOrDefault(item => item.Name == thisParentValue);

        }

    }

    public static List<InstanceSave> GetSiblingsIncludingThis(this InstanceSave thisInstance)
    {
        var container = thisInstance.ParentContainer;
        BehaviorSave? containerBehavior = null;

        StateSave? defaultState = null;
        if(container != null)
        {
            defaultState = container.DefaultState;


            List<InstanceSave> toReturn = new List<InstanceSave>();

            var rfv = new RecursiveVariableFinder(defaultState);

            var thisParentValueIgnoringInnerParents = rfv.GetValue($"{thisInstance.Name}.Parent") as string;

            if (thisParentValueIgnoringInnerParents?.Contains(".") == true)
            {
                thisParentValueIgnoringInnerParents =
                    thisParentValueIgnoringInnerParents.Substring(0, thisParentValueIgnoringInnerParents.IndexOf("."));
            }

            // Need to only consider the parent if it actually exists (also done below)
            if (container.GetInstance(thisParentValueIgnoringInnerParents) == null)
            {
                thisParentValueIgnoringInnerParents = null;
            }

            foreach (var instance in container.Instances)
            {
                var parentVariableName = $"{instance.Name}.Parent";
                //var instanceParentVariable = defaultState.GetValueOrDefault<string>(parentVariableName);
                var instanceParentVariable = rfv.GetValue(parentVariableName) as string;
                if (instanceParentVariable?.Contains(".") == true)
                {
                    instanceParentVariable = instanceParentVariable.Substring(0, instanceParentVariable.IndexOf("."));
                }
                if (container.GetInstance(instanceParentVariable) == null)
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
        else
        {
            foreach(var behavior in ObjectFinder.Self.GumProjectSave.Behaviors)
            {
                if(behavior.RequiredInstances.Contains(thisInstance))
                {
                    containerBehavior = behavior;
                    break;
                }
            }

            if(containerBehavior != null)
            {
                return containerBehavior.RequiredInstances.ToList<InstanceSave>();
            }
            else
            {
                return new List<InstanceSave>() { thisInstance};
            }
        }
    }
}

public static class ObjectFinderExt
{
    public static bool IsProjectSaved(this ObjectFinder objectFinder) =>
        objectFinder.GumProjectSave is { FullFileName.Length: > 0 };
}