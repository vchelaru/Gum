using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Managers;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary;
using Gum.RenderingLibrary;
using FlatRedBall.AnimationEditorForms.Controls;
using Gum.Plugins;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using RenderingLibrary.Math.Geometry;

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

        WireframeEditControl mEditControl;
        public WireframeControl WireframeControl { get; private set; }

        public Sprite BackgroundSprite { get; private set; }

        const int left = -4096;
        const int width = 8192;

        GraphicalUiElementManager gueManager;


        #endregion

        #region Properties

        public List<GraphicalUiElement> AllIpsos { get; private set; } = new List<GraphicalUiElement>();

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

        public System.Windows.Forms.Cursor AddCursor { get; private set; }

        #endregion

        #region Initialize

        public void Initialize(WireframeEditControl editControl, WireframeControl wireframeControl, System.Windows.Forms.Cursor addCursor)
        {
            AddCursor = addCursor;

            WireframeControl = wireframeControl;
            WireframeControl.AfterXnaInitialize += HandleAfterXnaIntiailize;

            WireframeControl.KeyDown += HandleKeyPress;

            mEditControl = editControl;
            mEditControl.ZoomChanged += HandleControlZoomChange;

            gueManager = new GraphicalUiElementManager();
            GraphicalUiElement.AreUpdatesAppliedWhenInvisible= true;

            ElementSaveExtensions.CustomCreateGraphicalComponentFunc = HandleCreateGraphicalComponent;
        }

        private IRenderable HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
        {

            IRenderable containedObject = null;

            TryHandleAsBaseType(type, systemManagers as SystemManagers, out containedObject);

#if GUM
            if (containedObject == null)
            {
                containedObject =
                    Gum.Plugins.PluginManager.Self.CreateRenderableForType(type);
            }
#endif

            return containedObject;
        }


        internal static void TryHandleAsBaseType(string baseType, SystemManagers systemManagers, out IRenderable containedObject)
        {
            containedObject = null;
#if MONOGAME || FNA
            switch (baseType)
            {

                case "Container":
                case "Component": // this should never be set in Gum, but there could be XML errors or someone could have used an old Gum...
                    if (GraphicalUiElement.ShowLineRectangles)
                    {
                        LineRectangle lineRectangle = new LineRectangle(systemManagers);
                        lineRectangle.Color = System.Drawing.Color.FromArgb(
#if GUM
                            255,
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorR,
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorG,
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorB
#else
                        255,255,255,255
#endif
                            );

                        containedObject = lineRectangle;
                    }
                    else
                    {
                        containedObject = new InvisibleRenderable();
                    }
                    break;

                case "Rectangle":
                    LineRectangle rectangle = new LineRectangle(systemManagers);
                    rectangle.IsDotted = false;
                    containedObject = rectangle;
                    break;
                case "Circle":
                    LineCircle circle = new LineCircle(systemManagers);
                    circle.CircleOrigin = CircleOrigin.TopLeft;
                    containedObject = circle;
                    break;
                case "Polygon":
                    LinePolygon polygon = new LinePolygon(systemManagers);
                    containedObject = polygon;
                    break;
                case "ColoredRectangle":
                    SolidRectangle solidRectangle = new SolidRectangle();
                    containedObject = solidRectangle;
                    break;
                case "Sprite":
                    Texture2D texture = null;

                    Sprite sprite = new Sprite(texture);
                    containedObject = sprite;

                    break;
                case "NineSlice":
                    {
                        NineSlice nineSlice = new NineSlice();
                        containedObject = nineSlice;
                    }
                    break;
                case "Text":
                    {
                        Text text = new Text(systemManagers, "");
                        containedObject = text;
                    }
                    break;
            }
#endif
        }

        #endregion

        #region Activity

        public void Activity()
        {
            gueManager.Activity();

            if(ProjectManager.Self.GeneralSettingsFile != null) {
                BackgroundSprite.Color = System.Drawing.Color.FromArgb(255,
                    ProjectManager.Self.GeneralSettingsFile.CheckerColor2R,
                    ProjectManager.Self.GeneralSettingsFile.CheckerColor2G,
                    ProjectManager.Self.GeneralSettingsFile.CheckerColor2B
                );

            }
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

            Microsoft.Xna.Framework.Color opaqueColor = Microsoft.Xna.Framework.Color.White;
            Microsoft.Xna.Framework.Color transparent = new Microsoft.Xna.Framework.Color(0,0,0,0);

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    bool isDark = ((x + y) % 2 == 0);
                    if (isDark)
                    {
                        imageData.SetPixel(x, y, transparent);

                    }
                    else
                    {
                        imageData.SetPixel(x, y, opaqueColor);
                    }
                }
            }

            Texture2D texture = imageData.ToTexture2D(false);
            texture.Name = "Background Checkerboard";
            BackgroundSprite = new Sprite(texture);
            BackgroundSprite.Name = "Background checkerboard Sprite";
            BackgroundSprite.Wrap = true;
            BackgroundSprite.X = -4096;
            BackgroundSprite.Y = -4096;
            BackgroundSprite.Width = 8192;
            BackgroundSprite.Height = 8192;
            BackgroundSprite.Color = System.Drawing.Color.FromArgb(255, 150, 150, 150);

            BackgroundSprite.Wrap = true;
            int timesToRepeat = 256;
            BackgroundSprite.SourceRectangle = new Rectangle(0, 0, timesToRepeat * texture.Width, timesToRepeat * texture.Height);

            SpriteManager.Self.Add(BackgroundSprite);
        }


        void HandleControlZoomChange(object sender, EventArgs e)
        {
            Renderer.Self.Camera.Zoom = mEditControl.PercentageValue / 100.0f;
        }

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
            if(SelectedState.Self.SelectedElements.Count == 1)
            {
                elementSave = SelectedState.Self.SelectedElement;
            }


            RefreshAll(forceLayout, forceReloadTextures, elementSave);

            SelectionManager.Self.Refresh();

            WireframeControl.UpdateCanvasBoundsToProject();

            PluginManager.Self.WireframeRefreshed();
        }

        public void RefreshGuides()
        {
            WireframeControl.RefreshGuides();
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
                ObjectFinder.Self.EnableCache();
                {
                    ClearAll();

                    if(forceReloadTextures)
                    {
                        ((ContentLoader)LoaderManager.Self.ContentLoader).DisposeAndClear();
                        LoaderManager.Self.CacheTextures = false;
                    }

                    LoaderManager.Self.CacheTextures = true;

                    var useNew = true;
                    if(useNew)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = true;

                        RootGue = elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
                        // Always set default first, then if the selected state is not the default, then apply that after:
                        RootGue.SetVariablesRecursively(elementSave, elementSave.DefaultState);
                        var selectedState = GumState.Self.SelectedState.SelectedStateSave;
                        if(selectedState != null && selectedState != elementSave.DefaultState)
                        {
                            RootGue.ApplyState(selectedState);
                        }


                        AddAllIpsos(RootGue);
                        HashSet<GraphicalUiElement> hashSet = new HashSet<GraphicalUiElement>();
                        var tempSorted = AllIpsos.OrderBy(item =>
                        {
                            hashSet.Clear();
                            return GetDepth(item, hashSet);
                        }).ToArray();

                        AllIpsos.Clear();
                        AllIpsos.AddRange(tempSorted);

                        UpdateTextOutlines(RootGue);

                        GraphicalUiElement.IsAllLayoutSuspended = false;

                        RootGue.UpdateFontRecursive();
                        RootGue.UpdateLayout();


                        // what about fonts?
                        // We recreate missing fonts on startup, so do we need to bother here?
                        // I'm not sure, but if we do we would call:
                        //FontManager.Self.CreateAllMissingFontFiles(ObjectFinder.Self.GumProjectSave);
                    }
                    else
                    {
                        RootGue = CreateIpsoForElement(elementSave);

                    }

                    if(LocalizationManager.HasDatabase)
                    {
                        ApplyLocalization();
                    }
                }
                ObjectFinder.Self.DisableCache();
            }
            ElementShowing = elementSave;

        }

        private void UpdateTextOutlines(GraphicalUiElement rootGue)
        {
            if(rootGue.Component is Text text)
            {
                text.RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;
            }
            if(rootGue.Children != null)
            {
                foreach(var child in rootGue.Children)
                {
                    if(child is GraphicalUiElement gue)
                    {
                        UpdateTextOutlines(gue);
                    }
                }
            }
            else
            {
                foreach(var child in rootGue.ContainedElements)
                {
                    UpdateTextOutlines(child);
                }
            }
        }

        private void AddAllIpsos(GraphicalUiElement rootGue)
        {
            AllIpsos.Add(rootGue);
            foreach(var item in rootGue.ContainedElements)
            {
                AllIpsos.Add(item);
            }
        }

        public void ApplyLocalization()
        {
            if(LocalizationManager.HasDatabase == false)
            {
                throw new InvalidOperationException("Cannot apply localization - the LocalizationManager doesn't have a localization database loaded");
            }

            foreach(var textContainer in GetTextsRecurisve(RootGue))
            {
                ApplyLocalization(textContainer);
            }
        }


        public void ApplyLocalization(GraphicalUiElement gue, string forcedId = null)
        {
            var shouldLocalize = GumState.Self.ProjectState.GumProjectSave.ShowLocalizationInGum;
            //if(gue.Tag is InstanceSave instance)
            //{
            //    var rfv = new RecursiveVariableFinder(GumState.Self.SelectedState.SelectedStateSave);

            //    var value = rfv.GetValue<bool>(instance.Name + ".Apply Localization");
            //    isLocalized = value;
            //}

            if(shouldLocalize)
            {
                var stringId = forcedId;
                if(string.IsNullOrWhiteSpace(stringId) && gue.RenderableComponent is Text asText)
                {
                    stringId = asText.RawText;
                }
 
                // Go through the GraphicalUiElement to kick off a layout adjustment if necessary
                gue.SetProperty("Text", LocalizationManager.Translate(stringId));
            }
        }

        public IEnumerable<GraphicalUiElement> GetTextsRecurisve(GraphicalUiElement parent)
        {
            if(parent.RenderableComponent is Text)
            {
                yield return parent;
            }
            if(parent.Tag is ScreenSave)
            {
                // it won't have children, so go to the toplevel objects
                foreach(var child in parent.ContainedElements)
                {
                    if(child.Parent == null)
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

        public GraphicalUiElement[] GetSelectedRepresentations()
        {
            if (!SelectionManager.Self.HasSelection)
            {
                return null;
            }
            else if(SelectedState.Self.SelectedInstances.Count() > 0)
            {
                return SelectedState.Self.SelectedInstances
                    .Select(item => GetRepresentation(item, SelectedState.Self.GetTopLevelElementStack()))
                    .ToArray();
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                return new GraphicalUiElement[]
                {
                    GetRepresentation(SelectedState.Self.SelectedElement)
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


        public bool IsRepresentation(IPositionedSizedObject ipso) => AllIpsos.Contains(ipso);

        public ElementSave GetElement(IPositionedSizedObject representation)
        {
            if (SelectedState.Self.SelectedElement != null &&
                SelectedState.Self.SelectedElement.Name == representation.Name)
            {
                return SelectedState.Self.SelectedElement;
            }

            return null;
        }
    }
}
