using System;
using System.Collections.Generic;
using System.Linq;
using BlendState = Gum.BlendState;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;



#if APOS_SHAPES
using SpriteBatch = Apos.Shapes.ShapeBatch;
#else
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;

#endif


namespace RenderingLibrary.Graphics
{
    public struct StateChangeInfo
    {
        public Texture2D Texture;
        public SpriteFont SpriteFont;
        public object ObjectRequestingChange;
    }

    public enum BeginType
    {
        Begin,
        Push
    }

    #region BeginParameters class

    public struct BeginParameters
    {
        public bool IsDefault
        {
            get;
            set;
        }

        public SpriteSortMode SortMode { get; set; }
        public BlendState BlendState { get; set; }
        public SamplerState SamplerState { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public Effect Effect { get; set; }
        public Microsoft.Xna.Framework.Matrix TransformMatrix { get; set; }
        public Rectangle ScissorRectangle { get; set; }

        public object ObjectChangingState { get; set; }

        /// <summary>
        /// A list of changes that happened with these same parameters which required changing either the Texture or SpriteFont.
        /// If this list is empty, no items were drawn with a Texture/SpriteFont. If any items were drawn, at least one item is
        /// present in this list.
        /// </summary>
        public StateChangeInfoList ChangeRecord
        {
            get; set;
        }

        public SpriteFont SpriteFont { get; set; }

        public BeginParameters Clone()
        {
            return (BeginParameters)this.MemberwiseClone();
        }

        public override string ToString()
        {
            if(ObjectChangingState != null)
            {
                if(ChangeRecord.Count == 0)
                {
                    return $"By {ObjectChangingState}";
                }
                else
                {
                    return $"By {ObjectChangingState} w/ {ChangeRecord.Count} Textures set(s)";
                }
            }
            else
            {
                return $"Begin w/ {ChangeRecord.Count} Textures set(s)";
            }
        }
    }

    #endregion

    public class SpriteBatchStack
    {
        static StateChangeInfoListPool StateChangeInfoListPool = new StateChangeInfoListPool();

        public enum SpriteBatchBeginEndState
        {
            Ended,
            Began
        }


        #region Fields

        SpriteBatchBeginEndState beginEndState;

        public SpriteBatchBeginEndState BeginEndState => beginEndState;

        List<BeginParameters> beginParametersUsedThisFrame = new List<BeginParameters>();

        List<BeginParameters?> mStateStack = new List<BeginParameters?>();
        BeginParameters? currentParameters;
        public BeginParameters? CurrentParameters => currentParameters;
        #endregion

        #region Properties

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

        public List<BeginParameters> LastFrameDrawStates
        {
            get
            {
                List<BeginParameters> toReturn = new List<BeginParameters>();

                toReturn.AddRange(beginParametersUsedThisFrame);

                // The last parameters used for draw will not be part of beginParametersUsedThisFrame, so add it here:
                if (currentParameters != null)
                {
                    toReturn.Add(currentParameters.Value);
                }

                return toReturn;
            }
        }

        public int StackCount => mStateStack.Count;

        #endregion

        public SpriteBatchStack(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
#if APOS_SHAPES

            SpriteBatch = new Apos.Shapes.ShapeBatch(graphicsDevice, contentManager);
#else
            SpriteBatch = new SpriteBatch(graphicsDevice);
#endif
        }

        public static void PerformStartOfLayerRenderingLogic()
        {
            // Resets the pool so that the rendering can use lists without having to instantiate or expand the internal array.
            // This means that the internal StateChange info is only valid for that particular frame of rendering
            StateChangeInfoListPool.MakeAllUnused();
        }

        public void Begin(bool createNewParameters = true)
        {
            if(createNewParameters)
            {
                var beginParams = new BeginParameters();
                beginParams.ChangeRecord = StateChangeInfoListPool.GetNextAvailable();
                beginParams.ChangeRecord.Clear();

                beginParams.IsDefault = true;
                currentParameters = beginParams;
            }

            if (beginEndState == SpriteBatchBeginEndState.Began)
            {
                SpriteBatch.End();
            }

            beginEndState = SpriteBatchBeginEndState.Began;
            SpriteBatch.Begin();
        }

        public void PushRenderStates(SpriteSortMode sortMode, 
            BlendState blendState, SamplerState samplerState,
            DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect,
            Microsoft.Xna.Framework.Matrix transformMatrix, Rectangle scissorRectangle,
            object? objectChangingState)
        {


            mStateStack.Add(currentParameters);

            // begin will end 
            ReplaceRenderStates(
                sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix, scissorRectangle, objectChangingState);
        }

        public void ForceSetRenderStatesToCurrent()
        {
            if (currentParameters != null)
            {
                ReplaceRenderStates(currentParameters.Value.SortMode,
                    currentParameters.Value.BlendState,
                    currentParameters.Value.SamplerState,
                    currentParameters.Value.DepthStencilState,
                    currentParameters.Value.RasterizerState,
                    currentParameters.Value.Effect,
                    currentParameters.Value.TransformMatrix,
                    currentParameters.Value.ScissorRectangle,
                    currentParameters.Value.ObjectChangingState);
            }
        }

        public void ReplaceRenderStates(SpriteSortMode sortMode, 
            BlendState blendState, 
            SamplerState samplerState,
            DepthStencilState depthStencilState, RasterizerState rasterizerState, 
            Effect effect, Microsoft.Xna.Framework.Matrix transformMatrix,
            Rectangle scissorRectangle,
            object? objectChangingState)
        {
            bool isNewRender = currentParameters.HasValue == false;

            var newParameters = new BeginParameters();
            newParameters.ChangeRecord = StateChangeInfoListPool.GetNextAvailable();
            newParameters.ChangeRecord.Clear();

            newParameters.SortMode = sortMode;
            newParameters.BlendState = blendState;
            newParameters.SamplerState = samplerState;
            newParameters.DepthStencilState = depthStencilState;
            newParameters.RasterizerState = rasterizerState;
            newParameters.Effect = effect;
            newParameters.TransformMatrix = transformMatrix;
            newParameters.ObjectChangingState = objectChangingState;

            try
            {
                newParameters.ScissorRectangle = scissorRectangle;
            }
            catch (Exception e)
            {
                throw new Exception("Could not set scissor rectangle to:" + scissorRectangle.ToString(), e);
            }
            if (currentParameters != null)
            {
                beginParametersUsedThisFrame.Add(currentParameters.Value);
            }

            currentParameters = newParameters;

            if (beginEndState == SpriteBatchBeginEndState.Began)
            {
                SpriteBatch.End();
            }

            try
            {
                SpriteBatch.GraphicsDevice.ScissorRectangle = scissorRectangle.ToXNA();
            }
            catch (Exception e)
            {
                throw new Exception("Error trying to set scissor rectangle:" + scissorRectangle.ToString(), e);
            }
            beginEndState = SpriteBatchBeginEndState.Began;
            // assign here so that any other renderables that rely on scissor rects can use it
            SpriteBatch.GraphicsDevice.RasterizerState = 
                rasterizerState ?? RasterizerState.CullCounterClockwise;
            SpriteBatch.Begin(sortMode,
                blendState.ToXNA(),
                samplerState, depthStencilState, rasterizerState, effect, transformMatrix);

        }

        internal void Draw(Texture2D texture2D, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, object objectRequestingChange)
        {
            AdjustCurrentParametersDrawCall(texture2D, null, objectRequestingChange);

            var xnaColor = color.ToXNA();

            SpriteBatch.Draw(texture2D, destinationRectangle.ToXNA(), sourceRectangle.ToXNA(), xnaColor);
        }

        internal void Draw(Texture2D texture2D, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotationInRadians, Vector2 origin, SpriteEffects effects, int layerDepth, object objectRequestingChange)
        {
            AdjustCurrentParametersDrawCall(texture2D, null, objectRequestingChange);


            SpriteBatch.Draw(texture2D, destinationRectangle.ToXNA(), sourceRectangle?.ToXNA(), color.ToXNA(), rotationInRadians, origin.ToXNA(), effects, layerDepth);
        }

        internal void Draw(Texture2D texture2D, Vector2 position, Rectangle? sourceRectangle, Color color, float rotationInRadians, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth, object objectRequestingChange)
        {
            AdjustCurrentParametersDrawCall(texture2D, null, objectRequestingChange);

            SpriteBatch.Draw(texture2D, position.ToXNA(), sourceRectangle?.ToXNA(), color.ToXNA(), rotationInRadians, origin.ToXNA(), scale.ToXNA(), effects, depth);
        }

        internal void DrawString(SpriteFont font, string line, Vector2 offset, Color color, object objectRequestingChange)
        {
            AdjustCurrentParametersDrawCall(null, font, objectRequestingChange);

            SpriteBatch.DrawString(font, line, offset.ToXNA(), color.ToXNA());
        }

        private void AdjustCurrentParametersDrawCall(Texture2D texture, SpriteFont spriteFont, object objectRequestingChange)
        {
            var paramsValue = currentParameters.Value;

            bool shouldRecordChange = paramsValue.ChangeRecord.Count == 0;

            if (!shouldRecordChange)
            {
                var last = paramsValue.ChangeRecord.Last();

                shouldRecordChange = last.Texture != texture || last.SpriteFont != spriteFont;
            }

            if (shouldRecordChange)
            {
                var newChange = new StateChangeInfo();
                newChange.Texture = texture;
                newChange.SpriteFont = spriteFont;
                newChange.ObjectRequestingChange = objectRequestingChange;

                paramsValue.ChangeRecord.Add(newChange);
                currentParameters = paramsValue;
            }
        }

        //void TryEnd()
        //{
        //    if (currentParameters != null)
        //    {
        //        End();
        //    }
        //}

        internal void End()
        {

            if (currentParameters != null)
            {
                RecordCurrentParameters();

                if (beginEndState == SpriteBatchBeginEndState.Began)
                {
                    SpriteBatch.End();
                    beginEndState = SpriteBatchBeginEndState.Ended;
                }
            }
            else
            {

                if (beginEndState == SpriteBatchBeginEndState.Began)
                {
                    SpriteBatch.End();
                    beginEndState = SpriteBatchBeginEndState.Ended;

                }
            }
        }

        public void PopRenderStates()
        {

            var parameters = mStateStack.Last();
            mStateStack.RemoveAt(mStateStack.Count - 1);


            if (parameters.HasValue)
            {
                ReplaceRenderStates(parameters.Value.SortMode, parameters.Value.BlendState,
                    parameters.Value.SamplerState, parameters.Value.DepthStencilState,
                    parameters.Value.RasterizerState, parameters.Value.Effect,
                    parameters.Value.TransformMatrix, parameters.Value.ScissorRectangle, null);
            }
            else
            {
                if (currentParameters != null)
                {
                    beginParametersUsedThisFrame.Add(currentParameters.Value);
                }
                // this is the end
                currentParameters = null;
                End();
            }
        }

        private void RecordCurrentParameters()
        {
            if (currentParameters != null)
            {
                beginParametersUsedThisFrame.Add(currentParameters.Value);
            }
        }

        internal void ClearPerformanceRecordingVariables()
        {
            currentParameters = null;
            beginParametersUsedThisFrame.Clear();
        }
    }



    public class StateChangeInfoList : List<StateChangeInfo>
    {
        public int Index { get; set; }
        public bool Used { get; set; }
    }

    public class StateChangeInfoListPool : IEnumerable<StateChangeInfoList>
    {
        #region Fields
        List<StateChangeInfoList> mPoolables = new List<StateChangeInfoList>();
        int mNextAvailable = -1;
        #endregion

        public bool HasAnyUnusedItems
        {
            get { return mNextAvailable != -1; }
        }

        #region Methods

        public void AddToPool(StateChangeInfoList poolableToAdd)
        {

            int index = mPoolables.Count;

            if (mNextAvailable == -1)
            {
                mNextAvailable = index;
            }

            mPoolables.Add(poolableToAdd);
            poolableToAdd.Index = index;
            poolableToAdd.Used = false;
        }
        
        public StateChangeInfoList GetNextAvailable()
        {
            StateChangeInfoList returnReference;

            bool createdNewInstance = false;
            if (mNextAvailable == -1)
            {
                returnReference = new StateChangeInfoList();
                mPoolables.Add(returnReference);
                returnReference.Index = mPoolables.Count - 1;

                createdNewInstance = true;
            }
            else
            {
                returnReference = mPoolables[mNextAvailable];

            }
            returnReference.Used = true;

            // find next available
            int count = mPoolables.Count;

            if(!createdNewInstance)
            {
                mNextAvailable = -1;

                for (int i = returnReference.Index + 1; i < count; i++)
                {
                    var poolable = mPoolables[i];

                    if (poolable.Used == false)
                    {
                        mNextAvailable = i;
                        break;
                    }
                }
            }

            return returnReference;
        }

        public void MakeAllUnused()
        {
            int count = mPoolables.Count;

            for (int i = 0; i < count; i++)
            {
                MakeUnused(mPoolables[i]);
            }
        }

        public void MakeUnused(StateChangeInfoList poolableToMakeUnused)
        {
            if (mNextAvailable == -1 || poolableToMakeUnused.Index < mNextAvailable)
            {
                mNextAvailable = poolableToMakeUnused.Index;
            }

            poolableToMakeUnused.Used = false;
        }

        #endregion


        public IEnumerator<StateChangeInfoList> GetEnumerator()
        {
            return mPoolables.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mPoolables.GetEnumerator();
        }
    }


}
