using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.Managers
{
    #region Enums

    public enum TextureAddress
    {
        EntireTexture,
        Custom,
        DimensionsBased
    }

    public enum ChildrenLayout
    {
        Regular,
        TopToBottomStack,
        LeftToRightStack

    }

    #endregion

    public class StandardElementsManager
    {
        #region Enums

        public enum DimensionVariableAction
        {
            ExcludeFileOptions,
            AllowFileOptions,
            DefaultToPercentageOfFile
        }

        #endregion

        #region Fields

        Dictionary<string, StateSave> mDefaults;

        static StandardElementsManager mSelf;

        #endregion

        #region Properties

        public IEnumerable<string> DefaultTypes
        {
            get
            {
                foreach (var kvp in mDefaults)
                {
                    yield return kvp.Key;
                }
            }
        }

        public static StandardElementsManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new StandardElementsManager();
                }
                return mSelf;
            }
        }

        public string DefaultType
        {
            get
            {
                return "Container";
            }
        }

        public Dictionary<string, StateSave> DefaultStates
        {
            get
            {
                return mDefaults;
            }
        }

        #endregion

        public StateSave GetDefaultStateFor(string type, bool throwExceptionOnMissing = true)
        {
            if (mDefaults == null)
            {
                throw new Exception("You must first call Initialize on StandardElementsManager before calling this function");
            }
            if (mDefaults.ContainsKey(type))
            {
                return mDefaults[type];

            }
            else
            {

                StateSave customState = null;
#if GUM
                
                customState = PluginManager.Self.GetDefaultStateFor(type);
#endif
                if (customState == null && throwExceptionOnMissing)
                {
                    throw new InvalidOperationException(
                        $"Could not get the default state for type {type} in either the default or through plugins");
                }
                else
                {
                    return customState;
                }
            }
        }

    }
}
