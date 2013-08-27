using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary;
using System.Collections;
using Gum.RenderingLibrary;
using Microsoft.Xna.Framework;
using FlatRedBall.AnimationEditorForms.Controls;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    #region Enums

    public enum InstanceFetchType
    {
        InstanceInCurrentElement,
        DeepInstance
    }

    #endregion

    public partial class WireframeObjectManager
    {
        #region Fields

        ElementSave mElementShowing;

        static WireframeObjectManager mSelf;

        List<LineRectangle> mLineRectangles = new List<LineRectangle>();
        List<Sprite> mSprites = new List<Sprite>();
        List<Text> mTexts = new List<Text>();
        List<SolidRectangle> mSolidRectangles = new List<SolidRectangle>();
        List<NineSlice> mNineSlices = new List<NineSlice>();

        WireframeEditControl mEditControl;
        WireframeControl mWireframeControl;

        Sprite mBackgroundSprite;

        const int left = -4096;
        const int width = 8192;


        #endregion

        #region Properties

        IEnumerable<IPositionedSizedObject> AllIpsos
        {
            get
            {
                foreach (Sprite sprite in mSprites)
                {
                    yield return sprite;
                }

                foreach (Text text in mTexts)
                {
                    yield return text;
                }
                foreach (LineRectangle rectangle in mLineRectangles)
                {
                    yield return rectangle;
                }
                foreach (SolidRectangle solidRectangle in mSolidRectangles)
                {
                    yield return solidRectangle;
                }
                foreach (NineSlice nineSlice in mNineSlices)
                {
                    yield return nineSlice;
                }
            }

        }

        public ElementSave ElementShowing
        {
            get;
            private set;
        }

        public static WireframeObjectManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new WireframeObjectManager();
                }
                return mSelf;
            }
        }

        #endregion

        public void Initialize(WireframeEditControl editControl, WireframeControl wireframeControl)
        {
            mWireframeControl = wireframeControl;
            mWireframeControl.AfterXnaInitialize += HandleAfterXnaIntiailize;
            mWireframeControl.XnaUpdate += HandleXnaUpdate;

            mEditControl = editControl;
            mEditControl.ZoomChanged += new EventHandler(HandleControlZoomChange);
        }

        private void HandleAfterXnaIntiailize(object sender, EventArgs e)
        {
            // Create the Texture2D here
            ImageData imageData = new ImageData(2, 2, null);

            int lightColor = 150;
            int darkColor = 170;
            Color darkGray = new Color(lightColor, lightColor, lightColor);
            Color lightGray = new Color(darkColor, darkColor, darkColor);

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    bool isDark = ((x + y) % 2 == 0);
                    if (isDark)
                    {
                        imageData.SetPixel(x, y, darkGray);

                    }
                    else
                    {
                        imageData.SetPixel(x, y, lightGray);
                    }
                }
            }

            Texture2D texture = imageData.ToTexture2D(false);
            mBackgroundSprite = new Sprite(texture);

            mBackgroundSprite.Wrap = true;
            mBackgroundSprite.X = -4096;
            mBackgroundSprite.Y = -4096;
            mBackgroundSprite.Width = 8192;
            mBackgroundSprite.Height = 8192;

            mBackgroundSprite.Wrap = true;
            int timesToRepeat = 256;
            mBackgroundSprite.SourceRectangle = new Rectangle(0, 0, timesToRepeat * texture.Width, timesToRepeat * texture.Height);

            SpriteManager.Self.Add(mBackgroundSprite);
        }

        private void HandleXnaUpdate()
        {
        }



        void HandleControlZoomChange(object sender, EventArgs e)
        {
            Renderer.Self.Camera.Zoom = mEditControl.PercentageValue/100.0f;
        }

        private void ClearAll()
        {
            foreach (LineRectangle rectangle in mLineRectangles)
            {
                ShapeManager.Self.Remove(rectangle);
            }
            mLineRectangles.Clear();

            foreach (Sprite sprite in mSprites)
            {
                SpriteManager.Self.Remove(sprite);
            }
            mSprites.Clear();

            foreach (Text text in mTexts)
            {
                TextManager.Self.Remove(text);
            }
            mTexts.Clear();

            foreach (SolidRectangle solidRectangle in mSolidRectangles)
            {
                ShapeManager.Self.Remove(solidRectangle);
            }
            mSolidRectangles.Clear();

            foreach (NineSlice nineSlice in mNineSlices)
            {
                SpriteManager.Self.Remove(nineSlice);
            }
            mNineSlices.Clear();
        }

        public void RefreshAll(bool force)
        {
            ElementSave elementSave = SelectedState.Self.SelectedElement;

            RefreshAll(force, elementSave);

            SelectionManager.Self.Refresh();

            mWireframeControl.UpdateWireframeToProject();
        }

        public void RefreshAll(bool force, ElementSave elementSave)
        {

            if (elementSave == null)
            {
                ClearAll();
            }

            else if (elementSave != null && (force || elementSave != ElementShowing))
            {

                ClearAll();

                LoaderManager.Self.CacheTextures = false;
                LoaderManager.Self.CacheTextures = true;

                IPositionedSizedObject rootIpso = null;

                if ((elementSave is ScreenSave) == false)
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
                foreach(var instance in elementSave.Instances)

                    {
                        IPositionedSizedObject child = CreateRepresentationForInstance(instance, null, elementStack, rootIpso);
                    }
                //);



                elementStack.Remove( elementStack.FirstOrDefault(item=>item.Element == elementSave));

            }
            ElementShowing = elementSave;
        }

        public IPositionedSizedObject GetSelectedRepresentation()
        {
            if (!SelectionManager.Self.HasSelection)
            {
                return null;
            }
            else if (SelectedState.Self.SelectedInstance != null)
            {
                return GetRepresentation(SelectedState.Self.SelectedInstance);
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                return GetRepresentation(SelectedState.Self.SelectedElement);
            }
            else
            {
                throw new Exception("The SelectionManager believes it has a selection, but there is no selected instance or element");
            }
        }

        public IPositionedSizedObject GetRepresentation(ElementSave elementSave)
        {
#if DEBUG
            if (elementSave == null)
            {
                throw new NullReferenceException("The argument elementSave is null");
            }
#endif
            foreach (IPositionedSizedObject ipso in AllIpsos)
            {
                if (ipso.Tag == elementSave)
                {
                    return ipso;
                }
            }

            return null;
        }

        public IPositionedSizedObject GetRepresentation(InstanceSave instanceSave)
        {
            if (instanceSave != null)
            {
                foreach (IPositionedSizedObject ipso in AllIpsos)
                {
                    if (ipso.Tag == instanceSave)
                    {
                        return ipso;
                    }
                }
            }
            return null;
        }
        
        public Text GetText(InstanceSave instanceSave)
        {
            foreach (Text text in mTexts)
            {
                if (text.Name == instanceSave.Name)
                {
                    return text;
                }
            }

            return null;

        }

        public Text GetText(ElementSave elementSave)
        {
            foreach (Text text in mTexts)
            {
                if (text.Name == elementSave.Name)
                {
                    return text;
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
        public InstanceSave GetInstance(IPositionedSizedObject representation, InstanceFetchType fetchType)
        {
            ElementSave selectedElement = SelectedState.Self.SelectedElement;


            string prefix = selectedElement.Name + ".";
            if (selectedElement is ScreenSave)
            {
                prefix = "";
            }

            return GetInstance(representation, selectedElement, prefix, fetchType);

        }

        public InstanceSave GetInstance(IPositionedSizedObject representation, ElementSave instanceContainer, string prefix, InstanceFetchType fetchType)
        {
            if (instanceContainer == null)
            {
                return null;
            }

            InstanceSave toReturn = null;

            string qualifiedName = representation.GetAttachmentQualifiedName();

            // strip off the guide name if it starts with a guide
            qualifiedName = StripGuideName(qualifiedName);
            

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

                    toReturn = GetInstance(representation, instanceElement, prefix + instanceSave.Name + ".", fetchType);

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
                }
            }

            return toReturn;
        }

        private string StripGuideName(string qualifiedName)
        {
            foreach (NamedRectangle rectangle in ObjectFinder.Self.GumProjectSave.Guides)
            {
                if (qualifiedName.StartsWith(rectangle.Name + "."))
                {
                    return qualifiedName.Substring(rectangle.Name.Length + 1);
                }
            }

            return qualifiedName;
        }


        public bool IsRepresentation(IPositionedSizedObject ipso)
        {
            return mLineRectangles.Contains(ipso) || mSprites.Contains(ipso) || 
                mTexts.Contains(ipso) || mSolidRectangles.Contains(ipso) ||
                mNineSlices.Contains(ipso) ;
        }

        public ElementSave GetElement(IPositionedSizedObject representation)
        {
            if (SelectedState.Self.SelectedElement != null && 
                SelectedState.Self.SelectedElement.Name == representation.Name)
            {
                return SelectedState.Self.SelectedElement;
            }

            return null;
        }

        public T GetIpsoAt<T>(float x, float y, IList<T> list) where T : IPositionedSizedObject
        {
            foreach(T ipso in list)
            {
                if (ipso.HasCursorOver(x, y))
                {
                    return ipso;
                }
            }
            return default(T);
        }


        internal void UpdateScalesAndPositionsForSelectedChildren()
        {
            List<ElementWithState> elementStack = new List<ElementWithState>();
            elementStack.Add( new ElementWithState( SelectedState.Self.SelectedElement ));
            foreach (IPositionedSizedObject selectedIpso in SelectionManager.Self.SelectedIpsos)
            {
                UpdateScalesAndPositionsForSelectedChildren(selectedIpso, selectedIpso.Tag as InstanceSave, elementStack);
            }
        }

        void UpdateScalesAndPositionsForSelectedChildren(IPositionedSizedObject ipso, InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            ElementSave selectedElement = null;

            if (instanceSave == null)
            {
                selectedElement = elementStack.Last().Element;
            }
            else
            {
                selectedElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                ElementWithState elementWithState = new ElementWithState(selectedElement);
                var state = new DataTypes.RecursiveVariableFinder(instanceSave, elementStack).GetValue("State") as string;
                elementWithState.StateName = state;
                //elementWithState.StateName 
                elementStack.Add( elementWithState);
            }
            foreach (IPositionedSizedObject child in ipso.Children)
            {
                InstanceSave childInstance = GetInstance(child, InstanceFetchType.DeepInstance);
                if (childInstance == null)
                {
                    continue;
                }

                StateSave stateSave = new StateSave();
                RecursiveVariableFinder rvf = new RecursiveVariableFinder(childInstance, elementStack);
                FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);

                List<VariableSave> exposedVariables = GetExposedVariablesForThisInstance(childInstance, instanceSave, elementStack);
                foreach (VariableSave variable in exposedVariables)
                {
                    stateSave.SetValue(variable.Name, variable.Value);
                }

                SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(child, selectedElement, stateSave);



                UpdateScalesAndPositionsForSelectedChildren(child, childInstance, elementStack);
            }
            elementStack.Remove(selectedElement);
        }
    }
}
