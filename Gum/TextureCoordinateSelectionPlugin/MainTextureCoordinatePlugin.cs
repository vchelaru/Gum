using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Services;
using TextureCoordinateSelectionPlugin.Logic;
using TextureCoordinateSelectionPlugin.ViewModels;

namespace TextureCoordinateSelectionPlugin;

[Export(typeof(PluginBase))]
public class MainTextureCoordinatePlugin : PluginBase, IRecipient<UiBaseFontSizeChangedMessage>
{
    #region Fields/Properties

    PluginTab textureCoordinatePluginTab;
    ISelectedState _selectedState;
    ControlLogic _controlLogic;
    MainControlViewModel _viewModel;

    public override string FriendlyName
    {
        get
        {
            return "Texture Coordinate Selection Plugin";
        }
    }

    public override Version Version
    {
        get => new Version(1, 0, 0);
    }

    #endregion

    public MainTextureCoordinatePlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();

        _viewModel = new (
            Locator.GetRequiredService<IProjectManager>(),
            Locator.GetRequiredService<IFileCommands>(),
            Locator.GetRequiredService<IFileWatchManager>(),
            Locator.GetRequiredService<IGuiCommands>());

        Locator.GetRequiredService<IMessenger>().RegisterAll(this);

        _controlLogic = new ControlLogic(
            Locator.GetRequiredService<ISelectedState>(),
            Locator.GetRequiredService<IUndoManager>(),
            Locator.GetRequiredService<IGuiCommands>(),
            Locator.GetRequiredService<IFileCommands>(),
            Locator.GetRequiredService<ISetVariableLogic>(),
            Locator.GetRequiredService<ITabManager>(),
            Locator.GetRequiredService<IHotkeyManager>(),
            new ScrollBarLogicWpf(),
            _viewModel);
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        if (textureCoordinatePluginTab is not null)
        {
            RemoveTab(textureCoordinatePluginTab);
        }

        _controlLogic?.Dispose();

        return true;
    }

    public override void StartUp()
    {
        textureCoordinatePluginTab = _controlLogic.CreateControl();
        textureCoordinatePluginTab.Hide();
        textureCoordinatePluginTab.GotFocus += HandleTabShown;

        AssignEvents();
    }

    private void HandleTabShown()
    {
        _controlLogic.CenterCameraOnSelection();
    }

    void IRecipient<UiBaseFontSizeChangedMessage>.Receive(UiBaseFontSizeChangedMessage message)
    {
        _controlLogic.UpdateButtonSizes(message.Size);
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;

        this.VariableSetLate += HandleVariableSet;
        // This is needed for when undos happen
        this.WireframeRefreshed += HandleWireframeRefreshed;

        this.ProjectLoad += HandleProjectLoaded;
    }

    private void HandleProjectLoaded(GumProjectSave save)
    {
        _viewModel.LoadSettings();
    }

    private void HandleWireframeRefreshed()
    {
        var element = _selectedState.SelectedElement;
        if(_selectedState.SelectedInstance != null)
        {
            element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance);
        }

        var hasTextureCoordinates = false;

        // Sprite and NineSlice have texture coords:
        if(element != null)
        {
            if(element is StandardElementSave)
            {
                hasTextureCoordinates = element.Name == "Sprite" || element.Name == "NineSlice";
            }
            else
            {
                var baseElements = ObjectFinder.Self.GetBaseElements(element);

                hasTextureCoordinates = baseElements.Any(item =>
                {
                    return element is StandardElementSave && (element.Name == "Sprite" || element.Name == "NineSlice");
                });
            }
        }

        // Check for exposed texture coordinate variables on the selected instance's type
        if (!hasTextureCoordinates && _selectedState.SelectedInstance != null && element != null)
        {
            hasTextureCoordinates = RefreshExposedTextureCoordinateInfo(element);
        }
        else
        {
            ResetExposedTextureCoordinateInfo();
        }

        if(hasTextureCoordinates)
        {
            textureCoordinatePluginTab.Show();
            RefreshControl();
        }
        else
        {
            textureCoordinatePluginTab.Hide();
        }
    }

    private bool RefreshExposedTextureCoordinateInfo(ElementSave element)
    {
        _controlLogic.IsExposedMode = false;
        _controlLogic.ExposedSourceObjectName = null;
        _controlLogic.ExposedLeftName = null;
        _controlLogic.ExposedTopName = null;
        _controlLogic.ExposedWidthName = null;
        _controlLogic.ExposedHeightName = null;

        var state = element.DefaultState;
        string? sourceObject = null;

        foreach (var variable in state.Variables)
        {
            if (string.IsNullOrEmpty(variable.ExposedAsName)) continue;
            if (string.IsNullOrEmpty(variable.SourceObject)) continue;

            var rootName = variable.GetRootName();

            bool isTextureCoordinate = rootName == "TextureLeft" || rootName == "TextureTop" ||
                                       rootName == "TextureWidth" || rootName == "TextureHeight";

            if (!isTextureCoordinate) continue;

            // If we haven't determined the source object yet, validate it's a Sprite or NineSlice
            if (sourceObject == null)
            {
                var instance = element.Instances.FirstOrDefault(i => i.Name == variable.SourceObject);
                if (instance != null)
                {
                    var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                    bool isSpriteOrNineSlice = false;
                    if (instanceElement is StandardElementSave ses)
                    {
                        isSpriteOrNineSlice = ses.Name == "Sprite" || ses.Name == "NineSlice";
                    }
                    else if (instanceElement != null)
                    {
                        var innerBaseElements = ObjectFinder.Self.GetBaseElements(instanceElement);
                        isSpriteOrNineSlice = innerBaseElements.Any(b =>
                            b is StandardElementSave bs && (bs.Name == "Sprite" || bs.Name == "NineSlice"));
                    }

                    if (isSpriteOrNineSlice)
                    {
                        sourceObject = variable.SourceObject;
                    }
                }
            }

            if (variable.SourceObject == sourceObject)
            {
                switch (rootName)
                {
                    case "TextureLeft":
                        _controlLogic.ExposedLeftName = variable.ExposedAsName;
                        break;
                    case "TextureTop":
                        _controlLogic.ExposedTopName = variable.ExposedAsName;
                        break;
                    case "TextureWidth":
                        _controlLogic.ExposedWidthName = variable.ExposedAsName;
                        break;
                    case "TextureHeight":
                        _controlLogic.ExposedHeightName = variable.ExposedAsName;
                        break;
                }
            }
        }

        bool hasAny = _controlLogic.ExposedLeftName != null || _controlLogic.ExposedTopName != null ||
                      _controlLogic.ExposedWidthName != null || _controlLogic.ExposedHeightName != null;

        if (hasAny)
        {
            _controlLogic.IsExposedMode = true;
            _controlLogic.ExposedSourceObjectName = sourceObject;
        }

        return hasAny;
    }

    private void ResetExposedTextureCoordinateInfo()
    {
        _controlLogic.IsExposedMode = false;
        _controlLogic.ExposedSourceObjectName = null;
        _controlLogic.ExposedLeftName = null;
        _controlLogic.ExposedTopName = null;
        _controlLogic.ExposedWidthName = null;
        _controlLogic.ExposedHeightName = null;
    }

    private void HandleTreeNodeSelected(TreeNode? treeNode)
    {
        RefreshControl();

        _controlLogic.CenterCameraOnSelection();
    }

    private void RefreshControl()
    {
        Texture2D? textureToAssign = GetTextureToAssign(out bool isNineslice, out float? customFrameTextureCoordinateWidth);

        _controlLogic.Refresh(textureToAssign, isNineslice, customFrameTextureCoordinateWidth);
    }

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
    {
        var shouldRefresh = true;

        if(shouldRefresh)
        {
            RefreshControl();
            _controlLogic.RefreshSelector(Logic.RefreshType.Force);
        }
    }



    private Texture2D? GetTextureToAssign(out bool isNineslice, out float? customFrameTextureCoordinateWidth)
    {
        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;
        isNineslice = false;
        customFrameTextureCoordinateWidth = null;
        Texture2D? textureToAssign = null;

        if (graphicalUiElement != null)
        {
            var containedRenderable = graphicalUiElement.RenderableComponent;

            if (containedRenderable is Sprite asSprite)
            {
                textureToAssign = asSprite.Texture;
            }
            else if (containedRenderable is NineSlice nineSlice)
            {
                isNineslice = true;
                customFrameTextureCoordinateWidth = nineSlice.CustomFrameTextureCoordinateWidth;
                var isUsingSameTextures =
                    nineSlice.TopLeftTexture == nineSlice.CenterTexture &&
                    nineSlice.TopTexture == nineSlice.CenterTexture &&
                    nineSlice.TopRightTexture == nineSlice.CenterTexture &&

                    nineSlice.LeftTexture == nineSlice.CenterTexture &&
                    //nineSlice.TopLeftTexture ==
                    nineSlice.RightTexture == nineSlice.CenterTexture &&

                    nineSlice.BottomLeftTexture == nineSlice.CenterTexture &&
                    nineSlice.BottomTexture == nineSlice.CenterTexture &&
                    nineSlice.BottomRightTexture == nineSlice.CenterTexture;

                if (isUsingSameTextures)
                {
                    textureToAssign = nineSlice.CenterTexture;
                }
            }

            // For exposed mode, find the inner child's texture
            if (textureToAssign == null && _controlLogic.IsExposedMode && _controlLogic.ExposedSourceObjectName != null)
            {
                var innerChild = graphicalUiElement.Children
                    .FirstOrDefault(c => c.Name == _controlLogic.ExposedSourceObjectName);

                if (innerChild is GraphicalUiElement innerGue)
                {
                    var innerRenderable = innerGue.RenderableComponent;
                    if (innerRenderable is Sprite innerSprite)
                    {
                        textureToAssign = innerSprite.Texture;
                    }
                    else if (innerRenderable is NineSlice innerNineSlice)
                    {
                        isNineslice = true;
                        customFrameTextureCoordinateWidth = innerNineSlice.CustomFrameTextureCoordinateWidth;
                        var isUsingSameTextures =
                            innerNineSlice.TopLeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.TopTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.TopRightTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.LeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.RightTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomLeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomRightTexture == innerNineSlice.CenterTexture;

                        if (isUsingSameTextures)
                        {
                            textureToAssign = innerNineSlice.CenterTexture;
                        }
                    }
                }
            }
        }

        if (textureToAssign?.IsDisposed == true)
        {
            textureToAssign = null;
        }

        return textureToAssign;
    }
}
