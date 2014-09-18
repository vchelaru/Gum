using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public enum BeginType
    {
        Begin,
        Push
    }

    #region BeginParameters class

    public class BeginParameters
    {
        public SpriteSortMode SortMode { get; set; }
        public BlendState BlendState { get; set; }
        public SamplerState SamplerState { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public Effect Effect { get; set; }
        public Matrix TransformMatrix { get; set; }
        public Rectangle ScissorRectangle { get; set; }
        public BeginParameters Clone()
        {
            return this.MemberwiseClone() as BeginParameters;
        }
    }

    #endregion

    public class SpriteBatchStack
    {
        #region Fields

        List<BeginParameters> mStateStack = new List<BeginParameters>();
        BeginParameters mLastParameters;

        #endregion

        public SpriteBatch SpriteBatch
        {
            get;
            private set;
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                return SpriteBatch.GraphicsDevice;
            }
        }

        public SpriteBatchStack(GraphicsDevice graphicsDevice)
        {
            SpriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void Push(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, 
            DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, 
            Matrix transformMatrix, Rectangle scissorRectangle)
        {


            mStateStack.Add(mLastParameters);

            // begin will end 
            Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix, scissorRectangle);
        }

        public void Pop()
        {
            mLastParameters = mStateStack.Last();
            mStateStack.RemoveAt(mStateStack.Count - 1);

            
            if (mLastParameters != null)
            {
                Begin(mLastParameters.SortMode, mLastParameters.BlendState, mLastParameters.SamplerState, mLastParameters.DepthStencilState,
                    mLastParameters.RasterizerState, mLastParameters.Effect, mLastParameters.TransformMatrix, mLastParameters.ScissorRectangle);
            }
            else
            {
                // this is the end
                End();
            }
        }

        public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState,
            DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix,
            Rectangle scissorRectangle)
        {
            bool isNewRender = mLastParameters == null;

            mLastParameters = new BeginParameters();
            
            mLastParameters.SortMode = sortMode;
            mLastParameters.BlendState = blendState;
            mLastParameters.SamplerState = samplerState;
            mLastParameters.DepthStencilState = depthStencilState;
            mLastParameters.RasterizerState = rasterizerState;
            mLastParameters.Effect = effect;
            mLastParameters.TransformMatrix = transformMatrix;
            mLastParameters.ScissorRectangle = scissorRectangle;
            if (!isNewRender)
            {
                SpriteBatch.End();
            }

            SpriteBatch.GraphicsDevice.ScissorRectangle = scissorRectangle;

            SpriteBatch.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);

        }

        void TryEnd()
        {
            if (mLastParameters != null)
            {
                End();
                mLastParameters = null;
            }
        }

        void End()
        {
            SpriteBatch.End();
        }
    }
}
