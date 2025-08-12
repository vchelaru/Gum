using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
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

namespace TextureCoordinateSelectionPlugin;

[Export(typeof(PluginBase))]
public class MainTextureCoordinatePlugin : PluginBase
{
    #region Fields/Properties

    PluginTab textureCoordinatePluginTab;
    ISelectedState _selectedState;

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
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        this.textureCoordinatePluginTab?.Hide();

        return true;
    }

    public override void StartUp()
    {
        textureCoordinatePluginTab = Logic.ControlLogic.Self.CreateControl();
        textureCoordinatePluginTab.Hide();

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;

        this.VariableSetLate += HandleVariableSet;
        // This is needed for when undos happen
        this.WireframeRefreshed += HandleWireframeRefreshed;
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

    private void HandleTreeNodeSelected(TreeNode treeNode)
    {
        RefreshControl();
    }

    private void RefreshControl()
    {
        Texture2D textureToAssign = GetTextureToAssign(out bool isNineslice, out float? customFrameTextureCoordinateWidth);

        Logic.ControlLogic.Self.Refresh(textureToAssign, isNineslice, customFrameTextureCoordinateWidth);
    }

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
    {
        var shouldRefresh = true;

        if(shouldRefresh)
        {
            RefreshControl();
            Logic.ControlLogic.Self.RefreshSelector(Logic.RefreshType.Force);
        }
    }



    private Texture2D GetTextureToAssign(out bool isNineslice, out float? customFrameTextureCoordinateWidth)
    {
        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;
        isNineslice = false;
        customFrameTextureCoordinateWidth = null;
        Texture2D textureToAssign = null;

        if (graphicalUiElement != null)
        {
            var containedRenderable = graphicalUiElement.RenderableComponent;

            if (containedRenderable is Sprite)
            {
                var sprite = containedRenderable as Sprite;

                textureToAssign = sprite.Texture;
            }
            else if (containedRenderable is NineSlice)
            {
                var nineSlice = containedRenderable as NineSlice;
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
        }

        if (textureToAssign?.IsDisposed == true)
        {
            textureToAssign = null;
        }

        return textureToAssign;
    }
}
