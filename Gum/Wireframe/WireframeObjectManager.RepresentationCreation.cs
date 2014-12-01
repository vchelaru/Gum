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

            if (isScreen == false)
            {
                rootIpso = new GraphicalUiElement(null, null);

                // We used to not add the IPSO for the root element to the list of graphical elements
                // and this prevented selection.  I'm not sure if this was intentionally left out or not
                // but I think it should be here
                mGraphicalElements.Add(rootIpso);

                if (elementSave.BaseType == "Sprite" || elementSave.Name == "Sprite")
                {
                    CreateSpriteFor(elementSave, rootIpso);
                }
                else if (elementSave.BaseType == "Text" || elementSave.Name == "Text")
                {
                    CreateTextFor(elementSave, rootIpso);
                }
                else if (elementSave.BaseType == "NineSlice" || elementSave.Name == "NineSlice")
                {
                    CreateNineSliceFor(elementSave, rootIpso);
                }
                else if (elementSave.BaseType == "ColoredRectangle" || elementSave.Name == "ColoredRectangle")
                {
                    CreateSolidRectangleFor(elementSave, rootIpso);
                }
                else
                {
                    CreateRectangleFor(elementSave, rootIpso);
                }


                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(SelectedState.Self.SelectedStateSaveOrDefault);

                string guide = rvf.GetValue<string>("Guide");
                SetGuideParent(null, rootIpso, guide, true);

            }
            else
            {
                rootIpso = new GraphicalUiElement();
                rootIpso.Tag = elementSave;
                mGraphicalElements.Add(rootIpso);
            }
            rootIpso.ElementSave = elementSave;

            foreach(var exposedVariable in elementSave.DefaultState.Variables.Where(item=> !string.IsNullOrEmpty(item.ExposedAsName) ))
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
                    mGraphicalElements.Add(child);

                    
                }
            }

                
            SetUpParentRelationship(newlyAdded, elementStack, elementSave.Instances);



            //);
            elementStack.Remove(elementStack.FirstOrDefault(item => item.Element == elementSave));



            rootIpso.SetStatesAndCategoriesRecursively(elementSave);

            // First we need to the default state (and do so recursively)
            rootIpso.SetVariablesRecursively(elementSave, elementSave.DefaultState);
            // then we override it with the current state if one is set:
            if (SelectedState.Self.SelectedStateSave != elementSave.DefaultState && SelectedState.Self.SelectedStateSave != null)
            {
                var state = SelectedState.Self.SelectedStateSave;
                rootIpso.SetVariablesTopLevel(elementSave, state);
            }

            // I think this has to be *after* we set varaibles because that's where clipping gets set
            if (rootIpso != null)
            {
                rootIpso.AddToManagers();

                if (rootIpso.ElementSave is ScreenSave)
                {
                    // If it's a screen and it hasn't been added yet, we need to add it.
                    foreach (var item in rootIpso.ContainedElements.Where(candidate=>candidate.Managers == null))
                    {
                        item.AddToManagers();
                    }
                }
            }

            return rootIpso;
        }

        private GraphicalUiElement CreateRepresentationForInstance(InstanceSave instance, InstanceSave parentInstance, List<ElementWithState> elementStack, GraphicalUiElement container)
        {
            IPositionedSizedObject newIpso = null;

            GraphicalUiElement graphicalElement = newIpso as GraphicalUiElement;
            var baseElement = ObjectFinder.Self.GetElementSave(instance.BaseType);

            if (instance.BaseType == "Sprite")
            {

                graphicalElement = new GraphicalUiElement(null, container);

                CreateSpriteFor(instance, elementStack, container, graphicalElement);
            }
            else if (instance.BaseType == "Text")
            {
                graphicalElement = new GraphicalUiElement(null, container);
                CreateTextFor(instance, elementStack, container, graphicalElement);
            }
            else if (instance.BaseType == "ColoredRectangle")
            {
                graphicalElement = new GraphicalUiElement(null, container);
                CreateSolidRectangleFor(instance, elementStack, graphicalElement);
            }
            else if (instance.BaseType == "NineSlice")
            {
                graphicalElement = new GraphicalUiElement(null, container);
                CreateNineSliceFor(instance, elementStack, graphicalElement);
            }
            else if (instance.IsComponent())
            {
                // Here we need to prefix the name before going any deeper, so we won't won't prefix the name at the end of the method
                //shouldPrefixParentName = false;

                //string prefixName = null;
                //if (parentIpso != null)
                //{
                //prefixName = parentIpso.Name + ".";
                //}
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

                SetGuideParent(container, graphicalElement, guide, container == null || parent == AvailableInstancesConverter.ScreenBoundsName);
            }

            if (baseElement != null)
            {
                graphicalElement.SetStatesAndCategoriesRecursively(baseElement);
                graphicalElement.SetVariablesRecursively(baseElement, baseElement.DefaultState);
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

                if (ses.Name == "Sprite")
                {
                    CreateSpriteFor(instance, elementStack, parentIpso, rootIpso);
                }
                else if (ses.Name == "Text")
                {
                    CreateTextFor(instance, elementStack, parentIpso, rootIpso);
                }
                else if (ses.Name == "ColoredRectangle")
                {
                    CreateSolidRectangleFor(instance, elementStack, rootIpso);
                }
                else if (ses.Name == "NineSlice")
                {
                    CreateNineSliceFor(instance, elementStack, rootIpso);
                }
                else
                {
                    CreateRectangleFor(instance, elementStack, rootIpso);
                }

                var selectedState = SelectedState.Self.SelectedStateSave;

                if (selectedState == null)
                {
                    selectedState = SelectedState.Self.SelectedElement.DefaultState;
                }

                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(selectedState);

                string guide = rvf.GetValue<string>("Guide");
                SetGuideParent(parentIpso, rootIpso, guide, false);

                ElementWithState elementWithState = new ElementWithState(baseComponentSave);
                var tempRvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);
                var state = tempRvf.GetValue("State") as string;
                elementWithState.StateName = state;

                foreach (var category in baseComponentSave.Categories)
                {
                    elementWithState.CategorizedStates.Add(category.Name, tempRvf.GetValue<string>(category.Name + "State"));
                }

                elementWithState.InstanceName = instance.Name;
                elementStack.Add(elementWithState);

                foreach (InstanceSave internalInstance in baseComponentSave.Instances)
                {
                    GraphicalUiElement createdIpso = CreateRepresentationForInstance(internalInstance, instance, elementStack, rootIpso);

                }

                SetUpParentRelationship(rootIpso.ContainedElements, elementStack, baseComponentSave.Instances);

                elementStack.Remove(elementStack.FirstOrDefault(item => item.Element == baseComponentSave));
            }

            return rootIpso;
        }

        private void SetUpParentRelationship(IEnumerable<GraphicalUiElement> siblings, List<ElementWithState> elementStack, IEnumerable<InstanceSave> childrenInstances)
        {
            // Now that we have created all instances, we can establish parent relationships

            foreach (GraphicalUiElement contained in siblings)
            {
                if (contained.Tag is InstanceSave)
                {
                    InstanceSave childInstanceSave = contained.Tag as InstanceSave;
                    RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(childInstanceSave, elementStack);

                    string parentName = rvf.GetValue<string>("Parent");

                    if (!string.IsNullOrEmpty(parentName) && parentName != AvailableInstancesConverter.ScreenBoundsName)
                    {
                        IPositionedSizedObject newParent = siblings.FirstOrDefault(item => item.Name == parentName);

                        // This may have bad XML so if it doesn't exist, then let's ignore this:
                        if (newParent != null)
                        {

                            contained.Parent = newParent;

                        }
                    }
                }
            }
        }




        private GraphicalUiElement CreateSpriteFor(ElementSave elementSave, GraphicalUiElement graphicalUiElement)
        {

            RecursiveVariableFinder rvf = GetRvfForCurrentElementState(elementSave);


            Sprite sprite = new Sprite(LoaderManager.Self.InvalidTexture);
            sprite.Name = elementSave.Name;
            sprite.Tag = elementSave;

            graphicalUiElement.SetContainedObject(sprite);
      

            InitializeSprite(sprite, rvf);
            
            graphicalUiElement.Visible = (bool)rvf.GetValue("Visible");

            graphicalUiElement.SetGueValues(rvf);


            return graphicalUiElement;
        }
        private GraphicalUiElement CreateSpriteFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentRepresentation, GraphicalUiElement graphicalUiElement)
        {
            ElementSave parent = elementStack.Last().Element;

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            Sprite sprite = new Sprite(LoaderManager.Self.InvalidTexture);
            sprite.Name = instance.Name;
            sprite.Tag = instance;

            graphicalUiElement.SetContainedObject(sprite);
                
                
            InitializeSprite(sprite, rvf);

            // Sprite may be dependent on the texture for its location, so set the dimensions and positions *after* texture

            graphicalUiElement.SetGueValues(rvf);


            graphicalUiElement.Visible = (bool)rvf.GetValue("Visible");
            return graphicalUiElement;
        }


        private IPositionedSizedObject CreateNineSliceFor(InstanceSave instance, List<ElementWithState> elementStack, GraphicalUiElement graphicalUiElement)
        {
            ElementSave parent = elementStack.Last().Element;
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            
            NineSlice nineSlice = CreateNineSliceInternal(instance, instance.Name, rvf);
            
            InitializeNineSlice(nineSlice, rvf);

            // NineSlice may be dependent on the texture for its location, so set the dimensions and positions *after* texture

            graphicalUiElement.SetGueValues(rvf);

            graphicalUiElement.SetContainedObject(nineSlice);

            return graphicalUiElement;
        }
        private IPositionedSizedObject CreateNineSliceFor(ElementSave elementSave, GraphicalUiElement graphicalUiElement)
        {

            RecursiveVariableFinder rvf = GetRvfForCurrentElementState(elementSave);

            NineSlice nineSlice = CreateNineSliceInternal(elementSave, elementSave.Name, rvf);


            InitializeNineSlice(nineSlice, rvf);


            graphicalUiElement.SetGueValues(rvf);

            graphicalUiElement.SetContainedObject(nineSlice);

            return graphicalUiElement;
        }
        private NineSlice CreateNineSliceInternal(object tag, string name, RecursiveVariableFinder rvf)
        {
            NineSlice nineSlice = new NineSlice();

            nineSlice.Name = name;
            nineSlice.Tag = tag;

            return nineSlice;
        }
        
        private IPositionedSizedObject CreateSolidRectangleFor(ElementSave elementSave, GraphicalUiElement graphicalUiElement)
        {

            RecursiveVariableFinder rvf = GetRvfForCurrentElementState(elementSave);

            string baseType = elementSave.BaseType;
            if (string.IsNullOrEmpty(baseType))
            {
                baseType = elementSave.Name;
            }
            
            graphicalUiElement.CreateGraphicalComponent(elementSave, null);
           
            graphicalUiElement.Tag = elementSave;



            SolidRectangle solidRectangle = graphicalUiElement.RenderableComponent as SolidRectangle;

            InitializeSolidRectangle(solidRectangle, elementSave.Name);
            solidRectangle.Tag = elementSave;



            

            return graphicalUiElement;           
        }

        private IPositionedSizedObject CreateSolidRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, GraphicalUiElement graphicalUiElement)
        {
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
            
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            graphicalUiElement.CreateGraphicalComponent(instanceBase, null);

            graphicalUiElement.Tag = instance;

            SolidRectangle solidRectangle = graphicalUiElement.RenderableComponent as SolidRectangle;

            InitializeSolidRectangle(solidRectangle, instance.Name);
            solidRectangle.Tag = instance;
            
            return graphicalUiElement;
        }

        private void InitializeSolidRectangle(SolidRectangle solidRectangle, string name)
        {

            solidRectangle.Name = name;
        }


        private IPositionedSizedObject CreateRectangleFor(ElementSave elementSave, GraphicalUiElement graphicalUiElement)
        {
            LineRectangle lineRectangle = InstantiateAndNameRectangle(elementSave.Name);
            lineRectangle.Tag = elementSave;

            RecursiveVariableFinder rvf = GetRvfForCurrentElementState(elementSave);

            graphicalUiElement.SetContainedObject(lineRectangle);

            graphicalUiElement.SetGueValues(rvf);

            // Right now I only set this value on GUEs for elements and not visual things like Sprites/Texts.
            // This may change.
            graphicalUiElement.ChildrenLayout = rvf.GetValue<ChildrenLayout>("Children Layout");
            graphicalUiElement.ClipsChildren = rvf.GetValue<bool>("Clips Children");
            graphicalUiElement.WrapsChildren = rvf.GetValue<bool>("Wraps Children");

            return graphicalUiElement;
        }

        private static DataTypes.RecursiveVariableFinder GetRvfForCurrentElementState(ElementSave elementSave)
        {
            StateSave sourceStateSave = elementSave.DefaultState;
            if (elementSave == SelectedState.Self.SelectedElement && SelectedState.Self.SelectedStateSave != null)
            {
                sourceStateSave = SelectedState.Self.SelectedStateSave;
            }

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(sourceStateSave);
            return rvf;
        }
        private IPositionedSizedObject CreateRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, GraphicalUiElement graphicalUiElement)
        {

            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            LineRectangle lineRectangle = InstantiateAndNameRectangle(instance.Name);
            lineRectangle.Tag = instance;

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);
            
            lineRectangle.Visible = rvf.GetValue<bool>("Visible");
            lineRectangle.LocalVisible = GraphicalUiElement.ShowLineRectangles;

            graphicalUiElement.SetContainedObject(lineRectangle);

            graphicalUiElement.SetGueValues(rvf);

            graphicalUiElement.ChildrenLayout = rvf.GetValue<ChildrenLayout>("Children Layout");
            graphicalUiElement.ClipsChildren = rvf.GetValue<bool>("Clips Children");
            graphicalUiElement.WrapsChildren = rvf.GetValue<bool>("Wraps Children");

            return graphicalUiElement;
        }
        private LineRectangle InstantiateAndNameRectangle(string name)
        {
            LineRectangle lineRectangle = new LineRectangle();

            lineRectangle.Name = name;
            return lineRectangle;
        }

        private void SetGuideParent(GraphicalUiElement parentIpso, GraphicalUiElement ipso, string guideName, bool setParentToBoundsIfNoGuide)
        {
            // I dont't think we want to do this anymore because it should be handled by the GraphicalUiElement
            if (parentIpso != null && (parentIpso.Tag == null || parentIpso.Tag is ScreenSave == false))
            {
                ipso.Parent = parentIpso;
            }

            if (!string.IsNullOrEmpty(guideName))
            {
                IPositionedSizedObject guideIpso = GetGuide(guideName);

                if (guideIpso != null)
                {
                    ipso.Parent = guideIpso;
                }
            }
            else if (setParentToBoundsIfNoGuide) 
            {
                ipso.Parent = mWireframeControl.ScreenBounds;
            }
        }


        private IPositionedSizedObject CreateTextFor(ElementSave elementSave, GraphicalUiElement graphicalUiElement)
        {

            RecursiveVariableFinder rvf = GetRvfForCurrentElementState(elementSave);

            Text text = CreateTextInternal(elementSave, elementSave.Name, rvf);


            graphicalUiElement.SetGueValues(rvf);

            text.EnableTextureCreation();

            graphicalUiElement.SetContainedObject(text);

            return graphicalUiElement;
        }
        private IPositionedSizedObject CreateTextFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentRepresentation, GraphicalUiElement graphicalUiElement)
        {
            ElementSave parent = elementStack.Last().Element;
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            Text text = CreateTextInternal(instance, instance.Name, rvf);
            
            graphicalUiElement.SetContainedObject(text);
            graphicalUiElement.SetGueValues(rvf);

            return graphicalUiElement;
        }
        private Text CreateTextInternal(object tag, string name, RecursiveVariableFinder rvf)
        {
            Text text = new Text(null);
            text.SuppressTextureCreation();
            text.Tag = tag;
            text.Name = name;

            text.RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;

            text.Alpha = rvf.GetValue<int>("Alpha") ;
            text.Red = rvf.GetValue<int>("Red");
            text.Green = rvf.GetValue<int>("Green");
            text.Blue = rvf.GetValue<int>("Blue");

            text.Visible = rvf.GetValue<bool>("Visible");

            if (rvf.GetVariable("Font Scale") != null)
            {
                text.FontScale = rvf.GetValue<float>("Font Scale");
            }

            text.RawText = (string)rvf.GetValue("Text");
            text.HorizontalAlignment = (HorizontalAlignment)rvf.GetValue("HorizontalAlignment");
            text.VerticalAlignment = (VerticalAlignment)rvf.GetValue("VerticalAlignment");

            if(rvf.GetValue<bool>("UseCustomFont"))
            {
                string customFontFile = rvf.GetValue<string>("CustomFontFile");

                if (!string.IsNullOrEmpty(customFontFile))
                {
                    customFontFile = ProjectManager.Self.MakeAbsoluteIfNecessary(customFontFile);

                    if (System.IO.File.Exists(customFontFile))
                    {
                        BitmapFont font = new BitmapFont(customFontFile, null);
                        text.BitmapFont = font;
                    }
                }
            }
            else
            {
                string fontName = (string)rvf.GetValue("Font");
                int fontSize = (int)rvf.GetValue("FontSize");
                int outlineThickness = rvf.GetValue<int>("OutlineThickness");

                BmfcSave.CreateBitmapFontFilesIfNecessary(
                    fontSize,
                    fontName,
                    outlineThickness);

                text.BitmapFont = FontManager.Self.GetBitmapFontFor(fontName, fontSize, outlineThickness);
            }
            text.EnableTextureCreation();
            return text;
        }

                
        StateSave temporaryStateSave = new StateSave();


        //public void FillStateWithPositionAndSizeVariables(ElementSave elementSave, StateSave stateSave)
        //{
        //    FillStateWithVariables(elementSave, stateSave, PositionAndSizeVariables);
        //}
        //public void FillStateWithPositionAndSizeVariables(InstanceSave instance, ElementSave parent, StateSave stateSave)
        //{
        //    FillStateWithVariables(instance, parent, stateSave, PositionAndSizeVariables);
        //}
        public void FillStateWithVariables(RecursiveVariableFinder rvf, StateSave stateSave, IEnumerable variables)
        {
            foreach (string variable in variables)
            {
                stateSave.SetValue(variable, rvf.GetValue(variable));
            }
        }
        
        private void InitializeSprite(Sprite sprite, RecursiveVariableFinder rvf)
        {
            string textureName = (string)rvf.GetValue("SourceFile");
            bool animate = rvf.GetValue<bool>("Animate");
            List<string> animations = (List<string>)rvf.GetValue("AnimationFrames");

            sprite.Animate = animate;

            textureName = ProjectManager.Self.MakeAbsoluteIfNecessary(textureName);

            if (animations != null && animations.Count != 0)
            {
                for (int i = 0; i < animations.Count; i++)
                {
                    animations[i] = ProjectManager.Self.MakeAbsoluteIfNecessary(animations[i]);
                }

                TextureFlipAnimation tfa = TextureFlipAnimation.FromStringList(animations, null);
                
                sprite.Animation = tfa;

            }
            else
            {
                if (!string.IsNullOrEmpty(textureName))
                {
                    if (System.IO.File.Exists(textureName))
                    {
                        string error;
                        sprite.Texture = LoaderManager.Self.LoadOrInvalid(textureName, null, out error);
                        // Do we want to print missing textures somewhere?  Well, for now we'll just
                        // tolerate it silently because the invalid texture is shown
                    }
                    else
                    {
                        // The file doesn't exist, let's give this Sprite the null texture
                        sprite.Texture = LoaderManager.Self.InvalidTexture;
                    }
                }
            }


            if (rvf.GetValue("FlipHorizontal") != null)
            {
                sprite.FlipHorizontal = (bool)rvf.GetValue("FlipHorizontal");
            }
            if (rvf.GetValue("FlipVertical") != null)
            {
                sprite.FlipVertical = (bool)rvf.GetValue("FlipVertical");
            }

            var color = GetColorFromRvf(rvf);

            sprite.Color = color;

            if (rvf.GetValue("Blend") != null)
            {
                var blend = (Blend)rvf.GetValue("Blend");
                if (blend == Blend.Normal)
                {
                    // do nothing?
                }
                else if (blend == Blend.Additive)
                {
                    sprite.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive;
                }
                else if (blend == Blend.Replace)
                {
                    sprite.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Opaque;
                
                }

                else
                {
                    throw new Exception("This blend mode is not supported");
                }
            }

            sprite.Rotation = rvf.GetValue<float>("Rotation");

            SetSpriteTextureCoordinates(sprite, rvf);


        }

        private static void SetSpriteTextureCoordinates(Sprite sprite, RecursiveVariableFinder rvf)
        {

            // Old way:
            //bool useCustomSource = rvf.GetValue<bool>("Custom Texture Coordinates");
            //if (useCustomSource)
            //{
            //    sprite.SourceRectangle = new Rectangle(
            //        rvf.GetValue<int>("Texture Left"),
            //        rvf.GetValue<int>("Texture Top"),
            //        rvf.GetValue<int>("Texture Width"),
            //        rvf.GetValue<int>("Texture Height"));

            //    sprite.Wrap = rvf.GetValue<bool>("Wrap");
            //}
            // New way:
            var textureAddress = rvf.GetValue<TextureAddress>("Texture Address");
            switch (textureAddress)
            {
                case TextureAddress.EntireTexture:
                    sprite.SourceRectangle = null;
                    sprite.Wrap = false;
                    break;
                case TextureAddress.Custom:
                    sprite.SourceRectangle = new Rectangle(
                        rvf.GetValue<int>("Texture Left"),
                        rvf.GetValue<int>("Texture Top"),
                        rvf.GetValue<int>("Texture Width"),
                        rvf.GetValue<int>("Texture Height"));
                    sprite.Wrap = rvf.GetValue<bool>("Wrap");

                    break;
                case TextureAddress.DimensionsBased:
                    int left = rvf.GetValue<int>("Texture Left");
                    int top = rvf.GetValue<int>("Texture Top");
                    int width = (int)(sprite.EffectiveWidth * rvf.GetValue<float>("Texture Width Scale"));
                    int height = (int)(sprite.EffectiveHeight * rvf.GetValue<float>("Texture Height Scale"));

                    sprite.SourceRectangle = new Rectangle(
                        left,
                        top,
                        width,
                        height);
                    sprite.Wrap = rvf.GetValue<bool>("Wrap");

                    break;
            }
        }

        private static Microsoft.Xna.Framework.Color GetColorFromRvf(RecursiveVariableFinder rvf)
        {
            int alpha = 255;
            int red = 255;
            int green = 255;
            int blue = 255;

            alpha = rvf.GetValue<int>("Alpha");
            red = rvf.GetValue<int>("Red");
            green = rvf.GetValue<int>("Green");
            blue = rvf.GetValue<int>("Blue");

            var color = new Color(red, green, blue, alpha);
            return color;
        }

        private void InitializeNineSlice(NineSlice nineSlice, RecursiveVariableFinder rvf)
        {
            string textureName = (string)rvf.GetValue("SourceFile");
            string absoluteTexture = ProjectManager.Self.MakeAbsoluteIfNecessary(textureName);

            string withoutExtension = FileManager.RemoveExtension(absoluteTexture);

            bool usePattern = NineSlice.GetIfShouldUsePattern(absoluteTexture);

            string toReturn = withoutExtension;

            if (FileManager.FileExists(absoluteTexture))
            {
                if (usePattern)
                {
                    nineSlice.SetTexturesUsingPattern(absoluteTexture, null);
                }
                else
                {
                    if (!string.IsNullOrEmpty(absoluteTexture))
                    {
                        nineSlice.SetSingleTexture(LoaderManager.Self.Load(absoluteTexture, null));
                    }

                }
            }
            var color = GetColorFromRvf(rvf);
            nineSlice.Color = color;

            if (rvf.GetValue("Blend") != null)
            {
                var blend = (Blend)rvf.GetValue("Blend");
                if (blend == Blend.Normal)
                {
                    // do nothing?
                }
                else if (blend == Blend.Additive)
                {
                    nineSlice.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive;
                }
                else
                {
                    throw new Exception("This blend mode is not supported");
                }
            }


            nineSlice.Visible = rvf.GetValue<bool>("Visible");

        }



    }
}
