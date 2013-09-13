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


        Dictionary<NineSliceSections, string> PossibleNineSliceEndings = new Dictionary<NineSliceSections, string>()
        {
            {NineSliceSections.Center, "_center"},
            {NineSliceSections.Left, "_left"},
            {NineSliceSections.Right, "_right"},
            {NineSliceSections.TopLeft, "_topLeft"},
            {NineSliceSections.Top, "_topCenter"},
            {NineSliceSections.TopRight, "_topRight"},
            {NineSliceSections.BottomLeft, "_bottomLeft"},
            {NineSliceSections.Bottom, "_bottomCenter"},
            {NineSliceSections.BottomRight, "_bottomRight"}
        };

        #endregion

        private void CreateIpsoForElement(ElementSave elementSave)
        {
            IPositionedSizedObject rootIpso = null;

            bool isScreen = elementSave is ScreenSave;

            if (isScreen == false)
            {
                if (elementSave.BaseType == "Sprite" || elementSave.Name == "Sprite")
                {
                    rootIpso = CreateSpriteFor(elementSave);
                }
                else if (elementSave.BaseType == "Text" || elementSave.Name == "Text")
                {
                    rootIpso = CreateTextFor(elementSave);
                }
                else if (elementSave.BaseType == "NineSlice" || elementSave.Name == "NineSlice")
                {
                    rootIpso = CreateNineSliceFor(elementSave);
                }
                else if (elementSave.BaseType == "ColoredRectangle" || elementSave.Name == "ColoredRectangle")
                {
                    rootIpso = CreateSolidRectangleFor(elementSave);
                }
                else
                {
                    rootIpso = CreateRectangleFor(elementSave);
                }
            }

            List<ElementWithState> elementStack = new List<ElementWithState>();

            ElementWithState elementWithState = new ElementWithState(elementSave);
            if (elementSave == SelectedState.Self.SelectedElement)
            {
                elementWithState.StateName = SelectedState.Self.SelectedStateSave.Name;
            }

            elementStack.Add(elementWithState);

            // parallel screws up the ordering of objects, so we'll do it on the primary thread for now
            // and parallelize it later:
            //Parallel.ForEach(elementSave.Instances, instance =>
            foreach (var instance in elementSave.Instances)
            {
                IPositionedSizedObject child = CreateRepresentationForInstance(instance, null, elementStack, rootIpso);
            }

                
            SetUpParentRelationship(null, elementStack, elementSave.Instances);

            if (rootIpso != null)
            {
                SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(rootIpso, elementStack.LastOrDefault().Element, new RecursiveVariableFinder(SelectedState.Self.SelectedStateSave));
                UpdateScalesAndPositionsForSelectedChildren(rootIpso, null, elementStack);
            }

            //);
            elementStack.Remove(elementStack.FirstOrDefault(item => item.Element == elementSave));
        }

        private IPositionedSizedObject CreateRepresentationForInstance(InstanceSave instance, InstanceSave parentInstance, List<ElementWithState> elementStack, IPositionedSizedObject parentIpso)
        {
            IPositionedSizedObject toReturn = null;


            List<VariableSave> exposedVariables = GetExposedVariablesForThisInstance(instance, elementStack.Last().InstanceName, elementStack);


            if (instance.BaseType == "Sprite")
            {
                toReturn = CreateSpriteFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (instance.BaseType == "Text")
            {
                toReturn = CreateTextFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (instance.BaseType == "ColoredRectangle")
            {
                toReturn = CreateSolidRectangleFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (instance.BaseType == "NineSlice")
            {
                toReturn = CreateNineSliceFor(instance, elementStack, parentIpso, exposedVariables);
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
                toReturn = CreateRepresentationsForInstanceFromComponent(instance, elementStack, parentInstance, parentIpso, ObjectFinder.Self.GetComponent(instance.BaseType));
            }
            else
            {
                // Make sure the base type is valid.
                // This could be null if a base type changed
                // its name but the derived wasn't updated, or 
                // if someone screwed with the XML files.  Who knows...
                var baseElement = ObjectFinder.Self.GetElementSave(instance.BaseType);

                if (baseElement != null)
                {
                    toReturn = CreateRectangleFor(instance, elementStack, parentIpso, exposedVariables);
                }
            }
            //if (shouldPrefixParentName)
            //{
            //    string prefixName = null;
            //    if (parentIpso != null)
            //    {
            //        prefixName = parentIpso.Name + ".";
            //    }

            //    toReturn.Name = prefixName + toReturn.Name;

            //}

            // This was an attempt to fix sorting when parallelized
            //if (toReturn != null)
            //{
            //    if(parentInstance == null)
            //    {
            //        var parent = instance.ParentContainer;

            //        if(parent != null)
            //        {
            //            toReturn.Z = parent.Instances.IndexOf(instance);
            //        }
            //    }
            //}

            return toReturn;
        }

        public List<VariableSave> GetExposedVariablesForThisInstance(DataTypes.InstanceSave instance, string parentInstanceName, List<ElementWithState> elementStack)
        {
            List<VariableSave> exposedVariables = new List<VariableSave>();
            if (elementStack.Count > 1)
            {
                ElementWithState containerOfVariables = elementStack[elementStack.Count - 2];
                ElementWithState definerOfVariables = elementStack[elementStack.Count - 1];

                foreach (VariableSave variable in definerOfVariables.Element.DefaultState.Variables)
                {
                    if (!string.IsNullOrEmpty(variable.ExposedAsName) && variable.SourceObject == instance.Name)
                    {
                        // This variable is exposed, let's see if the container does anything with it

                        VariableSave foundVariable = containerOfVariables.StateSave.GetVariableRecursive(parentInstanceName + "." + variable.ExposedAsName);

                        if (foundVariable != null)
                        {
                            VariableSave variableToAdd = new VariableSave();
                            variableToAdd.Type = variable.Type;
                            variableToAdd.Value = foundVariable.Value;
                            variableToAdd.Name = variable.Name.Substring(variable.Name.IndexOf('.') + 1);
                            exposedVariables.Add(variableToAdd);
                        }

                    }

                }

            }

            return exposedVariables;
        }

        private IPositionedSizedObject CreateRepresentationsForInstanceFromComponent(InstanceSave instance, 
            List<ElementWithState> elementStack, InstanceSave parentInstance, IPositionedSizedObject parentIpso, 
            ComponentSave baseComponentSave)
        {
            StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(instance);

            List<VariableSave> exposedVariables = GetExposedVariablesForThisInstance(instance, elementStack.Last().InstanceName, elementStack);

            IPositionedSizedObject rootIpso = null;

            if (ses.Name == "Sprite")
            {
                rootIpso = CreateSpriteFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (ses.Name == "Text")
            {
                rootIpso = CreateTextFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (ses.Name == "ColoredRectangle")
            {
                rootIpso = CreateSolidRectangleFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else if (ses.Name == "NineSlice")
            {
                rootIpso = CreateNineSliceFor(instance, elementStack, parentIpso, exposedVariables);
            }
            else
            {
                rootIpso = CreateRectangleFor(instance, elementStack, parentIpso, exposedVariables);
            }

            ElementWithState elementWithState = new ElementWithState(baseComponentSave);
            var state = new DataTypes.RecursiveVariableFinder(instance, elementStack).GetValue("State") as string;
            elementWithState.StateName = state;
            elementWithState.InstanceName = instance.Name;
            elementStack.Add( elementWithState );

            foreach (InstanceSave internalInstance in baseComponentSave.Instances)
            {
                IPositionedSizedObject createdIpso = CreateRepresentationForInstance(internalInstance, instance, elementStack, rootIpso);

            }

            SetUpParentRelationship(instance, elementStack, baseComponentSave.Instances);

            // This pulls from the Instance's state, which if set can override hard values
            // set on the instance.  States should take a backseat to explicit values set.  So
            // instead, we want this funciton to be called inside the creation of the object instead
            // of after.
            //SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(rootIpso, elementStack.LastOrDefault().Element, new RecursiveVariableFinder(instance, elementStack));
            UpdateScalesAndPositionsForSelectedChildren(rootIpso, instance, elementStack);
            elementStack.Remove( elementStack.FirstOrDefault(item=>item.Element == baseComponentSave));

            return rootIpso;
        }

        private void SetUpParentRelationship(InstanceSave containerInstance, List<ElementWithState> elementStack, IEnumerable<InstanceSave> childrenInstances)
        {
            // Now that we have created all instances, we can establish parent relationships
            foreach(var childInstanceSave in childrenInstances)
            {
                List<VariableSave> childExposedVariables = null;

                if (childInstanceSave != null)
                {
                    childExposedVariables = GetExposedVariablesForThisInstance(childInstanceSave, elementStack.Last().InstanceName, elementStack);
                }

                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(childInstanceSave, elementStack);

                string parentName = rvf.GetValue<string>("Parent");

                if (!string.IsNullOrEmpty(parentName))
                {
                    InstanceSave parentInstanceSave = childrenInstances.FirstOrDefault(item => item.Name == parentName);

                    IPositionedSizedObject childIpso = WireframeObjectManager.Self.GetRepresentation(childInstanceSave);
                    IPositionedSizedObject parentIpso = WireframeObjectManager.Self.GetRepresentation(parentInstanceSave);

                    if (childIpso != null)
                    {
                        childIpso.Parent = parentIpso;


                        SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(childIpso, elementStack.LastOrDefault().Element, rvf);



                        UpdateScalesAndPositionsForSelectedChildren(childIpso, childInstanceSave, elementStack);

                    }
                }

            }
        }




        private IPositionedSizedObject CreateSpriteFor(ElementSave elementSave)
        {
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            Sprite sprite = CreateSpriteInternal(elementSave, elementSave.Name, rvf, null);
      

            InitializeSprite(sprite, rvf);

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(sprite, elementSave, rvf);
            
            return sprite;
        }



        private IPositionedSizedObject CreateSpriteFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentRepresentation, List<VariableSave> exposedVariables)
        {
            ElementSave parent = elementStack.Last().Element;
            try
            {
                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

                Sprite sprite = CreateSpriteInternal(instance, instance.Name, rvf, parentRepresentation);
                
                
                InitializeSprite(sprite, rvf);

                // Sprite may be dependent on the texture for its location, so set the dimensions and positions *after* texture
                SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(sprite, parent, rvf);

                return sprite;
            }
            catch (Exception e)
            {
                int m = 3;
                throw e;
            }
        }

        private IPositionedSizedObject CreateNineSliceFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentRepresentation, List<VariableSave> exposedVariables)
        {
            ElementSave parent = elementStack.Last().Element;
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            NineSlice nineSlice = CreateNineSliceInternal(instance, instance.Name, rvf, parentRepresentation);
            

            InitializeNineSlice(nineSlice, rvf);

            // NineSlice may be dependent on the texture for its location, so set the dimensions and positions *after* texture
            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(nineSlice, parent, rvf);



            return nineSlice;
        }

        private IPositionedSizedObject CreateNineSliceFor(ElementSave elementSave)
        {
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            NineSlice nineSlice = CreateNineSliceInternal(elementSave, elementSave.Name, rvf, null);


            InitializeNineSlice(nineSlice, rvf);

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(nineSlice, elementSave, rvf);

            return nineSlice;
        }

        private NineSlice CreateNineSliceInternal(object tag, string name, RecursiveVariableFinder rvf, IPositionedSizedObject parentIpso)
        {
            NineSlice nineSlice = new NineSlice();

            // Add it to the manager first because the positioning code may need to access the source element/instance
            SpriteManager.Self.Add(nineSlice);
            nineSlice.Name = name;
            nineSlice.Tag = tag;

            mNineSlices.Add(nineSlice);

            string guide = rvf.GetValue<string>("Guide");
            SetGuideParent(parentIpso, nineSlice, guide);

            return nineSlice;
        }
        



        private Sprite CreateSpriteInternal(object tag, string name, RecursiveVariableFinder rvf, IPositionedSizedObject parentIpso)
        {
            Sprite sprite = new Sprite(LoaderManager.Self.InvalidTexture);

            // Add it to the manager first because the positioning code may need to access the source element/instance
            SpriteManager.Self.Add(sprite);
            sprite.Name = name;
            sprite.Tag = tag;

            mSprites.Add(sprite);

            SetGuideParent(parentIpso, sprite, rvf.GetValue<string>("Guide"));


            return sprite;
        }

        private IPositionedSizedObject CreateSolidRectangleFor(ElementSave elementSave)
        {
            SolidRectangle solidRectangle = InstantiateAndNameSolidRectangle(elementSave.Name);
            solidRectangle.Tag = elementSave;
            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.ColorAndAlpha);


            SetGuideParent(null, solidRectangle, (string)stateSave.GetValue("Guide"));

            SetAlphaAndColorValues(solidRectangle, stateSave);
            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(solidRectangle, elementSave, rvf);

            return solidRectangle;           
        }
        private IPositionedSizedObject CreateSolidRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, List<VariableSave> exposedVariables)
        {

            IPositionedSizedObject parentIpso = GetRepresentation(elementStack.Last().Element);

            return CreateSolidRectangleFor(instance, elementStack, parentIpso, exposedVariables);
        }
        private IPositionedSizedObject CreateSolidRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentIpso, List<VariableSave> exposedVariables)
        {
            if (exposedVariables == null)
            {
                throw new Exception("The exposedVariables argument is null when trying to create a Rectangle.  It shouldn't be.");
            }

            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            SolidRectangle solidRectangle = InstantiateAndNameSolidRectangle(instance.Name);
            solidRectangle.Tag = instance;


            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.ColorAndAlpha);


            foreach (VariableSave exposed in exposedVariables)
            {
                stateSave.SetValue(exposed.Name, exposed.Value);
            }



            SetGuideParent(parentIpso, solidRectangle, (string)stateSave.GetValue("Guide"));

            stateSave.SetValue("Visible", rvf.GetValue("Visible"));


            SetAlphaAndColorValues(solidRectangle, stateSave);
            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(solidRectangle, elementStack.Last().Element, rvf);

            solidRectangle.Visible = (bool)stateSave.GetValue("Visible");

            return solidRectangle;
        }
        private void SetAlphaAndColorValues(SolidRectangle solidRectangle, StateSave stateSave)
        {
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(
                (int)stateSave.GetValue("Red"),
                (int)stateSave.GetValue("Green"),
                (int)stateSave.GetValue("Blue"),
                (int)stateSave.GetValue("Alpha")
                
                );
            solidRectangle.Color = color;
        }
        private SolidRectangle InstantiateAndNameSolidRectangle(string name)
        {
            SolidRectangle solidRectangle = new SolidRectangle();

            // Add it to the manager first because the positioning code may need to access the source element/instance
            ShapeManager.Self.Add(solidRectangle);
            solidRectangle.Name = name;
            mSolidRectangles.Add(solidRectangle);
            return solidRectangle;
        }



        private IPositionedSizedObject CreateRectangleFor(ElementSave elementSave)
        {
            LineRectangle lineRectangle = InstantiateAndNameRectangle(elementSave.Name);
            lineRectangle.Tag = elementSave;
            StateSave stateSave = new StateSave();

            StateSave sourceStateSave = elementSave.DefaultState;
            if (elementSave == SelectedState.Self.SelectedElement)
            {
                sourceStateSave = SelectedState.Self.SelectedStateSave;
            }

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(sourceStateSave);

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);


            SetGuideParent(null, lineRectangle, (string)stateSave.GetValue("Guide"));

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(lineRectangle, elementSave, rvf);



            return lineRectangle;
        }
        private IPositionedSizedObject CreateRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentIpso, List<VariableSave> exposedVariables)
        {
            if (exposedVariables == null)
            {
                throw new Exception("The exposedVariables argument is null when trying to create a Rectangle.  It shouldn't be.");
            }

            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            LineRectangle lineRectangle = InstantiateAndNameRectangle(instance.Name);
            lineRectangle.Tag = instance;

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            SetGuideParent(parentIpso, lineRectangle, rvf.GetValue<string>("Guide"));

            if (exposedVariables == null)
            {
                int m = 3;
            }
            
            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(lineRectangle, elementStack.Last().Element, rvf);

            lineRectangle.Visible = rvf.GetValue<bool>("Visible");

            return lineRectangle;
        }
        private LineRectangle InstantiateAndNameRectangle(string name)
        {
            LineRectangle lineRectangle = new LineRectangle();

            // Add it to the manager first because the positioning code may need to access the source element/instance
            ShapeManager.Self.Add(lineRectangle);
            lineRectangle.Name = name;
            mLineRectangles.Add(lineRectangle);
            return lineRectangle;
        }

        private void SetGuideParent(IPositionedSizedObject parentIpso, IPositionedSizedObject ipso, string guideName)
        {
            ipso.Parent = parentIpso;

            if (!string.IsNullOrEmpty(guideName))
            {
                IPositionedSizedObject guideIpso = GetGuide(guideName);

                if (guideIpso != null)
                {
                    ipso.Parent = guideIpso;
                }
            }
        }



        private IPositionedSizedObject CreateTextFor(ElementSave elementSave)
        {
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            Text text = CreateTextInternal(elementSave, elementSave.Name, rvf);

            StateSave stateSave = new StateSave();

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);

            SetGuideParent(null, text, (string)stateSave.GetValue("Guide"));

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(text, elementSave, rvf);



            text.EnableTextureCreation();

            return text;
        }
        private IPositionedSizedObject CreateTextFor(InstanceSave instance, List<ElementWithState> elementStack, IPositionedSizedObject parentRepresentation, List<VariableSave> exposedVariables)
        {
            ElementSave parent = elementStack.Last().Element;
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);

            Text text = CreateTextInternal(instance, instance.Name, rvf);
            
            if (exposedVariables == null)
            {
                throw new ArgumentException("The exposedVariable argument is null.  It needs to be non-null");
            }

            SetGuideParent(parentRepresentation, text, (string)rvf.GetValue("Guide"));

            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(text, parent, rvf);

            return text;
        }

        private Text CreateTextInternal(object tag, string name, RecursiveVariableFinder rvf)
        {
            Text text = new Text(null);
            text.SuppressTextureCreation();
            text.Tag = tag;
            text.Name = name;

            text.RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;

            // Add it to the manager first because the positioning code may need to access the source element/instance
            TextManager.Self.Add(text);
            mTexts.Add(text);

            text.Alpha = rvf.GetValue<int>("Alpha") / 255.0f;
            text.Red = rvf.GetValue<int>("Red")/255.0f;
            text.Green = rvf.GetValue<int>("Green") / 255.0f;
            text.Blue = rvf.GetValue<int>("Blue") / 255.0f;

            text.Visible = rvf.GetValue<bool>("Visible");



            text.RawText = (string)rvf.GetValue("Text");
            text.HorizontalAlignment = (HorizontalAlignment)rvf.GetValue("HorizontalAlignment");
            text.VerticalAlignment = (VerticalAlignment)rvf.GetValue("VerticalAlignment");


            string fontName = (string)rvf.GetValue("Font");
            int fontSize = (int)rvf.GetValue("FontSize");

            BmfcSave.CreateBitmapFontFilesIfNecessary(
                fontSize,
                fontName);

            text.BitmapFont = GetBitmapFontFor(fontName, fontSize);
            text.EnableTextureCreation();
            return text;
        }



        private static StateSave GetStateSaveForTextVariables(InstanceSave instance, List<ElementWithState> elementStack)
        {
            StateSave stateSave = new StateSave();

            AddToStateFromInstance(stateSave, instance, elementStack, "Text",
                "HorizontalAlignment",
                "VerticalAlignment",
                "Font",
                "FontSize",
                "Alpha",
                "Red",
                "Green",
                "Blue");

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, elementStack);
            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            return stateSave;
        }

        static void AddToStateFromInstance(StateSave stateSave, InstanceSave instance, List<ElementWithState> elementStack, params string[] variables)
        {
            foreach (string variable in variables)
            {
                var value = instance.GetValueFromThisOrBase(elementStack, variable);

                stateSave.SetValue(variable, value);
            }
        }

        private BitmapFont GetBitmapFontFor(string fontName, int fontSize)
        {
            string fileName = FileManager.RelativeDirectory + "FontCache/Font" + fontSize + fontName + ".fnt";

            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    BitmapFont bitmapFont = new BitmapFont(fileName, (SystemManagers)null);

                    return bitmapFont;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }



        public void SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(IPositionedSizedObject ipso, ElementSave containerElement, RecursiveVariableFinder rvf)
        {
            object widthAsObjects = rvf.GetValue("Width");
            object heightAsObjects = rvf.GetValue("Height");
            object heightUnits = rvf.GetValue("Height Units");
            object widthUnits = rvf.GetValue("Width Units");

            var horizontalAlignment = (HorizontalAlignment)rvf.GetValue("X Origin");
            var verticalAlignment = (VerticalAlignment)rvf.GetValue("Y Origin");




            object xAsObject = rvf.GetValue("X");
            object yAsObject = rvf.GetValue("Y");

            object xUnits = rvf.GetValue("X Units");
            object yUnits = rvf.GetValue("Y Units");
#if DEBUG
            if (xUnits is int)
            {
                throw new Exception("X Units must not be an int - must be an enum");
            }
            if (widthUnits is int)
            {
                throw new Exception("Width Units must not be an int - must be an enum");
            }
#endif
            float xAsFloat = 0;
            if (xAsObject != null)
            {
                xAsFloat = (float)xAsObject;
            }
            float yAsFloat = 0;
            if (yAsObject != null)
            {
                yAsFloat = (float)yAsObject;
            }


            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(
                ipso,
                containerElement,
                (float)widthAsObjects,
                (float)heightAsObjects,
                widthUnits,
                heightUnits,
                horizontalAlignment,
                verticalAlignment,
                xAsFloat,
                yAsFloat,
                xUnits,
                yUnits
                
                
                );
        }

        private static void SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(IPositionedSizedObject ipso, ElementSave containerElement, 
            float widthBeforePercentage, float heightBeforePercentage, 
            object widthUnitType, object heightUnitType,
            HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float xBeforePercentage, float yBeforePercentage,
            object xUnitType, object yUnitType
            )
        {
            try
            {
                float widthAfterPercentage;
                float heightAfterPercentage;

                GumProjectSave gumProjectSave = ObjectFinder.Self.GumProjectSave;

                ipso.UpdateAccordingToPercentages(
                    containerElement,

                    widthBeforePercentage,
                    heightBeforePercentage, widthUnitType, heightUnitType,
                    gumProjectSave.DefaultCanvasWidth,
                    gumProjectSave.DefaultCanvasHeight,
                    out widthAfterPercentage, out heightAfterPercentage);

                ipso.Width = widthAfterPercentage;
                ipso.Height = heightAfterPercentage;

                // This updates the texture in case the values are 0 so that the element can be positioned correctly
                if (ipso is Text)
                {
                    ((Text)ipso).UpdateTextureToRender();
                }

                float multiplierX;
                float multiplierY;
                GetMultipliersFromAlignment(horizontalAlignment, verticalAlignment, out multiplierX, out multiplierY);

                float xAfterPercentage;
                float yAfterPercentage;
                ipso.UpdateAccordingToPercentages(
                    containerElement,
                    xBeforePercentage,
                    yBeforePercentage,

                    xUnitType, yUnitType,
                    gumProjectSave.DefaultCanvasWidth,
                    gumProjectSave.DefaultCanvasHeight,
                    out xAfterPercentage, out yAfterPercentage);

                ipso.X = xAfterPercentage + multiplierX * ipso.Width;
                ipso.Y = yAfterPercentage + multiplierY * ipso.Height;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static void GetMultipliersFromAlignment(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, out float multiplierX, out float multiplierY)
        {
            multiplierX = 0;
            multiplierY = 0;
            if (horizontalAlignment == HorizontalAlignment.Center)
            {
                multiplierX = -.5f;
            }
            else if (horizontalAlignment == HorizontalAlignment.Right)
            {
                multiplierX = -1;
            }
            if (verticalAlignment == VerticalAlignment.Center)
            {
                multiplierY = -.5f;
            }
            if (verticalAlignment == VerticalAlignment.Bottom)
            {
                multiplierY = -1;
            }
        }


        //public void SetInstanceIpsoDimensionsAndPositions(IPositionedSizedObject ipso, InstanceSave instance, ElementSave parent, IPositionedSizedObject parentRepresentation)
        //{
        //    ipso.Parent = parentRepresentation;

        //    FillStateWithPositionAndSizeVariables(instance, parent, temporaryStateSave);
        //    SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, parent, temporaryStateSave);
        //}

        StateSave temporaryStateSave = new StateSave();

        //public void UpdateIpsoPositionAndScaleAccordingToParent(IPositionedSizedObject ipso, InstanceSave instance, ElementSave parent)
        //{
        //    StateSave stateSave = temporaryStateSave;

        //    FillStateWithPositionAndSizeVariables(instance, parent, stateSave);

        //    SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, parent, temporaryStateSave);
        //}



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
            bool animate = (bool)rvf.GetValue("Animate");
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
                        // Do we want to print this out somewhere?  Well, for now we'll just
                        // tolearte it silently because the invalid texture is shown
                    }
                    else
                    {
                        // The file doesn't exist, let's give this Sprite the null texture
                        sprite.Texture = LoaderManager.Self.InvalidTexture;
                    }
                }
            }

            sprite.Visible = (bool)rvf.GetValue("Visible");

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
                else
                {
                    throw new Exception("This blend mode is not supported");
                }
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

            string extension = FileManager.GetExtension(absoluteTexture);

            string bareTexture = GetBareTextureForNineSliceTexture(absoluteTexture);
            string error;
            if (!string.IsNullOrEmpty(bareTexture))
            {
                nineSlice.TopLeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.TopLeft] + "." + extension, null, out error);
                nineSlice.TopTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.Top] + "." + extension, null, out error);
                nineSlice.TopRightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.TopRight] + "." + extension, null, out error);

                nineSlice.LeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.Left] + "." + extension, null, out error);
                nineSlice.CenterTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.Center] + "." + extension, null, out error);
                nineSlice.RightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.Right] + "." + extension, null, out error);

                nineSlice.BottomLeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.BottomLeft] + "." + extension, null, out error);
                nineSlice.BottomTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.Bottom] + "." + extension, null, out error);
                nineSlice.BottomRightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + PossibleNineSliceEndings[NineSliceSections.BottomRight] + "." + extension, null, out error);
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


            nineSlice.Visible = (bool)rvf.GetValue("Visible");

        }

        public string GetBareTextureForNineSliceTexture(string absoluteTexture)
        {
            string extension = FileManager.GetExtension(absoluteTexture);

            string withoutExtension = FileManager.RemoveExtension(absoluteTexture);

            string toReturn = withoutExtension;

            foreach (var kvp in PossibleNineSliceEndings)
            {
                if (withoutExtension.ToLower().EndsWith(kvp.Value.ToLower()))
                {
                    toReturn = withoutExtension.Substring(0, withoutExtension.Length - kvp.Value.Length);
                    break;
                }
            }

            // No extensions, because we'll need to append that
            //toReturn += "." + extension;

            return toReturn;
        }

    }
}
