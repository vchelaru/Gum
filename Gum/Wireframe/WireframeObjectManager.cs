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
using System.Windows.Forms;
using Gum.Plugins;

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

        List<GraphicalUiElement> mGraphicalElements = new List<GraphicalUiElement>();

        WireframeEditControl mEditControl;
        WireframeControl mWireframeControl;

        Sprite mBackgroundSprite;

        const int left = -4096;
        const int width = 8192;

        GraphicalUiElementManager gueManager;


        #endregion

        #region Properties

        public List<GraphicalUiElement> AllIpsos
        {
            get
            {
                return mGraphicalElements;
                //foreach (Sprite sprite in mSprites)
                //{
                //    yield return sprite;
                //}

                //foreach (Text text in mTexts)
                //{
                //    yield return text;
                //}
                //foreach (LineRectangle rectangle in mLineRectangles)
                //{
                //    yield return rectangle;
                //}
                //foreach (SolidRectangle solidRectangle in mSolidRectangles)
                //{
                //    yield return solidRectangle;
                //}
                //foreach (NineSlice nineSlice in mNineSlices)
                //{
                //    yield return nineSlice;
                //}
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

        public GraphicalUiElement RootGue
        {
            get;
            private set;
        }

        #endregion

        #region Initialize

        public void Initialize(WireframeEditControl editControl, WireframeControl wireframeControl)
        {
            mWireframeControl = wireframeControl;
            mWireframeControl.AfterXnaInitialize += HandleAfterXnaIntiailize;

            mWireframeControl.KeyDown += HandleKeyPress;

            mEditControl = editControl;
            mEditControl.ZoomChanged += HandleControlZoomChange;

            gueManager = new GraphicalUiElementManager();
        }

        #endregion

        #region Activity

        public void Activity()
        {
            gueManager.Activity();
        }

        #endregion

        private void HandleKeyPress(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            CameraController.Self.HandleKeyPress(e);
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
            mBackgroundSprite.Name = "Background checkerboard Sprite";
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


        void HandleControlZoomChange(object sender, EventArgs e)
        {
            Renderer.Self.Camera.Zoom = mEditControl.PercentageValue / 100.0f;
        }

        private void ClearAll()
        {
            foreach (var element in mGraphicalElements)
            {
                gueManager.Remove(element);

                element.RemoveFromManagers();
            }

            mGraphicalElements.Clear();
        }

        public void RefreshAll(bool forceLayout, bool forceReloadTextures = false)
        {
            ElementSave elementSave = SelectedState.Self.SelectedElement;

            RefreshAll(forceLayout, forceReloadTextures, elementSave);

            SelectionManager.Self.Refresh();

            mWireframeControl.UpdateToProject();

            PluginManager.Self.WireframeRefreshed();
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
            else if (forceLayout || forceReloadTextures)
            {
                ClearAll();

                if(forceReloadTextures)
                {
                    ((ContentLoader)LoaderManager.Self.ContentLoader).DisposeAndClear();
                    LoaderManager.Self.CacheTextures = false;
                }

                LoaderManager.Self.CacheTextures = true;

                RootGue = CreateIpsoForElement(elementSave);

            }
            ElementShowing = elementSave;

        }



        public GraphicalUiElement GetSelectedRepresentation()
        {
            if (!SelectionManager.Self.HasSelection)
            {
                return null;
            }
            else if (SelectedState.Self.SelectedInstance != null)
            {
                return GetRepresentation(SelectedState.Self.SelectedInstance, SelectedState.Self.GetTopLevelElementStack());
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
                    IEnumerable<IPositionedSizedObject> currentChildren = WireframeObjectManager.Self.AllIpsos.Where(item => item.Parent == null);

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
            ElementSave selectedElement = SelectedState.Self.SelectedElement;

            string prefix = selectedElement.Name + ".";
            if (selectedElement is ScreenSave)
            {
                prefix = "";
            }

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


        public bool IsRepresentation(IPositionedSizedObject ipso)
        {
            return mGraphicalElements.Contains(ipso);
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

        public T GetIpsoAt<T>(float x, float y, IList<T> list) where T : IRenderableIpso
        {
            foreach (T ipso in list)
            {
                if (ipso.HasCursorOver(x, y))
                {
                    return ipso;
                }
            }
            return default(T);
        }

               
        private static bool TryAddToElementStack(InstanceSave instanceSave, List<ElementWithState> elementStack, out ElementSave selectedElement)
        {
            bool toReturn = false;
            if (instanceSave == null)
            {
                selectedElement = elementStack.Last().Element;
            }
            else
            {
                selectedElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                if (elementStack.Count == 0 || elementStack.Last().Element != selectedElement)
                {

                    ElementWithState elementWithState = new ElementWithState(selectedElement);
                    var state = new DataTypes.RecursiveVariableFinder(instanceSave, elementStack).GetValue("State") as string;
                    elementWithState.StateName = state;
                    elementStack.Add(elementWithState);
                    toReturn = true;
                }
            }
            return toReturn;
        }

        private void GetRequiredDimensionsFromContents(IRenderableIpso parentIpso, out float requiredWidth, out float requiredHeight)
        {
            requiredWidth = 0;
            requiredHeight = 0;
            foreach (var child in parentIpso.Children)
            {
                requiredWidth = System.Math.Max(requiredWidth, child.X + child.Width);
                requiredHeight = System.Math.Max(requiredHeight, child.Y + child.Height);
            }
        }

    }
}
