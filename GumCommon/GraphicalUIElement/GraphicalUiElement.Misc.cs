using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumDataTypes.Variables;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilitiesStandard.Helpers;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Misc. Methods

        public bool IsFullyCreated { get; private set; }
        /// <summary>
        /// Method which is called after a control is fully created when it is created from a FrameworkElement 
        /// when ToGraphicalUiElement or SetGraphicalUiElement are called. 
        /// </summary>
        public virtual void AfterFullCreation()
        {
            IsFullyCreated = true;
        }

        /// <summary>
        /// Sets the default state.
        /// </summary>
        /// <remarks>
        /// This function is virtual so that derived classes can override it
        /// and provide a quicker method for setting default states
        /// </remarks>
        public virtual void SetInitialState()
        {
            var elementSave = this.Tag as ElementSave ?? this.ElementSave;
            this.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        }

        string NameOrType => !string.IsNullOrEmpty(Name) ? Name : $"<{GetType().Name}>";

        string ParentQualifiedName => Parent as GraphicalUiElement == null ? NameOrType : (Parent as GraphicalUiElement).ParentQualifiedName + "." + NameOrType;

        public static bool AreUpdatesAppliedWhenInvisible { get; set; } = false;

        public virtual void PreRender()
        {
            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.PreRender();
            }
        }

        static void GetRightAndUpFromRotation(float rotationInDegrees, out Vector3 right, out Vector3 up)
        {

            var quarterRotations = rotationInDegrees / 90;
            var radiansFromPerfectRotation = System.Math.Abs(quarterRotations - MathFunctions.RoundToInt(quarterRotations));

            const float errorToTolerate = .1f / 90f;

            if (radiansFromPerfectRotation < errorToTolerate)
            {
                var quarterRotationsAsInt = MathFunctions.RoundToInt(quarterRotations) % 4;
                if (quarterRotationsAsInt < 0)
                {
                    quarterRotationsAsInt += 4;
                }

                // invert it to match how rotation works with the CreateRotationZ method:
                quarterRotationsAsInt = 4 - quarterRotationsAsInt;

                right = Vector3Extensions.Right;
                up = Vector3Extensions.Up;

                switch (quarterRotationsAsInt)
                {
                    case 0:
                        right = Vector3Extensions.Right;
                        up = Vector3Extensions.Up;
                        break;
                    case 1:
                        right = Vector3Extensions.Up;
                        up = Vector3Extensions.Left;
                        break;
                    case 2:
                        right = Vector3Extensions.Left;
                        up = Vector3Extensions.Down;
                        break;

                    case 3:
                        right = Vector3Extensions.Down;
                        up = Vector3Extensions.Right;
                        break;
                }
            }
            else
            {
                var matrix = System.Numerics.Matrix4x4.CreateRotationZ(-MathHelper.ToRadians(rotationInDegrees));
                right = matrix.Right();
                up = matrix.Up();
            }

        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return GetType().Name;
            }
            else
            {
                return Name;
            }
        }

        public void SetGueValues(IVariableFinder rvf)
        {

            this.SuspendLayout();

            this.Width = rvf.GetValue<float>("Width");
            this.Height = rvf.GetValue<float>("Height");

            this.HeightUnits = rvf.GetValue<DimensionUnitType>("HeightUnits");
            this.WidthUnits = rvf.GetValue<DimensionUnitType>("WidthUnits");

            this.XOrigin = rvf.GetValue<HorizontalAlignment>("XOrigin");
            this.YOrigin = rvf.GetValue<VerticalAlignment>("YOrigin");

            this.X = rvf.GetValue<float>("X");
            this.Y = rvf.GetValue<float>("Y");

            this.XUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("XUnits"));
            this.YUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("YUnits"));

            this.TextureWidth = rvf.GetValue<int>("TextureWidth");
            this.TextureHeight = rvf.GetValue<int>("TextureHeight");
            this.TextureLeft = rvf.GetValue<int>("TextureLeft");
            this.TextureTop = rvf.GetValue<int>("TextureTop");

            this.TextureWidthScale = rvf.GetValue<float>("TextureWidthScale");
            this.TextureHeightScale = rvf.GetValue<float>("TextureHeightScale");

            this.Wrap = rvf.GetValue<bool>("Wrap");

            this.TextureAddress = rvf.GetValue<TextureAddress>("TextureAddress");

            this.ChildrenLayout = rvf.GetValue<ChildrenLayout>("ChildrenLayout");
            this.WrapsChildren = rvf.GetValue<bool>("WrapsChildren");
            this.ClipsChildren = rvf.GetValue<bool>("ClipsChildren");

            if (this.ElementSave != null)
            {
                foreach (var category in ElementSave.Categories)
                {
                    string valueOnThisState = rvf.GetValue<string>(category.Name + "State");

                    if (!string.IsNullOrEmpty(valueOnThisState))
                    {
                        this.ApplyState(valueOnThisState);
                    }
                }
            }

            this.ResumeLayout();
        }

        partial void CustomAddToManagers();

        /// <summary>
        /// Adds this as a renderable to the SystemManagers if not already added. If already added
        /// this does not perform any operations - it can be safely called multiple times.
        /// </summary>

#if NET6_0_OR_GREATER
        public virtual void AddToManagers()
        {

            AddToManagers(ISystemManagers.Default, null);

        }
#endif

        /// <summary>
        /// Adds this as a renderable to the SystemManagers on the argument layer if not already added
        /// to SystemManagers. If already added
        /// this does not perform any operations - it can be safely called multiple times, but
        /// calling it multiple times will not move this to a different layer.
        /// </summary>
        public virtual void AddToManagers(ISystemManagers managers, Layer? layer = null)
        {
#if FULL_DIAGNOSTICS
            if (managers == null)
            {
                throw new ArgumentNullException("managers cannot be null");
            }
#endif
            // If mManagers isn't null, it's already been added
            if (mManagers == null)
            {
                mLayer = layer;
                mManagers = managers;

                AddContainedRenderableToManagers(managers, layer);

                RecursivelyAddIManagedChildren(this);

                // Custom should be called before children have their Custom called
                CustomAddToManagers();

                // that means this is a screen, so the children need to be added directly to managers
                if (this.mContainedObjectAsIpso == null)
                {
                    AddChildren(managers, layer);
                }
                else
                {
                    CustomAddChildren();
                }
            }
        }

        private static void RecursivelyAddIManagedChildren(GraphicalUiElement gue)
        {
            if (gue.ElementSave != null && gue.ElementSave is ScreenSave)
            {

                //Recursively add children to the managers
                foreach (var child in gue.mWhatThisContains)
                {
                    if (child is IManagedObject managedObject)
                    {
                        managedObject.AddToManagers();
                    }
                    RecursivelyAddIManagedChildren(child);
                }
            }
            else if (gue.Children != null)
            {
                foreach (var child in gue.Children)
                {
                    if (child is IManagedObject managedObject)
                    {
                        managedObject.AddToManagers();
                    }

                    RecursivelyAddIManagedChildren(child);

                }
            }
        }

        private void CustomAddChildren()
        {
            foreach (var child in this.mWhatThisContains)
            {
                child.mManagers = this.mManagers;
                child.CustomAddToManagers();

                child.CustomAddChildren();
            }
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems)
                    {
#if FULL_DIAGNOSTICS
                        if (newItem == null)
                        {
                            throw new InvalidOperationException($"Attempting to add a null child to {this}");
                        }
                        if (newItem == this)
                        {
                            throw new InvalidOperationException($"{this} cannot be added as a child of itself");
                        }
#endif
                        var ipso = (GraphicalUiElement)newItem;

                        if (ipso.Parent != this)
                        {
                            ipso.Parent = this;

                        }
                    }

                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                // for now let's just do a layout on this and the children
                UpdateLayout();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (IRenderableIpso ipso in e.OldItems)
                    {
                        if (ipso.Parent == this)
                        {
                            ipso.Parent = null;
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (IRenderableIpso ipso in e.OldItems)
                    {
                        if (ipso.Parent == this)
                        {
                            ipso.Parent = null;
                        }
                    }
                }
                else
                {
#if FULL_DIAGNOSTICS
                    var message = "STOP!!! The GraphicalUiElement " + this + " has been reset, but the Children ObservableCollection " +
                        "did not include e.OldItems, so the old children cannot have their Parent set to null. This can cause memory leaks through " +
                        "events, and other references. You should consider implementing a Children backing field that instead loops through and removes each child through a .Remove call.";

                    System.Diagnostics.Debug.WriteLine(message);
#endif
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.OldItems != null)
                {
                    foreach (IRenderableIpso ipso in e.OldItems)
                    {
                        if (ipso.Parent == this)
                        {
                            ipso.Parent = null;
                        }
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (IRenderableIpso ipso in e.NewItems)
                    {
                        if (ipso.Parent != this)
                        {
                            ipso.Parent = this;

                        }
                    }
                }
            }
        }

        private void AddChildren(ISystemManagers managers, Layer layer)
        {
            // In a simple situation we'd just loop through the
            // ContainedElements and add them to the manager.  However,
            // this means that the container will dictate the Layer that
            // its children reside on.  This is not what we want if we have
            // two children, one of which is attached to the other, and the parent
            // instance clips its children.  Therefore, we should make sure that we're
            // only adding direct children and letting instances handle their own children

            if (this.ElementSave is ScreenSave || this.Children == null)
            {

                //Recursively add children to the managers
                foreach (var child in this.mWhatThisContains)
                {
                    // July 27, 2014
                    // Is this an unnecessary check?
                    // if (child is GraphicalUiElement)
                    {
                        // December 1, 2014
                        // I think that when we
                        // add a screen we should
                        // add all of the children of
                        // the screen.  There's nothing
                        // "above" that.
                        if (child.Parent == null || child.Parent == this)
                        {
                            child.AddToManagers(managers, layer);
                        }
                        else
                        {
                            child.mManagers = this.mManagers;

                            child.CustomAddToManagers();

                            child.CustomAddChildren();
                        }
                    }
                }
            }
            else if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    if (child is GraphicalUiElement)
                    {
                        if (child.Parent == null || child.Parent == this)
                        {
                            child.AddToManagers(managers, layer);
                        }
                        else
                        {
                            child.mManagers = this.mManagers;

                            child.CustomAddToManagers();

                            child.CustomAddChildren();
                        }
                    }
                }

                // If a Component contains a child and that child is parented to the screen bounds then we should still add it
                foreach (var child in this.mWhatThisContains)
                {
                    var childGue = child as GraphicalUiElement;

                    // We'll check if this child has a parent, and if that parent isn't part of this component. If not, then
                    // we'll add it
                    if (child.Parent != null && this.mWhatThisContains.Contains(child.Parent) == false)
                    {
                        childGue.AddToManagers(managers, layer);
                    }
                    else
                    {
                        childGue.mManagers = this.mManagers;

                        childGue.CustomAddToManagers();

                        childGue.CustomAddChildren();
                    }
                }
            }
        }


        private void AddContainedRenderableToManagers(ISystemManagers managers, Layer layer)
        {
            // This may be a Screen
            if (mContainedObjectAsIpso != null)
            {
                AddRenderableToManagers?.Invoke(mContainedObjectAsIpso, Managers, layer);

            }
        }

        // todo:  This should be called on instances and not just on element saves.  This is messing up animation
        public void AddExposedVariable(string variableName, string underlyingVariable)
        {
            mExposedVariables[variableName] = underlyingVariable;
        }

        public bool IsExposedVariable(string variableName)
        {
            return this.mExposedVariables.ContainsKey(variableName);
        }

        partial void CustomRemoveFromManagers();

        public void MoveToLayer(Layer layer)
        {
            var layerToRemoveFrom = mLayer;
            if (mLayer == null && mManagers != null)
            {
                layerToRemoveFrom = mManagers.Renderer.Layers[0];
            }

            var layerToAddTo = layer;
            if (layerToAddTo == null)
            {
                layerToAddTo = mManagers.Renderer.Layers[0];
            }

            bool isScreen = mContainedObjectAsIpso == null;
            if (!isScreen)
            {
                if (layerToRemoveFrom != null)
                {
                    layerToRemoveFrom.Remove(mContainedObjectAsIpso);
                }
                layerToAddTo.Add(mContainedObjectAsIpso);
            }
            else
            {
                // move all contained objects:
                foreach (var containedInstance in this.ContainedElements)
                {
                    var containedAsGue = containedInstance as GraphicalUiElement;
                    // If it's got a parent, the parent will handle it
                    if (containedAsGue.Parent == null)
                    {
                        containedAsGue.MoveToLayer(layer);
                    }
                }

            }
        }

        public virtual void RemoveFromManagers()
        {
            foreach (var child in this.mWhatThisContains)
            {
                if (child is GraphicalUiElement)
                {
                    (child as GraphicalUiElement).RemoveFromManagers();
                }
            }

            // if mManagers is null, then it was never added to the managers
            if (mManagers != null)
            {
                RemoveRenderableFromManagers?.Invoke(mContainedObjectAsIpso, mManagers);

                CustomRemoveFromManagers();

                mManagers = null;
            }
        }

        // This is made public so that specific implementations can fall back to it if needed:
        public static void SetPropertyThroughReflection(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
        {
            System.Reflection.PropertyInfo propertyInfo = mContainedObjectAsIpso.GetType().GetProperty(propertyName);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {

                if (value.GetType() != propertyInfo.PropertyType)
                {
                    value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                }
                propertyInfo.SetValue(mContainedObjectAsIpso, value, null);
            }
        }

        /// <summary>
        /// Sets a variable on this object (such as "X") to the argument value
        /// (such as 100.0f). This can be a primitive property like Height, or it can be
        /// a state.
        /// </summary>
        /// <param name="propertyName">The name of the variable on this object such as X or Height. If the property is a state, then the name should be "{CategoryName}State".</param>
        /// <param name="value">The value, casted to the correct type.</param>
        public void SetProperty(string propertyName, object? value)
        {

            if (mExposedVariables.ContainsKey(propertyName))
            {
                string underlyingProperty = mExposedVariables[propertyName];
                int indexOfDot = underlyingProperty.IndexOf('.');
                string instanceName = underlyingProperty.Substring(0, indexOfDot);
                GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = underlyingProperty.Substring(indexOfDot + 1);

                // Children may not have been created yet
                if (containedGue != null)
                {
                    containedGue.SetProperty(variable, value);
                }
            }
            else if (ToolsUtilities.StringFunctions.ContainsNoAlloc(propertyName, '.'))
            {
                int indexOfDot = propertyName.IndexOf('.');
                string instanceName = propertyName.Substring(0, indexOfDot);
                GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = propertyName.Substring(indexOfDot + 1);

                // instances may not have been set yet
                if (containedGue != null)
                {
                    containedGue.SetProperty(variable, value);
                }


            }
            else if (TrySetValueOnThis(propertyName, value))
            {
                // success, do nothing, but it's in an else if to prevent the following else if's from evaluating
            }
            else if (this.mContainedObjectAsIpso != null)
            {
#if FULL_DIAGNOSTICS
                if (SetPropertyOnRenderable == null)
                {
                    throw new Exception($"{nameof(SetPropertyOnRenderable)} must be set on GraphicalUiElement");
                }
#endif
                try
                {
                    SetPropertyOnRenderable(mContainedObjectAsIpso, this, propertyName, value);
                }
                catch (InvalidCastException invalidCastException)
                {
                    throw new InvalidCastException($"Error trying to set {propertyName} to {value} on {mContainedObjectAsIpso}", invalidCastException);
                }
            }
        }

        private bool TrySetValueOnThis(string propertyName, object value)
        {
            bool toReturn = false;
            try
            {
                switch (propertyName)
                {
                    case "AutoGridHorizontalCells":
                        this.AutoGridHorizontalCells = (int)value;
                        break;
                    case "AutoGridVerticalCells":
                        this.AutoGridVerticalCells = (int)value;
                        break;
                    case "ChildrenLayout":
                    case "Children Layout":
                        this.ChildrenLayout = (ChildrenLayout)value;
                        toReturn = true;
                        break;
                    case "ClipsChildren":
                    case "Clips Children":
                        this.ClipsChildren = (bool)value;
                        toReturn = true;
                        break;
#if !FRB && NET6_0_OR_GREATER
                    case "ExposeChildrenEvents":
                        {
                            if (this is InteractiveGue interactiveGue)
                            {
                                interactiveGue.ExposeChildrenEvents = (bool)value;
                                toReturn = true;
                            }
                        }
                        break;
#endif
                    case "FlipHorizontal":
                        this.FlipHorizontal = (bool)value;
                        toReturn = true;
                        break;
#if !FRB && NET6_0_OR_GREATER
                    case "HasEvents":
                        {
                            if (this is InteractiveGue interactiveGue)
                            {
                                interactiveGue.HasEvents = (bool)value;
                                toReturn = true;
                            }
                        }
                        break;
#endif
                    case "Height":
                        this.Height = (float)value;
                        toReturn = true;
                        break;
                    case "HeightUnits":
                    case "Height Units":
                        this.HeightUnits = (DimensionUnitType)value;
                        toReturn = true;
                        break;
                    case nameof(IgnoredByParentSize):
                        this.IgnoredByParentSize = (bool)value;
                        toReturn = true;
                        break;
                    case nameof(MaxHeight):
                        this.MaxHeight = (float?)value;
                        toReturn = true;
                        break;
                    case nameof(MaxWidth):
                        this.MaxWidth = (float?)value;
                        toReturn = true;
                        break;
                    case nameof(MinHeight):
                        this.MinHeight = (float?)value;
                        toReturn = true;
                        break;
                    case nameof(MinWidth):
                        this.MinWidth = (float?)value;
                        toReturn = true;
                        break;
                    case "Parent":
                        {
                            string valueAsString = (string)value;

                            if (!string.IsNullOrEmpty(valueAsString) && mWhatContainsThis != null)
                            {
                                var newParent = this.mWhatContainsThis.GetGraphicalUiElementByName(valueAsString);
                                if (newParent != null)
                                {
                                    Parent = newParent;
                                }
                            }
                            toReturn = true;
                        }
                        break;
                    case "Rotation":
                        this.Rotation = (float)value;
                        toReturn = true;
                        break;
                    case "StackSpacing":
                        this.StackSpacing = (float)value;
                        toReturn = true;
                        break;
                    case "TextureLeft":
                    case "Texture Left":
                        this.TextureLeft = (int)value;
                        toReturn = true;
                        break;
                    case "TextureTop":
                    case "Texture Top":
                        this.TextureTop = (int)value;
                        toReturn = true;
                        break;
                    case "TextureWidth":
                    case "Texture Width":
                        this.TextureWidth = (int)value;
                        toReturn = true;
                        break;
                    case "TextureHeight":
                    case "Texture Height":
                        this.TextureHeight = (int)value;
                        toReturn = true;

                        break;
                    case "TextureWidthScale":
                    case "Texture Width Scale":
                        this.TextureWidthScale = (float)value;
                        toReturn = true;
                        break;
                    case "TextureHeightScale":
                    case "Texture Height Scale":
                        this.TextureHeightScale = (float)value;
                        toReturn = true;
                        break;
                    case "TextureAddress":
                    case "Texture Address":
                        this.TextureAddress = (Gum.Managers.TextureAddress)value;
                        toReturn = true;
                        break;
                    case "Visible":
                        this.Visible = (bool)value;
                        toReturn = true;
                        break;
                    case "Width":
                        this.Width = (float)value;
                        toReturn = true;
                        break;
                    case "WidthUnits":
                    case "Width Units":
                        this.WidthUnits = (DimensionUnitType)value;
                        toReturn = true;
                        break;
                    case "X":
                        this.X = (float)value;
                        toReturn = true;
                        break;
                    case "XOrigin":
                    case "X Origin":
                        this.XOrigin = (HorizontalAlignment)value;
                        toReturn = true;
                        break;
                    case "XUnits":
                    case "X Units":
                        this.XUnits = UnitConverter.ConvertToGeneralUnit(value);
                        toReturn = true;
                        break;
                    case "Y":
                        this.Y = (float)value;
                        toReturn = true;
                        break;
                    case "YOrigin":
                    case "Y Origin":
                        this.YOrigin = (VerticalAlignment)value;
                        toReturn = true;
                        break;
                    case "YUnits":
                    case "Y Units":

                        this.YUnits = UnitConverter.ConvertToGeneralUnit(value);
                        toReturn = true;
                        break;
                    case "Wrap":
                        this.Wrap = (bool)value;
                        toReturn = true;
                        break;
                    case "WrapsChildren":
                    case "Wraps Children":
                        this.WrapsChildren = (bool)value;
                        toReturn = true;
                        break;
                }

                if (!toReturn)
                {
                    var propertyNameLength = propertyName.Length;
                    if (propertyNameLength > 5
                        && propertyName[propertyNameLength - 1] == 'e'
                        && propertyName[propertyNameLength - 2] == 't'
                        && propertyName[propertyNameLength - 3] == 'a'
                        && propertyName[propertyNameLength - 4] == 't'
                        && propertyName[propertyNameLength - 5] == 'S'
                        && value is string)
                    {
                        var valueAsString = value as string;

                        string nameWithoutState = propertyName.Substring(0, propertyName.Length - "State".Length);

                        if (string.IsNullOrEmpty(nameWithoutState))
                        {
                            // This is an uncategorized state
                            if (mStates.ContainsKey(valueAsString))
                            {
                                ApplyState(mStates[valueAsString]);
                                toReturn = true;
                            }
                        }
                        else if (mCategories.ContainsKey(nameWithoutState))
                        {

                            var category = mCategories[nameWithoutState];

                            var state = category.States.FirstOrDefault(item => item.Name == valueAsString);
                            if (state != null)
                            {
                                ApplyState(state);
                                toReturn = true;
                            }
                        }
                    }
                }
            }
            catch (InvalidCastException innerException)
            {
                // There could be some rogue value set to the incorrect type, or maybe
                // a new type or plugin initialized the default to the wrong type. We don't
                // want to blow up if this happens
                // Update October 12, 2023
                // This swallowed exception caused
                // problems for myself and arcnor. I 
                // am concerned there may be other exceptions
                // being swallowed, but maybe we should push those
                // errors up and let the callers handle it.
#if FULL_DIAGNOSTICS
                throw new InvalidCastException($"Trying to set property {propertyName} to a value of {value} of type {value?.GetType()} on {Name}", innerException);
#endif
            }
            return toReturn;
        }

        public void ApplyStateRecursive(string categoryName, string stateName)
        {
            if (mCategories.ContainsKey(categoryName))
            {
                var category = mCategories[categoryName];

                var state = category.States.FirstOrDefault(item => item.Name == stateName);
                if (state != null)
                {
                    ApplyState(state);
                }
            }

            if (Children != null)
            {
                foreach (GraphicalUiElement child in this.Children)
                {
                    child.ApplyStateRecursive(categoryName, stateName);
                }

            }
            else
            {
                foreach (var item in this.mWhatThisContains)
                {
                    item.ApplyStateRecursive(categoryName, stateName);
                }
            }
        }

        public void ApplyState(string name)
        {
            if (mStates.ContainsKey(name))
            {
                var state = mStates[name];

                ApplyState(state);

            }


            // This is a little dangerous because it's ambiguous.
            // Technically categories could have same-named states.
            foreach (var category in mCategories.Values)
            {
                var foundState = category.States.FirstOrDefault(item => item.Name == name);

                if (foundState != null)
                {
                    ApplyState(foundState);
                }
            }
        }

        public void ApplyState(string categoryName, string stateName)
        {
            if (mCategories.ContainsKey(categoryName))
            {
                var category = mCategories[categoryName];

                var state = category.States.FirstOrDefault(item => item.Name == stateName);

                if (state != null)
                {
                    ApplyState(state);
                }
            }
        }

        HashSet<StateSave> statesInStack = new HashSet<StateSave>();
        public virtual void ApplyState(DataTypes.Variables.StateSave state)
        {
            if (statesInStack.Contains(state))
            {
                return; // don't do anything, this would cause infinite recursion
            }
#if FULL_DIAGNOSTICS
            // Dynamic states can be applied in code. It is cumbersome for the user to
            // specify the ParentContainer, especially if the state is to be reused. 
            // I'm removing this to see if it causes problems:
            //if (state.ParentContainer == null)
            //{
            //    throw new InvalidOperationException("State.ParentContainer is null - did you remember to initialize the state?");
            //}
#endif
            statesInStack.Add(state);

            if (state.Apply != null)
            {
                state.Apply();
            }
            else
            {
                if (GraphicalUiElement.IsAllLayoutSuspended == false)
                {
                    this.SuspendLayout(true);
                }

                var variablesWithoutStatesOnParent =
                    state.Variables.Where(item =>
                    {
                        if (item.SetsValue)
                        {
                            // We can set the variable if it's not setting a state (to prevent recursive setting).
                            // Update May 4, 2023 - But if you have a base element that defines a state, and the derived
                            // element sets that state, then we want to allow it.  But should we just allow all states?
                            // Or should we check if it's defined by the base...
                            //return (item.IsState(state.ParentContainer) == false ||
                            //    // If it is setting a state we'll allow it if it's on a child.
                            //    !string.IsNullOrEmpty(item.SourceObject));
                            // let's test this out:
                            return true;

                        }
                        return false;
                    }).ToArray();


                var parentSettingVariables =
                    variablesWithoutStatesOnParent
                        .Where(item => item.GetRootName() == "Parent")
                        .OrderBy(item => GetOrderedIndexForParentVariable(item))
                        .ToArray();

                var nonParentSettingVariables =
                    variablesWithoutStatesOnParent
                        .Except(parentSettingVariables)
                        // Even though we removed state-setting variables on the parent, we still allow setting
                        // states on the contained objects
                        .OrderBy(item => state.ParentContainer == null || !item.IsState(state.ParentContainer))
                        .ToArray();

                var variablesToConsider =
                    parentSettingVariables.Concat(nonParentSettingVariables)
                    .ToArray();

                int variableCount = variablesToConsider.Length;
                for (int i = 0; i < variableCount; i++)
                {
                    var variable = variablesToConsider[i];
                    if (variable.SetsValue && variable.Value != null)
                    {
                        this.SetProperty(variable.Name, variable.Value);
                    }
                }

                foreach (var variableList in state.VariableLists)
                {
                    this.SetProperty(variableList.Name, variableList.ValueAsIList);
                }

                if (GraphicalUiElement.IsAllLayoutSuspended == false)
                {
                    this.ResumeLayout(true);

                }
            }

            statesInStack.Remove(state);

        }

        private int GetOrderedIndexForParentVariable(VariableSave item)
        {
            var objectName = item.SourceObject;
            for (int i = 0; i < ElementSave.Instances.Count; i++)
            {
                if (objectName == ElementSave.Instances[i].Name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void ApplyState(List<DataTypes.Variables.VariableSaveValues> variableSaveValues)
        {
            this.SuspendLayout(true);

            foreach (var variable in variableSaveValues)
            {
                if (variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }
            this.ResumeLayout(true);
        }

        public void AddCategory(DataTypes.Variables.StateSaveCategory category)
        {
#if FULL_DIAGNOSTICS
            if (string.IsNullOrEmpty(category.Name))
            {
                throw new ArgumentException("The category must have its Name set before being added to this");
            }
#endif
            //mCategories[category.Name] = category;
            // Why call "Add"? This makes Gum crash if there are duplicate catgories...
            //mCategories.Add(category.Name, category);
            mCategories[category.Name] = category;
        }

        public void AddStates(List<DataTypes.Variables.StateSave> list)
        {
            foreach (var state in list)
            {
#if FULL_DIAGNOSTICS
                if (state.Name == null)
                {
                    throw new ArgumentException("One of the states being added has a null name - be sure to set the name of all states");
                }
#endif
                // Right now this doesn't support inheritance
                // Need to investigate this....at some point:
                mStates[state.Name] = state;
            }
        }

        // When interpolating between two states,
        // the code is goign to merge the values from
        // the two states to create a 3rd set of (merged)
        // values. Interpolation can happen in complex animations
        // resulting in lots of merged lists being created. This allocates
        // tons of memory. Therefore we create a static set of variable lists
        // to store the merged values. We don't know how deep the stack will go
        // (animations within animations) so we need to support a dynamically growing
        // list. The numberOfUsedInterpolationLists stores how many times this is being
        // called so it knows if it needs to add more lists.
        static List<List<Gum.DataTypes.Variables.VariableSaveValues>> listOfListsForReducingAllocInInterpolation = new List<List<Gum.DataTypes.Variables.VariableSaveValues>>();
        int numberOfUsedInterpolationLists = 0;

        public void InterpolateBetween(Gum.DataTypes.Variables.StateSave first, Gum.DataTypes.Variables.StateSave second, float interpolationValue)
        {
            if (numberOfUsedInterpolationLists >= listOfListsForReducingAllocInInterpolation.Count)
            {
                const int capacity = 20;
                var newList = new List<DataTypes.Variables.VariableSaveValues>(capacity);
                listOfListsForReducingAllocInInterpolation.Add(newList);
            }

            List<Gum.DataTypes.Variables.VariableSaveValues> values = listOfListsForReducingAllocInInterpolation[numberOfUsedInterpolationLists];
            values.Clear();
            numberOfUsedInterpolationLists++;

            Gum.DataTypes.Variables.StateSaveExtensionMethods.Merge(first, second, interpolationValue, values);

            this.ApplyState(values);
            numberOfUsedInterpolationLists--;
        }


        public bool IsPointInside(float x, float y)
        {
            var asIpso = this as IRenderableIpso;

            var absoluteX = asIpso.GetAbsoluteX();
            var absoluteY = asIpso.GetAbsoluteY();

            return
                x > absoluteX &&
                y > absoluteY &&
                x < absoluteX + this.GetAbsoluteWidth() &&
                y < absoluteY + this.GetAbsoluteHeight();
        }


        public void SuspendLayout(bool recursive = false)
        {
            mIsLayoutSuspended = true;

            if (recursive)
            {
                if (this.Children?.Count > 0)
                {
                    var count = Children.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var asGraphicalUiElement = Children[i];
                        asGraphicalUiElement?.SuspendLayout(true);
                    }
                }
                else
                {
                    for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                    {
                        mWhatThisContains[i].SuspendLayout(true);
                    }

                }
            }
        }

        /// <summary>
        /// Clears the layout and font dirty state, resulting in no layout logic being
        /// performed on the next resume layout. This method should only be used 
        /// if you intend to manually perform layouts after a layout resume. Otherwise, calling
        /// this can cause layouts to behave incorrectly
        /// </summary>
        public void ClearDirtyLayoutState()
        {
            currentDirtyState = null;
            isFontDirty = false;
        }

        public void ResumeLayout(bool recursive = false)
        {
            mIsLayoutSuspended = false;

            if (recursive)
            {
                if (!IsAllLayoutSuspended)
                {

                    ResumeLayoutUpdateIfDirtyRecursive();
                }
            }
            else
            {
                if (isFontDirty)
                {
                    if (!IsAllLayoutSuspended)
                    {
                        this.UpdateToFontValues();
                        isFontDirty = false;
                    }
                }
                if (currentDirtyState != null)
                {
                    UpdateLayout(currentDirtyState.ParentUpdateType,
                        currentDirtyState.ChildrenUpdateDepth,
                        currentDirtyState.XOrY);
                }
            }
        }

        private bool ResumeLayoutUpdateIfDirtyRecursive()
        {

            mIsLayoutSuspended = false;
            UpdateFontRecursive();

            var didCallUpdateLayout = false;

            if (currentDirtyState != null)
            {
                didCallUpdateLayout = true;
                UpdateLayout(currentDirtyState.ParentUpdateType,
                    currentDirtyState.ChildrenUpdateDepth,
                    currentDirtyState.XOrY);
            }

            if (this.Children?.Count > 0)
            {
                var count = Children.Count;
                for (int i = 0; i < count; i++)
                {
                    var asGraphicalUiElement = Children[i];
                    asGraphicalUiElement.ResumeLayoutUpdateIfDirtyRecursive();
                }
            }
            else
            {
                int count = mWhatThisContains.Count;
                for (int i = 0; i < count; i++)
                {
                    mWhatThisContains[i].ResumeLayoutUpdateIfDirtyRecursive();
                }
            }

            return didCallUpdateLayout;
        }

        #endregion
    }
}

