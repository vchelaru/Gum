using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.RenderingLibrary;
using Gum.Services;
using Gum.Services.Dialogs; 
using Gum.ToolStates;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color; 
using Matrix = System.Numerics.Matrix4x4; 
using Rectangle = System.Drawing.Rectangle; 

namespace Gum.Wireframe;

#region Enums

public enum InstanceFetchType
{
    InstanceInCurrentElement,
    DeepInstance
}

#endregion

public partial class WireframeObjectManager
{



    #region Properties

    public List<GraphicalUiElement> AllIpsos { get; private set; } = new List<GraphicalUiElement>();

    public ElementSave? ElementShowing
    {
        get;
        private set;
    }


    public GraphicalUiElement? RootGue
    {
        get;
        private set;
    }


    #endregion

    #region Constructor/Initialize


    private readonly FontManager _fontManager;
    private readonly ISelectedState _selectedState;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    GraphicalUiElementManager gueManager;
    private LocalizationManager _localizationManager;
    private readonly PluginManager _pluginManager;

    public WireframeObjectManager(FontManager fontManager,
        ISelectedState selectedState,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        LocalizationManager localizationManager, 
        PluginManager pluginManager)
    {
        _fontManager = fontManager;
        _selectedState = selectedState;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _localizationManager = localizationManager;
        _pluginManager = pluginManager;

        gueManager = new GraphicalUiElementManager();
    }

    // This method will eventually move up to the constructor, but we can't do it yet until we get rid of all Self usages
    // Update - self usages are gone, but not sure if these can be moved up, need to run some tests
    public void Initialize()
    {

        GraphicalUiElement.AreUpdatesAppliedWhenInvisible= true;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;

        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = HandleCreateGraphicalComponent;

    }

    private IRenderable HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
    {

        IRenderable? containedObject = null;

#if GUM
        containedObject =
            _pluginManager.CreateRenderableForType(type);
#endif

        return containedObject;
    }

    #endregion

    #region Activity

    public void Activity()
    {
        gueManager.Activity();

    }

    #endregion


    private void ClearAll()
    {
        foreach (var element in AllIpsos)
        {
            gueManager.Remove(element);

            element.RemoveFromManagers();
        }

        AllIpsos.Clear();
    }

    public void RefreshAll(bool forceLayout, bool forceReloadTextures = false)
    {
        ElementSave elementSave = null;

        // If mulitple elements are selected then we can't show them all, so act as if nothing is selected.
        if(_selectedState.SelectedElements.Count() == 1)
        {
            elementSave = _selectedState.SelectedElement;
        }


        RefreshAll(forceLayout, forceReloadTextures, elementSave);

        _pluginManager.WireframeRefreshed();
    }

    private void RefreshAll(bool forceLayout, bool forceReloadTextures, ElementSave elementSave)
    {
        bool shouldRecreateIpso = forceLayout || elementSave != ElementShowing;
        //bool shouldReloadTextures = forceReloadTextures || elementSave != ElementShowing;
        bool shouldReloadTextures =
            false;
            //forceReloadTextures || elementSave != ElementShowing;

        if (elementSave == null || elementSave.IsSourceFileMissing)
        {
            ClearAll();
            RootGue = null;
        }
        else if(elementSave is ComponentSave && string.IsNullOrEmpty(elementSave.BaseType))
        {
            ClearAll();
            RootGue = null;
            _guiCommands.PrintOutput($"Error - cannot create representation for Component {elementSave.Name} because its BaseType is not set.");
        }
        else if (forceLayout || forceReloadTextures)
        {
            ObjectFinder.Self.EnableCache();
            {
                ClearAll();

                if(forceReloadTextures)
                {
                    LoaderManager.Self.DisposeAndClear();
                    LoaderManager.Self.CacheTextures = false;
                }

                LoaderManager.Self.CacheTextures = true;


                GraphicalUiElement.IsAllLayoutSuspended = true;

                try
                {
                    RootGue = _pluginManager.CreateGraphicalUiElement(elementSave);

                    if(RootGue != null)
                    {
                        // Always set default first, then if the selected state is not the default, then apply that after:
                        RootGue.SetVariablesRecursively(elementSave, elementSave.DefaultState);
                        var selectedState = _selectedState.SelectedStateSave;
                        if(selectedState != null && selectedState != elementSave.DefaultState)
                        {
                            RootGue.ApplyState(selectedState);
                        }


                        AddAllIpsos(RootGue);
                    }

                }
                catch(Exception e)
                {
                    RootGue = null;
                    _guiCommands.PrintOutput(e.ToString());
                    _dialogService.ShowMessage($"Error loading {elementSave}. See output window for more details");
                }
                GraphicalUiElement.IsAllLayoutSuspended = false;

                if(RootGue != null)
                {
                    RootGue.UpdateFontRecursive();
                    RootGue.UpdateLayout();

                    gueManager.Add(RootGue);
                    // what about fonts?
                    // We recreate missing fonts on startup, so do we need to bother here?
                    // I'm not sure, but if we do we would call:
                    //FontManager.Self.CreateAllMissingFontFiles(ObjectFinder.Self.GumProjectSave);


                    if(_localizationManager.HasDatabase)
                    {
                        ApplyLocalization();
                    }
                }
            }
            ObjectFinder.Self.DisableCache();
        }
        ElementShowing = elementSave;

    }


    private void AddAllIpsos(GraphicalUiElement gue)
    {
        AllIpsos.Add(gue);

        var containedElements = gue.ContainedElements.ToHashSet();

        if (gue.Children != null)
        {
            AddChildrenRecursively(gue, containedElements);
        }
        else
        {
            foreach (var item in gue.ContainedElements)
            {
                if (item.Parent == null)
                {
                    AllIpsos.Add(item);
                    AddChildrenRecursively(item, containedElements);
                }
            }

        }
    }

    private void AddChildrenRecursively(GraphicalUiElement gue, HashSet<GraphicalUiElement> containedElements)
    {
        if (gue.Children == null) return;

        foreach(var childAsRenderable in gue.Children)
        {
            if(childAsRenderable is GraphicalUiElement child )
            {
                if(containedElements.Contains(childAsRenderable))
                {
                    AllIpsos.Add(child);
                }
                // don't put this in the if containedElements.Contains 
                // check because we can have children which are attached
                // to an item inside of component instnace using parent dot operator
                AddChildrenRecursively(child, containedElements);
            }

        }
        //AllIpsos.Add(rootGue);
        //foreach(var item in rootGue.ContainedElements)
        //{
        //    AllIpsos.Add(item);
        //}
    }

    public void ApplyLocalization()
    {
        if(_localizationManager.HasDatabase == false)
        {
            throw new InvalidOperationException("Cannot apply localization - the LocalizationManager doesn't have a localization database loaded");
        }

        var texts = GetTextsRecurisve(RootGue);

        foreach (var textContainer in texts)
        {
            ApplyLocalization(textContainer);
        }
    }


    public void ApplyLocalization(GraphicalUiElement gue, string forcedId = null)
    {
        var shouldLocalize = GumState.Self.ProjectState.GumProjectSave.ShowLocalizationInGum;
        //if(gue.Tag is InstanceSave instance)
        //{
        //    var rfv = new RecursiveVariableFinder(_selectedState.SelectedStateSave);

        //    var value = rfv.GetValue<bool>(instance.Name + ".Apply Localization");
        //    isLocalized = value;
        //}

        if(shouldLocalize)
        {
            var stringId = forcedId;
            if(string.IsNullOrWhiteSpace(stringId) && gue.RenderableComponent is IText asText)
            {
                stringId = asText.StoredMarkupText;
                if(string.IsNullOrEmpty(stringId))
                {
                    stringId =  asText.RawText;
                }

            }

            // Go through the GraphicalUiElement to kick off a layout adjustment if necessary
            gue.SetProperty("Text", _localizationManager.Translate(stringId));
        }
    }

    public IEnumerable<GraphicalUiElement> GetTextsRecurisve(GraphicalUiElement parent)
    {
        if(parent.RenderableComponent is IText)
        {
            yield return parent;
        }
        if(parent.Tag is ScreenSave)
        {
            // it won't have children, so go to the toplevel objects
            foreach(var child in parent.ContainedElements)
            {
                if(child.Parent == null || 
                    // new as of February 2025 children can now have parents
                    child.Parent == parent)
                {
                    var textsInChild = GetTextsRecurisve(child);
                    foreach(var textInChild in textsInChild)
                    {
                        yield return textInChild;
                    }

                }
            }
        }
        else if(parent.Children != null)
        {
            foreach(var child in parent.Children)
            {
                var textsInChild = GetTextsRecurisve(child as GraphicalUiElement);
                foreach (var textInChild in textsInChild)
                {
                    yield return textInChild;
                }
            }
        }
    }


    public GraphicalUiElement GetSelectedRepresentation()
    {
        if (_selectedState.SelectedIpso == null)
        {
            return null;
        }
        else if (_selectedState.SelectedInstance != null)
        {
            return GetRepresentation(_selectedState.SelectedInstance, _selectedState.GetTopLevelElementStack());
        }
        else if (_selectedState.SelectedElement != null)
        {
            return GetRepresentation(_selectedState.SelectedElement);
        }
        else
        {
            throw new Exception("The SelectionManager believes it has a selection, but there is no selected instance or element");
        }
    }

    public GraphicalUiElement[] GetSelectedRepresentations()
    {
        if (_selectedState.SelectedIpso == null)
        {
            return null;
        }
        else if(_selectedState.SelectedInstances.Count() > 0)
        {
            return _selectedState.SelectedInstances
                .Select(item => GetRepresentation(item, _selectedState.GetTopLevelElementStack()))
                .ToArray();
        }
        else if (_selectedState.SelectedElement != null)
        {
            return new GraphicalUiElement[]
            {
                GetRepresentation(_selectedState.SelectedElement)
            };
        }
        else
        {
            throw new Exception("The SelectionManager believes it has a selection, but there is no selected instance or element");
        }
    }

    /// <summary>
    /// Returns any element that has the argument element as its tag. This will not return
    /// instances which use this element as their base type.
    /// </summary>
    /// <param name="elementSave">The element to search for.</param>
    /// <returns>The matching representation, or null if one isn't found.</returns>
    public GraphicalUiElement GetRepresentation(ElementSave elementSave)
    {
#if DEBUG
        if (elementSave == null)
        {
            throw new NullReferenceException("The argument elementSave is null");
        }
#endif
        foreach (GraphicalUiElement ipso in AllIpsos)
        {
            if (ipso.Tag == elementSave)
            {
                return ipso;
            }
        }

        return null;
    }

    public GraphicalUiElement GetRepresentation(InstanceSave instanceSave, List<ElementWithState> elementStack = null)
    {
        if (instanceSave != null)
        {
            if (elementStack == null)
            {
                return AllIpsos.FirstOrDefault(item => item.Tag == instanceSave);
            }
            else
            {
                IEnumerable<IPositionedSizedObject> currentChildren = AllIpsos.Where(item => item.Parent == null);

                return AllIpsos.FirstOrDefault(item => item.Tag == instanceSave);
                        

            }
        }
        return null;
    }

    /// <summary>
    /// Returns the InstanceSave that uses this representation or the
    /// instance that has a a contained instance that uses this representation.
    /// </summary>
    /// <param name="representation">The representation in question.</param>
    /// <returns>The InstanceSave or null if one isn't found.</returns>
    public InstanceSave GetInstance(IRenderableIpso representation, InstanceFetchType fetchType, List<ElementWithState> elementStack)
    {
        ElementSave selectedElement = _selectedState.SelectedElement;

        string prefix = selectedElement.Name + ".";
        // Screens now have parents, so we no longer need to strip prefixes:
        //if (selectedElement is ScreenSave)
        //{
        //    prefix = "";
        //}

        return GetInstance(representation, selectedElement, prefix, fetchType, elementStack);
    }

    public InstanceSave GetInstance(IRenderableIpso representation, ElementSave instanceContainer, string prefix, InstanceFetchType fetchType, List<ElementWithState> elementStack)
    {
        if (instanceContainer == null)
        {
            return null;
        }

        InstanceSave toReturn = null;


        string qualifiedName = representation.GetAttachmentQualifiedName(elementStack);

        // strip off the guide name if it starts with a guide
        qualifiedName = StripGuideOrParentNameIfNecessaryName(qualifiedName, representation);


        foreach (InstanceSave instanceSave in instanceContainer.Instances)
        {
            if (prefix + instanceSave.Name == qualifiedName)
            {
                toReturn = instanceSave;
                break;
            }
        }

        if (toReturn == null)
        {
            foreach (InstanceSave instanceSave in instanceContainer.Instances)
            {
                ElementSave instanceElement = instanceSave.GetBaseElementSave();

                bool alreadyInStack = elementStack.Any(item => item.Element == instanceElement);

                if (!alreadyInStack)
                {
                    var elementWithState = new ElementWithState(instanceElement);

                    elementStack.Add(elementWithState);

                    toReturn = GetInstance(representation, instanceElement, prefix + instanceSave.Name + ".", fetchType, elementStack);

                    if (toReturn != null)
                    {
                        if (fetchType == InstanceFetchType.DeepInstance)
                        {
                            // toReturn will be toReturn, no need to do anything
                        }
                        else // fetchType == InstanceInCurrentElement
                        {
                            toReturn = instanceSave;
                        }
                        break;
                    }

                    elementStack.Remove(elementWithState);
                }
            }
        }

        return toReturn;
    }

    private string StripGuideOrParentNameIfNecessaryName(string qualifiedName, IRenderableIpso representation)
    {
        foreach (NamedRectangle rectangle in ObjectFinder.Self.GumProjectSave.Guides)
        {
            if (qualifiedName.StartsWith(rectangle.Name + "."))
            {
                return qualifiedName.Substring(rectangle.Name.Length + 1);
            }
        }

        if (representation.Parent != null && representation.Parent.Tag is InstanceSave && representation.Tag is InstanceSave)
        {
            // strip this off!
            if ((representation.Parent.Tag as InstanceSave).ParentContainer == (representation.Tag as InstanceSave).ParentContainer)
            {
                string whatToTakeOff = (representation.Parent.Tag as InstanceSave).Name + ".";

                int index = qualifiedName.IndexOf(whatToTakeOff);

                return qualifiedName.Replace(whatToTakeOff, "");

                //return qualifiedName.Substring((representation.Parent.Tag as InstanceSave).Name.Length + 1);
            }
        }

        return qualifiedName;
    }


    public bool IsRepresentation(IPositionedSizedObject ipso) => AllIpsos.Contains(ipso);

    public ElementSave GetElement(IPositionedSizedObject representation)
    {
        if (_selectedState.SelectedElement != null &&
            _selectedState.SelectedElement.Name == representation.Name)
        {
            return _selectedState.SelectedElement;
        }

        return null;
    }
}
