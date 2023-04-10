
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi.EventArguments;

namespace WpfDataUi.DataTypes
{
    public class InstanceMember : DependencyObject
    {
        #region Fields

        string mCustomDisplay;

        public int UniqueId;

        static int mNextUniqueId;

        Type mPreferredDisplayer;

        GridLength mFirstGridLength;
        GridLength mSecondGridLength;

        #endregion

        #region Properties

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(InstanceMember), new PropertyMetadata(null));

        public Dictionary<string, RoutedEventHandler> ContextMenuEvents
        {
            get;
            private set;
        }

        public Dictionary<string, object> PropertiesToSetOnDisplayer { get; private set; } = new Dictionary<string, object>();

        public GridLength FirstGridLength
        {
            get { return mFirstGridLength; }
            set
            {
                mFirstGridLength = value;
                OnPropertyChanged("FirstGridLength");
            }
        }

        public GridLength SecondGridLength
        {
            get
            {
                return mSecondGridLength;
            }
        }

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

        public object Instance
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public object Value
        {
            get
            {
                // I think we want to do the CustomGetEvent even if it's null...
                //if (Instance == null)
                //{
                //    return null;
                //}
                //else if (CustomGetEvent != null)
                //{
                //    return CustomGetEvent(Instance);
                //}

                if (CustomGetEvent != null)
                {
                    return CustomGetEvent(Instance);
                }
                else if (Instance != null)
                {
                    return LateBinder.GetInstance(Instance.GetType()).GetValue(Instance, Name);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (CustomSetEvent != null)
                {
                    CustomSetEvent(Instance, value);
                }
                else
                {
                    LateBinder.GetInstance(Instance.GetType()).SetValue(Instance, Name, value);
                }
                OnPropertyChanged("Value");
            }

        }

        public bool IsDefined 
        {
            get
            {

                return (Instance != null && string.IsNullOrEmpty(Name) == false) ||
                    (CustomGetTypeEvent != null && CustomGetEvent != null && CustomSetEvent != null)
                    
                    ;
            }
        }

        public bool IsWriteOnly 
        {
            get
            {
                if (!IsDefined)
                {
                    return false;
                }
                else
                {
                    if (CustomGetEvent != null)
                    {
                        return false;
                    }
                    else
                    {
                        return LateBinder.GetInstance(Instance.GetType()).IsWriteOnly(Name);
                    }
                }
            }

        }


        public virtual bool IsReadOnly 
        {
            get
            {
                if (!IsDefined)
                {
                    return false;
                }
                else if (Instance != null)
                {
                    return LateBinder.GetInstance(Instance.GetType()).IsReadOnly(Name);
                }
                else
                {
                    return CustomSetEvent == null;                
                }
            }
        }

        public virtual Type PreferredDisplayer
        {
            get { return mPreferredDisplayer; }
            set
            {
                mPreferredDisplayer = value;
                OnPropertyChanged("PreferredDisplayer");
            }

        }

        public virtual Type PropertyType
        {
            get
            {
                if (CustomGetTypeEvent != null)
                {
                    return CustomGetTypeEvent(Instance);
                }
                else
                {
                    if(Instance == null)
                    {
                        throw new NullReferenceException($"The member {this.Name} needs to have its Instance assigned, or needs to have its CustomGetTypeEvent assgned");
                    }
                    return IDataUiExtensionMethods.GetPropertyType(Name, Instance.GetType());
                }
            }

        }

        public MemberCategory Category
        {
            get;
            set;
        }

        public virtual bool IsDefault
        {
            get
            {
                return false;
            }
            set
            {
                // do nothing?
                // Update August 1, 2022
                // Why not set the value to
                // null?
                var propertyType = PropertyType;
                if(propertyType != null)
                {
                    if (propertyType.IsValueType)
                    {
                        Value = Activator.CreateInstance(propertyType);
                    }
                    else
                    {
                        Value = null;
                    }
                }

            }
        }

        public virtual bool IsIndeterminate { get; } = false;

        // Used to "new" this up, but doing so makes combo boxes
        // have no options. If this is null, combo box displayers
        // use the default full enum list.
        //List<object> backingList = new List<object>();
        IList<object> backingList;
        public virtual IList<object> CustomOptions
        {
            get
            {
                return backingList;
            }
            set
            {
                backingList = value;
            }
        }

        public Brush BackgroundColor
        {
            get;
            set;
        }

        string detailText;
        public string DetailText 
        {
            get => detailText;
            set
            {
                if(detailText != value)
                {
                    detailText = value;
                    OnPropertyChanged(nameof(DetailText));
                }
            }
        }

        // see TypeMemberDisplayProperties
        //public bool IsVisible
        //{
        //    get;
        //    set;
        //} = true;

        #endregion

        #region Events
        public EventHandler BeforeSetByUi;
        public EventHandler AfterSetByUi;

        public event Action<UserControl> UiCreated;

        /// <summary>
        /// Action which is called whenever an error occurs when the user enters a value.
        /// Parameter contains the newly-set value. 
        /// </summary>
        /// <example>
        /// This can occur if the user attemts to set a non-numerical value for a numerical variable, such as
        /// "a" for a float.
        /// </example>
        public Action<object> SetValueError;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Provides a custom set event. This is required if the instance member is not part of the
        /// Instance class as a field or property. The parameters are (owner, value)
        /// </summary>
        /// 
        public event Action<object, object> CustomSetEvent;
        /// <summary>
        /// Allows the InstanceMenber to define its own custom logic for getting a value.
        /// This requires a CustomSetEvent to be functional.
        /// </summary>
        /// <remarks>
        /// The object passed in is the container of this member - which usually is the Instance of the DataGrid.
        /// </remarks>
        public event Func<object, object> CustomGetEvent;
        public event Func<object, Type> CustomGetTypeEvent;

        #endregion

        #region Methods

        public InstanceMember()
        {
            ContextMenuEvents = new Dictionary<string, RoutedEventHandler>();
            mFirstGridLength = new GridLength(100);
            mSecondGridLength = new GridLength(100);
            UniqueId = mNextUniqueId;
            mNextUniqueId++;
        }

        public InstanceMember(string name, object instance)
        {
            ContextMenuEvents = new Dictionary<string, RoutedEventHandler>();


            mFirstGridLength = new GridLength(100);
            mSecondGridLength = new GridLength(100);
            UniqueId = mNextUniqueId;
            mNextUniqueId++;
            Instance = instance;
            Name = name;
        }

        public void SimulateValueChanged()
        {
            OnPropertyChanged("Value");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs args =
                    new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, args);
            }
        }

        public override string ToString()
        {
            string name = Name;
            if(string.IsNullOrEmpty(name))
            {
                name = mCustomDisplay;
            }
            return name + " = " + Value;
        }

        public void CallAfterSetByUi()
        {
            if (AfterSetByUi != null)
            {
                AfterSetByUi(this, null);
            }
        }

        internal void CallBeforeSetByUi(IDataUi dataUi)
        {
            if (BeforeSetByUi != null)
            {
                BeforePropertyChangedArgs args = new BeforePropertyChangedArgs();
                object value;
                dataUi.TryGetValueOnUi(out value);

                args.NewValue = value;

                BeforeSetByUi(this, args);

                if (args.WasManuallySet)
                {
                    // The event changed it, so let's force it back on the UI
                    dataUi.TrySetValueOnUi(args.OverridingValue);
                }
            }
        }

        internal void CallUiCreated(UserControl control)
        {
            UiCreated?.Invoke(control);
        }

        #endregion
    }
}
