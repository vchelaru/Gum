using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
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

namespace TextureCoordinateSelectionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainTextureCoordinatePlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName
        {
            get
            {
                return "Texture Coordinate Selection Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            // todo - hide the window
            return true;
        }

        public override void StartUp()
        {
            Logic.ControlLogic.Self.CreateControl();


            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.VariableSet += HandleVariableSet;
            // This is needed for when undos happen
            this.WireframeRefreshed += HandleWireframeRefreshed;
        }

        private void HandleWireframeRefreshed()
        {
            RefreshControl();
        }

        private void HandleTreeNodeSelected(TreeNode treeNode)
        {
            RefreshControl();
        }

        private void RefreshControl()
        {
            Texture2D textureToAssign = GetTextureToAssign();

            Logic.ControlLogic.Self.Refresh(textureToAssign);
        }

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
        {
            var shouldRefresh = true;

            if(shouldRefresh)
            {
                Logic.ControlLogic.Self.RefreshSelector(Logic.RefreshType.Force);
            }
        }



        private static Texture2D GetTextureToAssign()
        {
            var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;

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
}
