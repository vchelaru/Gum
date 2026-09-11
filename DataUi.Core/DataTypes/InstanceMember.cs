
using System;
using System.Collections.Generic;
using System.ComponentModel;
using WpfDataUi.EventArguments;

namespace WpfDataUi.DataTypes
{
    #region SetPropertyArgs
    public enum SetPropertyCommitType
    {
        // A value is being set, but the user is continually editing the value, like sliding a slider
        Intermediate,
        // A value has been fully set, such as finishing sliding a slider, or a text box losing focus
        Full
    }

    public class SetPropertyArgs
    {
        public SetPropertyCommitType CommitType { get; set; }
        public object? Value { get; set; }
        /// <summary>
        /// Can be assigned to mark that assignment was cancelled. This can be used
        /// by views to react to a cancelled assignment, such as by preventing futher 
        /// processing of a value.
        /// </summary>
        public bool IsAssignmentCancelled { get; set; }
    }

    #endregion

    /// <summary>
    /// One row of a data grid: a named, typed value read from and written to an owning instance,
    /// through reflection or custom get/set events. Framework-neutral, so the WPF and Avalonia grids
    /// render the same rows.
    /// </summary>
    public class InstanceMember : INotifyPropertyChanged
    {
        #region Fields

        string mCustomDisplay;

        public int UniqueId;

        static int mNextUniqueId;

        Type mPreferredDisplayer;

        double mFirstGridLength;

        #endregion

        #region Properties

        /// <summary>
        /// Right-click menu entries for this row, keyed by header. Each displayer's menu shows them
        /// after the automatic "Make Default" entry.
        /// </summary>
        public Dictionary<string, EventHandler> ContextMenuEvents
        {
            get;
            private set;
        }

        public Dictionary<string, object> PropertiesToSetOnDisplayer { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Width, in device-independent pixels, of the label column. Displayers bind their first
        /// column to it.
        /// </summary>
        public double FirstGridLength
        {
            get { return mFirstGridLength; }
            set
            {
                mFirstGridLength = value;
                OnPropertyChanged("FirstGridLength");
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

        /// <summary>
        /// The value Value held immediately before the most recent UI-driven assignment
        /// (see IDataUiExtensionMethods.TrySetValueOnInstance and MenuItemExposedClick.MakeDefault).
        /// Used to populate PropertyChangedArgs.OldValue when DataUiGrid.PropertyChange fires.
        /// </summary>
        public object OldValue { get; set; }

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
                SetValue(value, SetPropertyCommitType.Full);
            }

        }

        public ApplyValueResult SetValue(object value, SetPropertyCommitType commitType)
        {
            ApplyValueResult result = ApplyValueResult.Success;

            if (CustomSetPropertyEvent != null)
            {
                var args = new SetPropertyArgs
                {
                    CommitType = commitType,
                    Value = value
                };
                CustomSetPropertyEvent(Instance, args);

                if(args.IsAssignmentCancelled)
                {
                    result = ApplyValueResult.UnknownError;
                }
            }
            else if (CustomSetEvent != null)
            {
                CustomSetEvent(Instance, value);
            }
            else
            {
                var instanceType = Instance.GetType();
                if(instanceType != null)
                {
                    var instance = LateBinder.GetInstance(instanceType);
                    instance?.SetValue(Instance, Name, value);
                }
            }
            OnPropertyChanged("Value");
            OnPropertyChanged(nameof(IsDefault));
            return result;
        }

        public bool IsDefined 
        {
            get
            {

                return (Instance != null && string.IsNullOrEmpty(Name) == false) ||
                    (CustomGetTypeEvent != null && CustomGetEvent != null)
                    
                    ;
            }
        }

        bool _supportsMakeDefault = true;
        /// <summary>
        /// Controls whether this InstanceMember automatically adds a 
        /// "Make Default" menu item. This defaults to true. Set this to
        /// false if you do not want "Make Default" added automatically to
        /// the right-click menu.
        /// </summary>
        public bool SupportsMakeDefault 
        {
            get => _supportsMakeDefault;
            set
            {
                if(value != _supportsMakeDefault)
                {
                    _supportsMakeDefault = value;
                    OnPropertyChanged(nameof(SupportsMakeDefault));
                }
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


        bool forcedReadOnly;
        /// <summary>
        /// Whether the member is read-only. If not explicitly set, this is true if the member is undefined, 
        /// if the property is readonly on the member (obtained through reflection), or if the 
        /// CustomSetEvent is null
        /// </summary>
        public virtual bool IsReadOnly 
        {
            set
            {
                forcedReadOnly = value;
            }
            get
            {
                if(forcedReadOnly)
                {
                    return true;
                }
                else if (!IsDefined)
                {
                    return false;
                }
                else if (Instance != null && (CustomGetEvent == null && CustomSetPropertyEvent == null))
                {
                    return LateBinder.GetInstance(Instance.GetType()).IsReadOnly(Name);
                }
                else
                {
                    return CustomSetEvent == null && CustomSetPropertyEvent == null;                
                }
            }
        }

        public virtual Type PreferredDisplayer
        {
            get => mPreferredDisplayer; 
            set
            {
                mPreferredDisplayer = value;
                OnPropertyChanged(nameof(PreferredDisplayer));
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
                        throw new NullReferenceException($"The member {this.Name} needs to have its Instance assigned, or needs to have its CustomGetTypeEvent assgned so that the type can be determined when deciding which UI control to create.");
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

        /// <summary>
        /// Whether the shown value is the default, differs across a multi-selection, or is set
        /// explicitly. Displayers tint their field from this.
        /// </summary>
        public DataUiValueState ValueState =>
            IsDefault ? DataUiValueState.Default
            : IsIndeterminate ? DataUiValueState.Indeterminate
            : DataUiValueState.Custom;

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

        string? toolTipText;
        public string? ToolTipText
        {
            get => toolTipText;
            set
            {
                var normalizedValue = string.IsNullOrEmpty(value) ? null : value;
                if (toolTipText != normalizedValue)
                {
                    toolTipText = normalizedValue;
                    OnPropertyChanged(nameof(ToolTipText));
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

        /// <summary>Raised with the displayer control each time a row host creates or reuses one for this member.</summary>
        public event Action<object>? UiCreated;

        /// <summary>
        /// Action which is called whenever an error occurs when the user enters a value.
        /// Parameter contains the newly-set value. 
        /// </summary>
        /// <example>
        /// This can occur if the user attemts to set a non-numerical value for a numerical variable, such as
        /// "a" for a float.
        /// </example>
        public Action<object> SetValueError;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Provides a custom set event. This is required if the instance member is not part of the
        /// Instance class as a field or property. The parameters are (owner, value)
        /// </summary>
        [Obsolete("Use CustomSetPropertyEvent to differentiate between Intermediate and Full property assignments")]
        public event Action<object, object> CustomSetEvent;

        /// <summary>
        /// Provides a custom set event which supports intermediate and full setting. 
        /// This is required if the instance member is not part of the
        /// Instance class as a field or property. The parameters are (owner, setPropertyArgs)
        /// </summary>
        public event Action<object, SetPropertyArgs> CustomSetPropertyEvent;

        /// <summary>
        /// Allows the InstanceMenber to define its own custom logic for getting a value.
        /// This requires a CustomSetEvent or CustomSetPropertyEvent to be functional.
        /// </summary>
        /// <remarks>
        /// The object passed in is the container of this member - which usually is the Instance of the DataGrid.
        /// </remarks>
        public event Func<object, object?> CustomGetEvent;
        public event Func<object, Type> CustomGetTypeEvent;

        #endregion

        #region Methods

        public InstanceMember()
        {
            ContextMenuEvents = new Dictionary<string, EventHandler>();
            mFirstGridLength = 100;
            Name = string.Empty;
            UniqueId = mNextUniqueId;
            mNextUniqueId++;
        }

        public InstanceMember(string name, object instance)
        {
            ContextMenuEvents = new Dictionary<string, EventHandler>();
            mFirstGridLength = 100;
            UniqueId = mNextUniqueId;
            mNextUniqueId++;
            Instance = instance;
            Name = name;
        }

        public void SimulateValueChanged()
        {
            OnPropertyChanged("Value");
            OnPropertyChanged(nameof(IsDefault));
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
            if(Category != null)
            {
                Category.HandleValueSetByUi(this);
            }
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
                object? value;
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

        /// <summary>
        /// Raises <see cref="UiCreated"/>. Called by the grid's row host after it assigns a displayer
        /// to this member.
        /// </summary>
        public void CallUiCreated(object control)
        {
            UiCreated?.Invoke(control);
        }

        #endregion
    }
}
