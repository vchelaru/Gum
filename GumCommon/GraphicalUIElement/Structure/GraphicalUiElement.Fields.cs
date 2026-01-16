using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Fields

        public static float GlobalFontScale = 1;

        private DirtyState currentDirtyState;
        bool isFontDirty = false;
        public bool IsFontDirty
        {
            get => isFontDirty;
            set => isFontDirty = value;
        }

        /// <summary>
        /// The total number of layout calls that have been performed since the application has started running.
        /// This value can be used as a rough indication of the layout cost and to measure whether efforts to reduce
        /// layout calls have been effective.
        /// </summary>
        public static int UpdateLayoutCallCount;
        public static int ChildrenUpdatingParentLayoutCalls;

        // This used to be true until Jan 26, 2024, but it's
        // confusing for new users. Let's keep this off and document
        // how to use it (eventually).
        public static bool ShowLineRectangles = false;

        // to save on casting:
        protected IRenderableIpso? mContainedObjectAsIpso;
        protected IVisible mContainedObjectAsIVisible;

        GraphicalUiElement? mWhatContainsThis;

        /// <summary>
        /// A flat list of all GraphicalUiElements contained by this element. For example, if this GraphicalUiElement
        /// is a Screen, this list is all GraphicalUielements for every instance contained regardless of hierarchy.
        /// </summary>
        List<GraphicalUiElement> mWhatThisContains = new List<GraphicalUiElement>();

        protected List<GraphicalUiElement> WhatThisContains => mWhatThisContains;

        Dictionary<string, string> mExposedVariables = new Dictionary<string, string>();

        GeneralUnitType mXUnits;
        GeneralUnitType mYUnits;
        HorizontalAlignment mXOrigin;
        VerticalAlignment mYOrigin;
        DimensionUnitType mWidthUnit;
        DimensionUnitType mHeightUnit;

        protected ISystemManagers? mManagers;

        int mTextureTop;
        int mTextureLeft;
        int mTextureWidth;
        int mTextureHeight;
        bool mWrap;

        bool mWrapsChildren = false;

        float mTextureWidthScale = 1;
        float mTextureHeightScale = 1;

        TextureAddress mTextureAddress;

        float mX;
        float mY;
        // Since these are protected, we can't change them to _width and _height 
        // FRB already uses mWidth and mHeight in its codegen
        protected float mWidth;
        protected float mHeight;
        float mRotation;

        GraphicalUiElement? _parent;

        protected bool mIsLayoutSuspended = false;
        public bool IsLayoutSuspended => mIsLayoutSuspended;

        // We need ThreadStatic in case screens are being loaded
        // in the background - we don't want to interrupt the foreground
        // layout behavior.
        [ThreadStatic]
        public static bool IsAllLayoutSuspended = false;

        Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
            new Dictionary<string, DataTypes.Variables.StateSave>();

        public Dictionary<string, Gum.DataTypes.Variables.StateSave> States => mStates;

        Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
            new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

        // This needs to be made public so that individual Forms objects can be customized:
        public Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> Categories => mCategories;

        // the row or column index when anobject is sorted.
        // This is used by the stacking logic to properly sort objects
        public int StackedRowOrColumnIndex { get; set; } = -1;

        // null by default, non-null if an object uses
        // stacked layout for its children.
        public List<float> StackedRowOrColumnDimensions { get; private set; }
        #endregion
    }
}