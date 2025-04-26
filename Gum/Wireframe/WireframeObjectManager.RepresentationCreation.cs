using System;
using System.Collections.Generic;
using System.Linq;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.PropertyGridHelpers.Converters;
using GumRuntime;
using Gum.Plugins;
using Gum.Services;

namespace Gum.Wireframe;

public partial class WireframeObjectManager
{
    #region Fields


    public string[] PositionAndSizeVariables = new string[]{
        "Width",
        "Height",
        "WidthUnits",
        "HeightUnits",
        "XOrigin",
        "YOrigin",
        "X",
        "Y",
        "XUnits",
        "YUnits",
        "Guide",
        "Parent"
    
    };

    public string[] ColorAndAlpha = new string[]{
        "Red",
        "Green",
        "Blue",
        "Alpha"
    };

    public string[] Color = new string[]{
        "Red",
        "Green",
        "Blue"
    };

    #endregion

    FontManager _fontManager;

    public WireframeObjectManager()
    {
        _fontManager = Builder.Get<FontManager>();
    }

    private static bool GetIfSelectedStateIsSetRecursively()
    {
        var category = SelectedState.Self.SelectedStateCategorySave;
        if(category != null)
        {
            var selectedElement = SelectedState.Self.SelectedElement;
            foreach(var behaviorReference in selectedElement.Behaviors)
            {
                var behavior = ObjectFinder.Self.GetBehavior(behaviorReference);

                if(behavior != null && behavior.Categories.Any(item => item.Name == category.Name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsRecursive(GraphicalUiElement item, HashSet<GraphicalUiElement> history)
    {
        if (history.Contains(item))
        {
            // recursion found!!!!!!
            return true;
        }
        history.Add(item);
        var parentGue = item.Parent as GraphicalUiElement;
        if (parentGue == null)
        {
            return false;
        }
        else
        {
            return IsRecursive(parentGue, history);
        }
    }

    private int GetDepth(GraphicalUiElement item, HashSet<GraphicalUiElement> history)
    {
        if(history.Contains(item))
        {
            return int.MaxValue / 2;
        }
        history.Add(item);
        var parentGue = item.Parent as GraphicalUiElement;
        if(parentGue == null)
        {
            return 0;
        }
        else
        {
            return 1 + GetDepth(parentGue, history);
        }
    }

    private GraphicalUiElement CreateRepresentationForInstance(InstanceSave instance, InstanceSave parentInstance, List<ElementWithState> elementStack, GraphicalUiElement container)
    {
        IPositionedSizedObject newIpso = null;

        GraphicalUiElement graphicalElement = newIpso as GraphicalUiElement;
        var baseElement = ObjectFinder.Self.GetElementSave(instance.BaseType);

        string type = instance.BaseType;

        var renderable = PluginManager.Self.CreateRenderableForType(type);

        if(renderable != null)
        {
            graphicalElement = new GraphicalUiElement(null, container);
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
            graphicalElement.SetContainedObject(renderable);
            graphicalElement.Tag = instance;
            graphicalElement.Name = instance.Name;
            graphicalElement.Component.Tag = instance;
        }


        else if (type == "Sprite" || type == "ColoredRectangle" || type == "NineSlice" || type == "Text" || type == "Circle" || type == "Rectangle")
        {
            graphicalElement = new GraphicalUiElement(null, container);
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(type);
            graphicalElement.CreateGraphicalComponent(instanceBase, null);
            graphicalElement.Tag = instance;
            graphicalElement.Name = instance.Name;
            graphicalElement.Component.Tag = instance;

            if (type == "Text")
            {
                (graphicalElement.RenderableComponent as Text).RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;
                if(SelectedState.Self.SelectedStateSave != null)
                {
                    var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                    if (instanceElement != null)
                    {
                        elementStack.Add(new ElementWithState(instanceElement));
                    }
                    var rfv = new RecursiveVariableFinder(elementStack);


                    var forcedValues = new StateSave();

                    void TryAddForced(string variableName)
                    {
                        var value = rfv.GetValueByBottomName(variableName);
                        if (value != null)
                        {
                            forcedValues.SetValue(variableName, value);
                        }
                    }

                    TryAddForced("Font");
                    TryAddForced("FontSize");
                    TryAddForced("OutlineThickness");
                    TryAddForced("UseFontSmoothing");
                    TryAddForced("IsItalic");
                    TryAddForced("IsBold");

                    StateSave stateSave = SelectedState.Self.SelectedStateSave;

                    // If the user has a category selected but no state in the category, then use the default:
                    if (stateSave == null && SelectedState.Self.SelectedStateCategorySave != null)
                    {
                        stateSave = SelectedState.Self.SelectedElement.DefaultState;
                    }



                    _fontManager.ReactToFontValueSet(instance, GumState.Self.ProjectState.GumProjectSave, stateSave, forcedValues);
                }
            }

        }
        else if (instance.IsComponent())
        {
            newIpso = CreateRepresentationsForInstanceFromComponent(instance, elementStack, parentInstance, container, ObjectFinder.Self.GetComponent(instance.BaseType));
        }
        else
        {
            // Make sure the base type is valid.
            // This could be null if a base type changed
            // its name but the derived wasn't updated, or 
            // if someone screwed with the XML files.  Who knows...

            if (baseElement != null)
            {
                graphicalElement = new GraphicalUiElement(null, container);

                CreateRectangleFor(instance, elementStack, graphicalElement);
            }
        }



        if (newIpso != null && (newIpso is GraphicalUiElement) == false)
        {
            graphicalElement = new GraphicalUiElement(newIpso as IRenderable, container);
        }
        else if (newIpso is GraphicalUiElement)
        {
            graphicalElement = newIpso as GraphicalUiElement;
        }

        if(graphicalElement != null)
        {
            if (baseElement != null)
            {
                graphicalElement.ElementSave = baseElement;
            }

            graphicalElement.AddExposedVariablesRecursively(baseElement);

            var selectedState = SelectedState.Self.SelectedStateSave;
            if (selectedState == null)
            {
                selectedState = SelectedState.Self.SelectedElement.DefaultState;
            }

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(selectedState);

            string guide = rvf.GetValue<string>("Guide");
            string parent = rvf.GetValue<string>(instance.Name + ".Parent");

            SetGuideParent(container, graphicalElement, guide);

            if (baseElement != null)
            {
                graphicalElement.AddStatesAndCategoriesRecursivelyToGue(baseElement);

                // for performance reasons, we'll suspend the layout here:
                graphicalElement.SuspendLayout();
                {
                    graphicalElement.SetVariablesRecursively(baseElement, baseElement.DefaultState);
                }
                graphicalElement.ResumeLayout();
            }
        }

        return graphicalElement;
    }

    private GraphicalUiElement CreateRepresentationsForInstanceFromComponent(InstanceSave instance, 
        List<ElementWithState> elementStack, InstanceSave parentInstance, GraphicalUiElement parentIpso, 
        ComponentSave baseComponentSave)
    {
        StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(instance);

        GraphicalUiElement rootIpso = null;
        if (ses != null)
        {
            rootIpso = new GraphicalUiElement(null, parentIpso);

            string type = ses.Name;

            var renderable = PluginManager.Self.CreateRenderableForType(type);

            if(renderable != null)
            {
                rootIpso.SetContainedObject(renderable);
                rootIpso.Tag = instance;
                rootIpso.Name = instance.Name;
                rootIpso.Component.Tag = instance;
            }

            else if (type == "Sprite" || type == "ColoredRectangle" || type == "NineSlice" || type == "Text" || type == "Circle" || type == "Rectangle")
            {
                ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
                rootIpso.CreateGraphicalComponent(instanceBase, null);
                rootIpso.Tag = instance;
                rootIpso.Name = instance.Name;
                rootIpso.Component.Tag = instance;

                if(type == "Text")
                {
                    (rootIpso.RenderableComponent as Text).RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;
                }

            }
            else
            {
                // give the plugin manager a shot at it:

                CreateRectangleFor(instance, elementStack, rootIpso);
            }

            var selectedState = SelectedState.Self.SelectedStateSave;

            if (selectedState == null)
            {
                selectedState = SelectedState.Self.SelectedElement.DefaultState;
            }

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(selectedState);

            string guide = rvf.GetValue<string>("Guide");
            SetGuideParent(parentIpso, rootIpso, guide);

            ElementWithState elementWithStateForNewInstance = new ElementWithState(baseComponentSave);
            var tempRvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);
            var state = tempRvf.GetValue("State") as string;
            elementWithStateForNewInstance.StateName = state;

            foreach (var category in baseComponentSave.Categories)
            {
                elementWithStateForNewInstance.CategorizedStates.Add(category.Name, tempRvf.GetValue<string>(category.Name + "State"));
            }

            // This seems wrong - we shouldn't be setting the instance name on the elementState for the instance. It should be one above:
            //elementWithStateForNewInstance.InstanceName = instance.Name;
            elementStack.Last().InstanceName = instance.Name;

            // Does this element already exist?
            bool alreadyExists = elementStack.Any(item => item.Element == elementWithStateForNewInstance.Element);
            if(!alreadyExists)
            {
                elementStack.Add(elementWithStateForNewInstance);

                foreach (InstanceSave internalInstance in baseComponentSave.Instances)
                {
                    // let's make sure we don't recursively create the same instance causing a stack overflow:
                    elementWithStateForNewInstance.InstanceName = internalInstance.Name;

                    GraphicalUiElement createdIpso = CreateRepresentationForInstance(internalInstance, instance, elementStack, rootIpso);

                }
            }

            SetUpParentRelationship(rootIpso.ContainedElements, elementStack);

            elementStack.Remove(elementStack.FirstOrDefault(item => item.Element == baseComponentSave));
        }

        return rootIpso;
    }

    private void SetUpParentRelationship(IEnumerable<GraphicalUiElement> elements, List<ElementWithState> elementStack)
    {
        // Now that we have created all instances, we can establish parent relationships

        HashSet<IRenderable> recursiveHashSet = new HashSet<IRenderable>();

        foreach (GraphicalUiElement contained in elements)
        {
            if (contained.Tag is InstanceSave childInstanceSave)
            {
                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementStack);

                string parentName = rvf.GetValue<string>($"{childInstanceSave.Name}.Parent");

                if (!string.IsNullOrEmpty(parentName) && parentName != StandardElementsManager.ScreenBoundsName)
                {
                    IRenderableIpso newParent = elements.FirstOrDefault(item => item.Name == parentName);

                    // This may have bad XML so if it doesn't exist, then let's ignore this:
                    if (newParent != null)
                    {
                        recursiveHashSet.Clear();
                        GetAllParents(newParent, recursiveHashSet);
                        if(recursiveHashSet.Contains(contained))
                        {
                            // RECURSIVE!!!!
                        }
                        else
                        {
                            contained.Parent = newParent;
                        }

                    }
                }

                //var innerChildren = contained.Children.Select(item => item as GraphicalUiElement).ToArray();
                //if(innerChildren.Length > 0)
                //{
                //    SetUpParentRelationship(innerChildren, elementStack);
                //}


            }
        }
    }

    void GetAllParents(IRenderableIpso ipso, HashSet<IRenderable> toFill)
    {
        if(ipso.Parent != null)
        {
            toFill.Add(ipso.Parent);
            GetAllParents(ipso.Parent, toFill);
        }
    }
    
    private IPositionedSizedObject CreateRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, GraphicalUiElement graphicalUiElement)
    {
        ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
        graphicalUiElement.CreateGraphicalComponent(instanceBase, null);
        graphicalUiElement.Tag = instance;
        graphicalUiElement.Name = instance.Name;
        graphicalUiElement.Component.Tag = instance;
        
        return graphicalUiElement;
    }

    private void SetGuideParent(GraphicalUiElement parentIpso, GraphicalUiElement ipso, string guideName)
    {
        // I dont't think we want to do this anymore because it should be handled by the GraphicalUiElement
        if (parentIpso != null && (parentIpso.Tag == null || parentIpso.Tag is ScreenSave == false))
        {
            ipso.Parent = parentIpso;
        }
        
        // don't do this because it causes double render.
        //else if (setParentToBoundsIfNoGuide) 
        //{
        //    ipso.Parent = mWireframeControl.ScreenBounds;
        //}
    }
}
