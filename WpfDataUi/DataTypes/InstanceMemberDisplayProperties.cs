using System;

namespace WpfDataUi.DataTypes
{
    public class InstanceMemberDisplayProperties
    {
        #region Fields

        string mCustomDisplay;

        #endregion

        #region Properties

        public string DisplayName 
        {
            get
            {
                if (string.IsNullOrEmpty(mCustomDisplay))
                {
                    return Name;
                }
                else
                {
                    return mCustomDisplay;
                }
            }
            set
            {
                mCustomDisplay = value;
            }
        }

        public bool IsHidden
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public Type PreferredDisplayer
        {
            get;
            set;
        }

        // If adding anything here that doesn't clone automatically, modify the Clone method

        #endregion

        public Func<InstanceMember, bool> IsHiddenDelegate;

        #region Methods

        public InstanceMemberDisplayProperties()
        {

        }

        public bool GetEffectiveIsHidden(InstanceMember instance)
        {
            if (IsHiddenDelegate != null)
            {
                return IsHiddenDelegate(instance);
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public InstanceMemberDisplayProperties Clone()
        {
            return this.MemberwiseClone() as InstanceMemberDisplayProperties;
        }

        #endregion
    }
}
