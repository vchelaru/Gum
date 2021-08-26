using Gum.Wireframe;
using SkiaGum.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaGum.GueDeriving
{

    public class BindableGraphicalUiElement : GraphicalUiElement
    {
        #region VmToUiProperty struct

        [DebuggerDisplay("VM:{VmProperty} UI:{UiProperty}")]
        struct VmToUiProperty
        {
            public string VmProperty;
            public string UiProperty;

            public override string ToString()
            {
                return $"VM:{VmProperty} UI{UiProperty}";
            }
        }

        #endregion

        #region Fields/Properties

        List<VmToUiProperty> vmPropsToUiProps = new List<VmToUiProperty>();

        object mBindingContext;
        public object BindingContext
        {
            get => mBindingContext;
            set
            {
                var oldBindingContext = mBindingContext;
                if(oldBindingContext is INotifyPropertyChanged oldViewModel)
                {
                    oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
                }
                mBindingContext = value;

                if (value is INotifyPropertyChanged viewModel)
                {
                    viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                }

                foreach(var vmToUiProp in vmPropsToUiProps)
                {
                    UpdateToVmProperty(vmToUiProp.VmProperty);
                }

                foreach(var child in this.Children)
                {
                    if(child is BindableGraphicalUiElement bindableElement)
                    {
                        bindableElement.BindingContext = BindingContext;
                    }
                }

                BindingContextChanged?.Invoke();

                this.EffectiveManagers?.InvalidateSurface();
            }
        }

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
                    return (this.ElementGueContainingThis as BindableGraphicalUiElement)?.EffectiveManagers ?? (this.EffectiveParentGue as BindableGraphicalUiElement)?.EffectiveManagers;
                }
            }
        }

        public ISystemManagers Managers
        {
            get;
            protected set;
        }

        #endregion

        #region Events

        public event Action BindingContextChanged;

        Func<Task> clickedAsync;
        public Func<Task> ClickedAsync
        {
            get => clickedAsync;
            set
            {
                clickedAsync = value;

                if(this.EffectiveManagers != null && clickedAsync != null)
                {
                    this.EffectiveManagers.EnableTouchEvents = true;
                }
            }
        }


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

                        vmProperty.SetValue(BindingContext, uiValue, null);
                    }
                }
            }
        }


        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vmPropertyName = e.PropertyName;
            var updated = UpdateToVmProperty(vmPropertyName);
            if(updated)
            {
                this.EffectiveManagers?.InvalidateSurface();
            }
        }

        private bool UpdateToVmProperty(string vmPropertyName)
        {
            var updated = false;
            foreach(var vmToUiProp in vmPropsToUiProps.Where(item => item.VmProperty == vmPropertyName))
            {
                var vmProperty = mBindingContext.GetType().GetProperty(vmPropertyName);
                if(vmProperty == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find property {vmPropertyName} in {mBindingContext.GetType()}");
                }
                else
                {
                    var vmValue = vmProperty.GetValue(mBindingContext, null);

                    var uiProperty = this.GetType().GetProperty(vmToUiProp.UiProperty);

                    if(uiProperty == null)
                    {
                        var exceptionMessage = $"Error binding {this.GetType()}.{vmToUiProp.UiProperty} to view model property {vmToUiProp.VmProperty}";
                        throw new Exception(exceptionMessage);
                    }

                    if(uiProperty.PropertyType == typeof(string))
                    {
                        uiProperty.SetValue(this, vmValue?.ToString(), null);
                    }
                    else
                    {
                        try
                        {
                            uiProperty.SetValue(this, vmValue, null);
                        }
                        catch(Exception e)
                        {
                            var message = $"Error reacting to view model property {vmPropertyName} with value {vmValue} and " +
                                $"assigning it to UI prop {vmToUiProp.UiProperty} on element of type {this.GetType()}. See inner exception";

                            throw new Exception(message, e);
                        }
                    }
                    updated = true;
                }
            }
            return updated;
        }

        public void SetBinding(string uiProperty, string vmProperty)
        {

#if DEBUG
            var foundUiProperty = this.GetType().GetProperty(uiProperty);

            if (foundUiProperty == null)
            {
                var exceptionMessage = $"Error binding: {this.GetType()}.{uiProperty} does not exist so it cannot be bound to {vmProperty}";

                throw new Exception(exceptionMessage);
            }

#endif

            var newAssociation = new VmToUiProperty();
            newAssociation.VmProperty = vmProperty;
            newAssociation.UiProperty = uiProperty;
            vmPropsToUiProps.Add(newAssociation);

            this.EffectiveManagers?.InvalidateSurface();

        }

    }
}
