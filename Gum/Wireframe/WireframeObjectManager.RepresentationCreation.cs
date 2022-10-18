using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using RenderingLibrary.Content;
using Gum.DataTypes.Variables;
using RenderingLibrary.Math.Geometry;
using Gum.DataTypes;
using Gum.RenderingLibrary;
using Gum.ToolStates;
using RenderingLibrary.Graphics.Fonts;
using System.Collections;
using ToolsUtilities;
using Microsoft.Xna.Framework;
using Gum.Converters;
using Gum.PropertyGridHelpers.Converters;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using Gum.Plugins;

namespace Gum.Wireframe
{
    public partial class WireframeObjectManager
    {

        #region Fields


        public string[] PositionAndSizeVariables = new string[]{
            "Width",
            "Height",
            "Width Units",
            "Height Units",
            "X Origin",
            "Y Origin",
            "X",
            "Y",
            "X Units",
            "Y Units",
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

        private GraphicalUiElement CreateIpsoForElement(ElementSave elementSave)
        {
            GraphicalUiElement rootIpso = null;
            bool isScreen = elementSave is ScreenSave;
            rootIpso = new GraphicalUiElement();

            
            // If we don't turn off layouts, layouts can be called tens of thousands of times for
            // complex elements. We'll turn off layouts, then turn them back on at the end and manually
            // layout:
            GraphicalUiElement.IsAllLayoutSuspended = true;
            {

                rootIpso.Tag = elementSave;
                AllIpsos.Add(rootIpso);

                rootIpso.ElementSave = elementSave;
                if (isScreen == false)
                {
                    // We used to not add the IPSO for the root element to the list of graphical elements
                    // and this prevented selection.  I'm not sure if this was intentionally left out or not
                    // but I think it should be here
                    // July 18, 2022
                    // This is a duplicate
                    // add. It's also added
                    // above. Which should it
                    // be?
                    //AllIpsos.Add(rootIpso);

                    rootIpso.CreateGraphicalComponent(elementSave, null);

                    // can be null if the element save references a bad file
                    if(rootIpso.Component != null)
                    {
                        rootIpso.Name = elementSave.Name;
                        rootIpso.Component.Tag = elementSave;

                    }

                    RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(SelectedState.Self.SelectedStateSaveOrDefault);

                    string guide = rvf.GetValue<string>("Guide");
                    SetGuideParent(null, rootIpso, guide);
                }

                var exposedVariables = elementSave
                    .DefaultState
                    .Variables
                    .Where(item => 
                        !string.IsNullOrEmpty(item.ExposedAsName) 
                        // October 18, 2022
                        // Why do we only consider variables that
                        // have no SourceObject? This should consider
                        // all exposed variables:
                        //&& string.IsNullOrEmpty(item.SourceObject)
                        )
                    .ToArray();

                foreach (var exposedVariable in exposedVariables)
                {
                    rootIpso.AddExposedVariable(exposedVariable.ExposedAsName, exposedVariable.Name);
                }

                List<GraphicalUiElement> newlyAdded = new List<GraphicalUiElement>();
                List<ElementWithState> elementStack = new List<ElementWithState>();

                ElementWithState elementWithState = new ElementWithState(elementSave);
                if (elementSave == SelectedState.Self.SelectedElement)
                {
                    if (SelectedState.Self.SelectedStateSave != null)
                    {
                        elementWithState.StateName = SelectedState.Self.SelectedStateSave.Name;
                    }
                    else
                    {
                        elementWithState.StateName = "Default";
                    }
                }

                var baseElements = ObjectFinder.Self.GetBaseElements(elementSave);
                baseElements.Reverse();
                foreach (var baseElement in baseElements)
                {
                    elementStack.Add(baseElement);
                }
                elementStack.Add(elementWithState);

                // parallel screws up the ordering of objects, so we'll do it on the primary thread for now
                // and parallelize it later:
                //Parallel.ForEach(elementSave.Instances, instance =>
                foreach (var instance in elementSave.Instances)
                {
                    GraphicalUiElement child = CreateRepresentationForInstance(instance, null, elementStack, rootIpso);

                    if (child == null)
                    {
                        // This can occur
                        // if an instance references
                        // a component that doesn't exist.
                        // I don't think we need to do anything
                        // here.
                    }
                    else
                    {
                        newlyAdded.Add(child);
                        AllIpsos.Add(child);
                    }
                }

                SetUpParentRelationship(newlyAdded, elementStack);

                elementStack.Remove(elementStack.FirstOrDefault(item => item.Element == elementSave));

                rootIpso.SetStatesAndCategoriesRecursively(elementSave);

                // First we need to the default state (and do so recursively)
                try
                {
                    rootIpso.SetVariablesRecursively(elementSave, elementSave.DefaultState);
                }
                catch(Exception e)
                {
                    // this barfed, but we don't want to crash the tool
                    GumCommands.Self.GuiCommands.PrintOutput($"Error loading {elementSave}:\n{e.ToString()}");
                }
                // then we override it with the current state if one is set:
                if (SelectedState.Self.SelectedStateSave != elementSave.DefaultState && SelectedState.Self.SelectedStateSave != null)
                {
                    var state = SelectedState.Self.SelectedStateSave;
                    rootIpso.ApplyState(state);
                }

                if(elementSave is StandardElementSave && elementSave.Name == "Text" && SelectedState.Self.SelectedStateSave != null)
                {
                    // We created a new Text object, so let's try generating fonts for it:
                    FontManager.Self.ReactToFontValueSet(null);

                }

                // I think this has to be *after* we set varaibles because that's where clipping gets set
                if (rootIpso != null)
                {
                    gueManager.Add(rootIpso);

                    rootIpso.AddToManagers();

                }
            }
            GraphicalUiElement.IsAllLayoutSuspended = false;
            HashSet<GraphicalUiElement> hashSet = new HashSet<GraphicalUiElement>();
            var tempSorted = AllIpsos.OrderBy(item =>
            {
                hashSet.Clear();
                return GetDepth(item, hashSet);
            }).ToArray();

            foreach(var item in AllIpsos)
            {
                hashSet.Clear();
                var isRecursive = IsRecursive(item, hashSet);

                if(isRecursive)
                {
                    var message = $"Recursion found in {elementSave}:";

                    foreach(var recursiveItem in hashSet)
                    {
                        message += $"\n  {recursiveItem}  child of...";
                    }

                    message += $"\n back to {item}";


                    GumCommands.Self.GuiCommands.ShowMessage(message);
                }
            }

            AllIpsos.Clear();
            AllIpsos.AddRange(tempSorted);

            rootIpso.UpdateFontRecursive();
            rootIpso.UpdateLayout();

            return rootIpso;
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
                        FontManager.Self.ReactToFontValueSet(instance);
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
                    foreach (var exposedVariable in baseElement.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                    {
                        graphicalElement.AddExposedVariable(exposedVariable.ExposedAsName, exposedVariable.Name);
                    }



                }



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
                    graphicalElement.SetStatesAndCategoriesRecursively(baseElement);

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

                ElementWithState elementWithState = new ElementWithState(baseComponentSave);
                var tempRvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);
                var state = tempRvf.GetValue("State") as string;
                elementWithState.StateName = state;

                foreach (var category in baseComponentSave.Categories)
                {
                    elementWithState.CategorizedStates.Add(category.Name, tempRvf.GetValue<string>(category.Name + "State"));
                }

                elementWithState.InstanceName = instance.Name;

                // Does this element already exist?
                bool alreadyExists = elementStack.Any(item => item.Element == elementWithState.Element);
                if(!alreadyExists)
                {
                    elementStack.Add(elementWithState);

                    foreach (InstanceSave internalInstance in baseComponentSave.Instances)
                    {
                        // let's make sure we don't recursively create the same instance causing a stack overflow:

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

                    if (!string.IsNullOrEmpty(parentName) && parentName != AvailableInstancesConverter.ScreenBoundsName)
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
}
