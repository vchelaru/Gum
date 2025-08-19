using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SkiaGum.GueDeriving
{
    
    public class BindingContextChangedEventArgs : EventArgs
    {
        public object OldBindingContext { get; set; }
    }

    [DebuggerDisplay("{DebugDetails}")]
    [Obsolete("Use BindableGue")]
    public class BindableGraphicalUiElement : GraphicalUiElement
    {
        #region VmToUiProperty struct

        [DebuggerDisplay("VM:{VmProperty} UI:{UiProperty}")]
        struct VmToUiProperty
        {
            public string VmProperty;
            public string UiProperty;

            public string ToStringFormat;

            public override string ToString()
            {
                return $"VM:{VmProperty} UI{UiProperty}";
            }

            public static VmToUiProperty Unassigned => new VmToUiProperty();
        }

        #endregion

        #region Fields/Properties

        List<VmToUiProperty> vmPropsToUiProps = new List<VmToUiProperty>();

        object mInheritedBindingContext;
        internal object InheritedBindingContext
        {
            get => mInheritedBindingContext;
            set
            {
                if (value != mInheritedBindingContext)
                {
                    var oldEffectiveBindingContext = EffectiveBindingContext;
                    mInheritedBindingContext = value;
                    HandleBindingContextChangedInternal(oldEffectiveBindingContext);

                }
            }
        }

        object mBindingContext;
        public object BindingContext
        {
            get => EffectiveBindingContext;
            set
            {
                if (value != EffectiveBindingContext)
                {
                    var oldEffectiveBindingContext = EffectiveBindingContext;
                    mBindingContext = value;
                    HandleBindingContextChangedInternal(oldEffectiveBindingContext);
                }

            }
        }

        public object BindingContextBindingPropertyOwner { get; private set; }
        public string BindingContextBinding { get; private set; }


        object EffectiveBindingContext => mBindingContext ?? InheritedBindingContext;

        /// <summary>
        /// Returns this instance's SystemManagers, or climbs up the parent/child relationship
        /// until a non-null SystemsManager is found. Otherwise, returns null.
        /// </summary>
        public ISystemManagers EffectiveManagers
        {
            get
            {
                if (Managers != null)
                {
                    return Managers;
                }
                else
                {
                    return (this.ElementGueContainingThis as BindableGue)?.EffectiveManagers ?? 
                        (this.EffectiveParentGue as BindableGue)?.EffectiveManagers;
                }
            }
        }

        public ISystemManagers Managers
        {
            get;
            protected set;
        }

        public static bool ShouldApplyDynamicStates;


        #endregion

        #region Events

        Func<Task>? clickedAsync;
        /// <summary>
        /// Occurs when the user releases the cursor on this, whether this was initially the pushed element or not.
        /// </summary>
        public Func<Task>? ClickedAsync
        {
            get => clickedAsync;
            set
            {
                clickedAsync = value;

                if (this.EffectiveManagers != null && clickedAsync != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }

        Func<float, float, Task>? pushedAsync;
        public Func<float, float, Task>? PushedAsync
        {
            get => pushedAsync;
            set
            {
                pushedAsync = value;

                if (this.EffectiveManagers != null && pushedAsync != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }


        Func<float, float, Task>? dragAsync;
        public Func<float, float, Task>? DragAsync
        {
            get => dragAsync;
            set
            {
                dragAsync = value;

                if (this.EffectiveManagers != null && dragAsync != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }

        Func<Task>? dragOff;
        public Func<Task>? DragOff
        {
            get => dragOff;
            set
            {
                dragOff = value;

                if (this.EffectiveManagers != null && dragOff != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }

        Func<Task>? releasedIfPushed;
        public Func<Task>? ReleasedIfPushed
        {
            get => releasedIfPushed;
            set
            {
                releasedIfPushed = value;

                if (this.EffectiveManagers != null && releasedIfPushed != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }

        public event Action<object, BindingContextChangedEventArgs> BindingContextChanged;

        #endregion


        public virtual void AddToManagers(ISystemManagers managers/*, Layer layer*/)
        {
#if DEBUG
            if (managers == null)
            {
                throw new ArgumentNullException("managers cannot be null");
            }
#endif
            // If mManagers isn't null, it's already been added
            if (Managers == null)
            {
                //mLayer = layer;
                Managers = managers;

                //AddContainedRenderableToManagers(managers, layer);

                // Custom should be called before children have their Custom called
                //CustomAddToManagers();

                // that means this is a screen, so the children need to be added directly to managers
                //if (this.mContainedObjectAsIpso == null)
                //{
                //    //AddChildren(managers, layer);
                //}
                //else
                //{
                //    //CustomAddChildren();
                //}
            }
        }

        protected void PushValueToViewModel([System.Runtime.CompilerServices.CallerMemberName] string uiPropertyName = null)
        {
            var kvp = vmPropsToUiProps.FirstOrDefault(item => item.UiProperty == uiPropertyName);

            if (kvp.UiProperty == uiPropertyName)
            {
                var vmPropName = kvp.VmProperty;

                var vmProperty = BindingContext?.GetType().GetProperty(vmPropName);

                if (vmProperty != null)
                {
                    var uiProperty = this.GetType().GetProperty(uiPropertyName);
                    if (uiProperty != null)
                    {
                        var uiValue = uiProperty.GetValue(this, null);

                        var convertedValue = ConvertValue(uiValue, vmProperty.PropertyType, null);

                        vmProperty.SetValue(BindingContext, convertedValue, null);
                    }
                }
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vmPropertyName = e.PropertyName;
            var updated = UpdateToVmProperty(vmPropertyName);
            if (updated)
            {
                try
                {
                    this.EffectiveManagers?.InvalidateSurface();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception trying to handle vm property {vmPropertyName}", ex);
                }
            }
        }

        private bool UpdateToVmProperty(string vmPropertyName)
        {
            var updated = false;

            var isBoundToVmProperty = vmPropsToUiProps.Any(item => item.VmProperty == vmPropertyName) ||
                BindingContextBinding == vmPropertyName;

            var bindingContextObjectToUse = BindingContextBinding == vmPropertyName ?
                BindingContextBindingPropertyOwner : EffectiveBindingContext;
            if (isBoundToVmProperty && bindingContextObjectToUse != null)
            {

                var vmProperty = bindingContextObjectToUse.GetType().GetProperty(vmPropertyName);
                object vmValue = null;
                bool didSetVmValue = false;
                string uiPropertyName;

                string toStringFormat = null;

                if (vmPropertyName == BindingContextBinding)
                {
                    uiPropertyName = nameof(BindingContext);
                }
                else
                {
                    var vmToUiProp = vmPropsToUiProps.First(item => item.VmProperty == vmPropertyName);
                    uiPropertyName = vmToUiProp.UiProperty;
                    toStringFormat = vmToUiProp.ToStringFormat;
                }

                if (vmProperty == null && BindingContextBinding != vmPropertyName)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find property {vmPropertyName} in {bindingContextObjectToUse?.GetType()}");
                }
                else
                {
                    try
                    {
                        vmValue = vmProperty.GetValue(bindingContextObjectToUse, null);
                    }
                    catch (TargetInvocationException)
                    {
                        throw new Exception($"Error getting property {vmPropertyName} in {bindingContextObjectToUse?.GetType()}");
                    }
                    didSetVmValue = true;
                }

                if (!didSetVmValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find property {vmPropertyName} in {bindingContextObjectToUse?.GetType()}");
                }
                else
                {
                    var uiProperty = this.GetType().GetProperty(uiPropertyName);

                    if (uiProperty == null)
                    {
                        var exceptionMessage = $"Error binding {this.GetType()}.{uiPropertyName} to view model property {vmPropertyName}";
                        throw new Exception(exceptionMessage);
                    }

                    var convertedValue = ConvertValue(vmValue, uiProperty.PropertyType, toStringFormat);
                    try
                    {
                        uiProperty.SetValue(this, convertedValue, null);
                    }
                    catch (Exception e)
                    {
                        var message = $"Error reacting to view model property {vmPropertyName} " +
                            $"with value {vmValue} ({vmValue?.GetType().ToString()}) and " +
                            $"assigning it to UI prop {uiPropertyName} ({uiProperty.PropertyType}) on element of type {this.GetType()}.\n\n{e.Message}";

                        throw new Exception(message, e);
                    }
                    updated = true;
                }


            }
            TryPushBindingContextChangeToChildren(vmPropertyName);


            return updated;
        }

        private object ConvertValue(object value, Type desiredType, string format)
        {
            object convertedValue = value;
            if (desiredType == typeof(string))
            {
                if(!string.IsNullOrEmpty(format))
                {
                    if (value is int asInt) convertedValue = asInt.ToString(format);
                    else if(value is double asDouble) convertedValue = asDouble.ToString(format);
                    else if(value is decimal asDecimal) convertedValue = asDecimal.ToString(format);
                    else if(value is float asFloat) convertedValue = asFloat.ToString(format);
                    else if(value is long asLong) convertedValue = asLong.ToString(format);
                }
                else
                {
                    convertedValue = value?.ToString();
                }
            }
            else if (desiredType == typeof(int))
            {
                if (value is decimal asDecimal)
                {
                    // do we round? 
                    convertedValue = (int)asDecimal;
                }
            }
            else if (desiredType == typeof(double))
            {
                if (value is int asInt)
                {
                    convertedValue = (double)asInt;
                }
                else if (value is decimal asDecimal)
                {
                    convertedValue = (double)asDecimal;
                }
                else if(value is float asFloat)
                {
                    convertedValue = (double)asFloat;
                }
            }
            else if (desiredType == typeof(decimal))
            {
                if (value is int asInt)
                {
                    convertedValue = (decimal)asInt;
                }
                else if (value is double asDouble)
                {
                    convertedValue = (decimal)asDouble;
                }
                else if(value is float asFloat)
                {
                    convertedValue = (decimal)asFloat;
                }
            }
            else if (desiredType == typeof(float))
            {
                if (value is int asInt)
                {
                    convertedValue = (float)asInt;
                }
                else if(value is double asDouble)
                {
                    convertedValue = (float)asDouble;
                }
                else if(value is decimal asDecimal)
                {
                    convertedValue = (float)asDecimal;
                }
            }
            return convertedValue;
        }

        private void TryPushBindingContextChangeToChildren(string vmPropertyName)
        {
            if (this.Children != null)
            {
                foreach (var child in Children)
                {
                    if (child is BindableGraphicalUiElement gue)
                    {
                        if (gue.BindingContextBinding == vmPropertyName && gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                        {
                            gue.UpdateToVmProperty(vmPropertyName);
                        }
                        gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                    }
                }
            }
            else
            {
                foreach (BindableGraphicalUiElement gue in ContainedElements)
                {
                    if (gue.BindingContextBinding == vmPropertyName && gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                    {
                        gue.UpdateToVmProperty(vmPropertyName);
                    }
                    gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                }
            }
        }

        public void SetBinding(string uiProperty, string vmProperty, string toStringFormat = null)
        {
            if (uiProperty == nameof(BindingContext))
            {
                BindingContextBinding = vmProperty;
            }
            else
            {
                var existingVm = vmPropsToUiProps.FirstOrDefault(item => item.VmProperty == vmProperty);
                if (!string.IsNullOrWhiteSpace(existingVm.VmProperty))
                {
                    vmPropsToUiProps.Remove(existingVm);
                }
                // This prevents single UI properties from being bound to multiple VM properties
                if (vmPropsToUiProps.Any(item => item.UiProperty == uiProperty))
                {
                    var toRemove = vmPropsToUiProps.Where(item => item.UiProperty == uiProperty).ToArray();

                    foreach (var kvp in toRemove)
                    {
                        vmPropsToUiProps.Remove(kvp);
                    }
                }

                var vmToUiProperty = new VmToUiProperty() 
                { 
                    VmProperty = vmProperty, 
                    UiProperty = uiProperty 
                };

                vmToUiProperty.ToStringFormat = toStringFormat;

                vmPropsToUiProps.Add(vmToUiProperty);

                if (EffectiveBindingContext != null)
                {
                    UpdateToVmProperty(vmProperty);
                }

            }
            this.EffectiveManagers?.InvalidateSurface();
        }

        private void HandleBindingContextChangedInternal(object oldBindingContext)
        {
            if (oldBindingContext is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
            if (EffectiveBindingContext is INotifyPropertyChanged viewModel)
            {
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;

            }
            if (EffectiveBindingContext != null)
            {
                foreach (var vmProperty in vmPropsToUiProps)
                {
                    UpdateToVmProperty(vmProperty.VmProperty);
                }


            }

            var args = new BindingContextChangedEventArgs();
            args.OldBindingContext = oldBindingContext;


            if (this.Children != null)
            {
                // do the default first...
                UpdateChildrenInheritedBindingContext(this.Children, EffectiveBindingContext);
                // ... then overwrite it
                foreach (var child in this.Children)
                {
                    if (child is BindableGraphicalUiElement gue)
                    {
                        if (gue.BindingContextBinding != null)
                        {
                            gue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                            gue.UpdateToVmProperty(gue.BindingContextBinding);
                        }
                    }
                }
            }
            else
            {
                // Do the default functionalty first...
                UpdateChildrenInheritedBindingContext(this.ContainedElements, EffectiveBindingContext);
                // ... then overwrite it
                foreach (var gue in this.ContainedElements)
                {
                    if (gue is BindableGraphicalUiElement bgue)
                    {
                        if (bgue.BindingContextBinding != null)
                        {
                            bgue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                            bgue.UpdateToVmProperty(bgue.BindingContextBinding);
                        }

                    }
                }
            }
            BindingContextChanged?.Invoke(this, args);
        }

        private static void UpdateChildrenInheritedBindingContext(IEnumerable<IRenderableIpso> children, object effectiveBindingContext)
        {
            foreach (var child in children)
            {
                if (child is BindableGraphicalUiElement gue)
                {
                    if (gue.InheritedBindingContext != effectiveBindingContext)
                    {
                        var effectiveBeforeChange = gue.EffectiveBindingContext;
                        gue.InheritedBindingContext = effectiveBindingContext;
                        if (effectiveBindingContext != gue.EffectiveBindingContext)
                        {
                            // This saves us some processing. If the parent's effective didn't change, then no need
                            // to notify the children
                            UpdateChildrenInheritedBindingContext(child.Children, gue.EffectiveBindingContext);
                        }
                    }
                }

            }
        }

        public string DebugDetails => $"{Name} {GetType().Name}";
    }
}
