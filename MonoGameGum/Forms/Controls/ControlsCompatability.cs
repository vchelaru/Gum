using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

// These classes were added late July 2025 to support older projects.
// Eventually these will be removed and the new namespace will be used.
// But until then , we need to support both namespaces.

namespace MonoGameGum.Forms
{



    public enum DefaultVisualsVersion
    {
        /// <summary>
        /// The first version introduced with the first version of Gum Forms.
        /// Most controls use solid colors and ColoredRectangles for their backgrounds.
        /// </summary>
        V1,
        /// <summary>
        /// The second version introduced mid 2025. This version uses NineSlices for backgrounds,
        /// and respects a centralized styling.
        /// </summary>
        V2
    }

    [Obsolete("Use the version this class in Gum.Forms instead")]
    public class VisualTemplate : Gum.Forms.VisualTemplate 
    {
        public VisualTemplate(Type type) : base(type) { }

        public VisualTemplate(Func<GraphicalUiElement> creationFunc) : base(creationFunc) { }

        public VisualTemplate(Func<object, GraphicalUiElement> creationFunc) : base(creationFunc) { }

        public VisualTemplate(Func<object, bool, GraphicalUiElement> creationFunc) : base(creationFunc) { }
    }

    [Obsolete("Use the version this class in Gum.Forms instead")]
    public class FrameworkElementTemplate : Gum.Forms.FrameworkElementTemplate
    {
        public FrameworkElementTemplate(Type type) : base(type) { }
        public FrameworkElementTemplate(Func<Controls.FrameworkElement> creationFunc) : base(creationFunc) { }
    }

    public class FormsUtilities
    {
        public static Cursor Cursor => Gum.Forms.FormsUtilities.Cursor;
        public static MonoGameGum.Input.Keyboard Keyboard => Gum.Forms.FormsUtilities.Keyboard;
        public static MonoGameGum.Input.GamePad[] Gamepads => Gum.Forms.FormsUtilities.Gamepads;
    }

    [Obsolete("Use the version this class in Gum.Forms instead")]
    public static class GraphicalUiElementFormsExtensions
    {
        public static FrameworkElementType GetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : Controls.FrameworkElement
        {
            var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

#if DEBUG
            if (frameworkVisual == null)
            {
                throw new ArgumentException("Could not find a GraphicalUiElement with the name " + name);
            }
#endif

            var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

#if DEBUG

            if (frameworkVisualAsInteractiveGue == null)
            {
                throw new ArgumentException("The GraphicalUiElement with the name " + name + " is not an InteractiveGue");
            }

#endif
            var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

#if DEBUG

            if (formsControlAsObject == null)
            {
                throw new ArgumentException("The GraphicalUiElement with the name " + name + " does not have a FormsControlAsObject");
            }
#endif
            var frameworkElement = formsControlAsObject as FrameworkElementType;
            if (frameworkElement == null)
            {
#if DEBUG
                var message = "The GraphicalUiElement with the name " + name +
                    " is expected to be of type " + typeof(FrameworkElementType) + " but is instead " + formsControlAsObject?.GetType();

                throw new ArgumentException(message);
#endif
            }
            return frameworkElement;
        }

        public static FrameworkElementType TryGetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : Controls.FrameworkElement
        {
            var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

            if (frameworkVisual == null)
            {
                return default(FrameworkElementType);
            }

            var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

            if (frameworkVisualAsInteractiveGue == null)
            {
                return default(FrameworkElementType);
            }

            var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

            if (formsControlAsObject == null)
            {
                return default(FrameworkElementType);
            }

            var frameworkElement = formsControlAsObject as FrameworkElementType;
            if (frameworkElement == null)
            {
                return default(FrameworkElementType);

            }
            return frameworkElement;
        }

    }

    [Obsolete("Use the version this class in Gum.Forms instead")]
    public enum TextWrapping
    {
        // todo - support wrap with overflow

        /// <summary>
        /// No line wrapping is performed.
        /// </summary>
        NoWrap = 1,

        /// <summary>
        /// Line-breaking occurs if the line overflows beyond the available block width,
        /// even if the standard line breaking algorithm cannot determine any line break
        /// opportunity, as in the case of a very long word constrained in a fixed-width
        /// container with no scrolling allowed.
        /// </summary>
        Wrap = 2
    }
    [Obsolete("Use the version this class in Gum.Forms instead")]
    public class Window : Gum.Forms.Window
    {
        public Window() : base() { }
        public Window(InteractiveGue visual) : base(visual) { }
    }

}

namespace MonoGameGum.Forms.Controls
{
    [Obsolete("Use Gum.Forms.Controls.Button instead")]
    public class Button : Gum.Forms.Controls.Button
    {
        public Button() : base() { }
        public Button(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.CheckBox instead")]
    public class CheckBox : Gum.Forms.Controls.CheckBox
    {
        public CheckBox() : base() { }
        public CheckBox(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.ComboBox instead")]
    public class  ComboBox : Gum.Forms.Controls.ComboBox
    {
        public ComboBox() : base() { }
        public ComboBox(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.DragDropReorderMode instead")]
    public enum DragDropReorderMode
    {
        NoReorder,
        Immediate
    }

    [Obsolete("Use Gum.Forms.Controls.FrameworkElement instead")]
    public class FrameworkElement : Gum.Forms.Controls.FrameworkElement
    {
        public FrameworkElement() : base() { }
        public FrameworkElement(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.Image instead")]
    public class Image : Gum.Forms.Controls.Image
    {
        public Image() : base() { }
    }

    [Obsolete("Use Gum.Forms.Controls.ItemsControl instead")]
    public class ItemsControl : Gum.Forms.Controls.ItemsControl
    {
        public new ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)(int)base.VerticalScrollBarVisibility;
            set => base.VerticalScrollBarVisibility = (Gum.Forms.Controls.ScrollBarVisibility)(int)value;
        }

        public ItemsControl() : base() { }
        public ItemsControl(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.KeyCombo instead")]
    public struct KeyCombo
    {
        public Keys PushedKey { get; set; }
        public Keys? HeldKey { get; set; }
        public bool IsTriggeredOnRepeat { get; set; }



        // Implicit conversion from KeyCombo to KeyCombo2
        public static implicit operator KeyCombo(Gum.Forms.Controls.KeyCombo original)
        {
            return new KeyCombo
            {
                PushedKey = original.PushedKey,
                HeldKey = original.HeldKey,
                IsTriggeredOnRepeat = original.IsTriggeredOnRepeat
            };
        }

        // Implicit conversion from KeyCombo2 to KeyCombo
        public static implicit operator Gum.Forms.Controls.KeyCombo(KeyCombo keyCombo2)
        {
            return new Gum.Forms.Controls.KeyCombo
            {
                PushedKey = keyCombo2.PushedKey,
                HeldKey = keyCombo2.HeldKey,
                IsTriggeredOnRepeat = keyCombo2.IsTriggeredOnRepeat
            };
        }
    }

    [Obsolete("Use Gum.Forms.Controls.Label instead")]
    public class Label : Gum.Forms.Controls.Label
    {
        public Label() : base() { }
        public Label(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.ListBox instead")]
    public class ListBox : Gum.Forms.Controls.ListBox
    {
        public new ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)(int)base.VerticalScrollBarVisibility;
            set => base.VerticalScrollBarVisibility = (Gum.Forms.Controls.ScrollBarVisibility)(int)value;
        }

        public DragDropReorderMode DragDropReorderMode
        {
            get => (DragDropReorderMode)(int)base.DragDropReorderMode;
            set => base.DragDropReorderMode = (Gum.Forms.Controls.DragDropReorderMode)(int)value;
        }

        public ListBox() : base() { }
        public ListBox(InteractiveGue visual) : base(visual) { }

        public new void ScrollIntoView(object item, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView) =>
            base.ScrollIntoView(item, (Gum.Forms.Controls.ScrollIntoViewStyle)(int)scrollIntoViewStyle);

        public void ScrollIndexIntoView(int itemIndex, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView) =>
            base.ScrollIndexIntoView(itemIndex, (Gum.Forms.Controls.ScrollIntoViewStyle)(int)scrollIntoViewStyle);
    }

    [Obsolete("Use Gum.Forms.Controls.ListBoxItem instead")]
    public class ListBoxItem : Gum.Forms.Controls.ListBoxItem
    {
        public ListBoxItem() : base() { }
        public ListBoxItem(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.Menu instead")]
    public class Menu : Gum.Forms.Controls.Menu
    {
        public Menu() : base() { }
        public Menu(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.MenuItem instead")]
    public class MenuItem : Gum.Forms.Controls.MenuItem
    {
        public MenuItem() : base() { }
        public MenuItem(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.Orientation instead")]
    public enum Orientation
    {
        Horizontal = 0,
        Vertical = 1
    }

    [Obsolete("Use Gum.Forms.Controls.Panel instead")]
    public class Panel : Gum.Forms.Controls.Panel
    {
        public Panel() : base() { }
        public Panel(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.PasswordBox instead")]
    public class PasswordBox : Gum.Forms.Controls.PasswordBox
    {
        public new TextWrapping TextWrapping
        {
            get => (TextWrapping)(int)base.TextWrapping;
            set => base.TextWrapping = (Gum.Forms.TextWrapping)(int)value;
        }

        public PasswordBox() : base() { }
        public PasswordBox(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.RadioButton instead")]
    public class RadioButton : Gum.Forms.Controls.RadioButton
    {
        public RadioButton() : base() {}
        public RadioButton(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.ResizeBehavior instead")]
    public enum ResizeBehavior
    {
        Rows,
        Columns
    }

    [Obsolete("Use Gum.Forms.Controls.ScrollBar instead")]
    public class ScrollBar : Gum.Forms.Controls.ScrollBar
    { 
        public ScrollBar() : base() { }
        public ScrollBar(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.ScrollBarVisibility instead")]
    public enum ScrollBarVisibility
    {
        /// <summary>
        /// The ScrollBar displays only if needed based on the size of the inner panel
        /// </summary>
        Auto = 1,
        /// <summary>
        /// The ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
        /// </summary>
        Hidden = 2,
        /// <summary>
        /// The ScrollBar always displays
        /// </summary>
        Visible = 3
    }

    [Obsolete("Use Gum.Forms.Controls.ScrollIntoViewStyle instead")]
    public enum ScrollIntoViewStyle
    {
        BringIntoView,

        Top,
        Center,
        Bottom
    }

    [Obsolete("Use Gum.Forms.Controls.ScrollViewer instead")]
    public class ScrollViewer :Gum.Forms.Controls.ScrollViewer
    {
        public new ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)(int)base.VerticalScrollBarVisibility;
            set => base.VerticalScrollBarVisibility = (Gum.Forms.Controls.ScrollBarVisibility)(int)value;
        }

        public ScrollViewer() : base() { }
        public ScrollViewer(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.Slider instead")]
    public class Slider : Gum.Forms.Controls.Slider
    {
        public Slider() : base() { }
        public Slider(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.Splitter instead")]
    public class Splitter : Gum.Forms.Controls.Splitter
    {
        public new ResizeBehavior? ResizeBehavior
        {
            get => base.ResizeBehavior == null
                ? null
                : (ResizeBehavior)(int)base.ResizeBehavior;
            set
            {
                if(value == null)
                {
                    base.ResizeBehavior = null;
                }
                else
                {
                    base.ResizeBehavior = (Gum.Forms.Controls.ResizeBehavior) (int)value;
                }
            }
        }
        public Splitter() : base() { }
        public Splitter(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.StackPanel instead")]
    public class StackPanel : Gum.Forms.Controls.StackPanel
    {
        public new Orientation Orientation
        {
            get  => (Orientation)(int)base.Orientation;
            set => base.Orientation = (Gum.Forms.Controls.Orientation)(int)value;
        }

        public StackPanel() : base() { }
        public StackPanel(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.TextBoxBase instead")]
    public abstract class TextBoxBase : Gum.Forms.Controls.TextBoxBase
    {
        public new TextWrapping TextWrapping
        {
            get => (TextWrapping)(int)base.TextWrapping;
            set => base.TextWrapping = (Gum.Forms.TextWrapping)(int)value;
        }

        public TextBoxBase() : base() { }
        public TextBoxBase(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.TextBox instead")]
    public class TextBox : Gum.Forms.Controls.TextBox
    {
        public new TextWrapping TextWrapping
        {
            get => (TextWrapping)(int)base.TextWrapping;
            set => base.TextWrapping = (Gum.Forms.TextWrapping)(int)value;
        }

        public TextBox() : base() { }
        public TextBox(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.TextCompositionEventArgs instead")]
    public class TextCompositionEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The new text value.
        /// </summary>
        public string Text { get; }
        public TextCompositionEventArgs(string text) { Text = text; }
    }

    [Obsolete("Use Gum.Forms.Controls.ToggleButton instead")]
    public class ToggleButton : Gum.Forms.Controls.ToggleButton
    {
        public ToggleButton() : base() { }
        public ToggleButton(InteractiveGue visual) : base(visual) { }
    }
    [Obsolete("Use Gum.Forms.Controls.UserControl instead")]
    public class UserControl : Gum.Forms.Controls.UserControl
    {
        public UserControl() : base() { }
        public UserControl(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use Gum.Forms.Controls.FrameworkElementExt instead")]
    public static class FrameworkElementExt
    {
        public static void SetBinding(this FrameworkElement element, string uiProperty, LambdaExpression propertyExpression) =>
            element.SetBinding(uiProperty, BinderHelpers.ExtractPath(propertyExpression));

        public static void SetBinding<T>(this FrameworkElement element, string uiProperty, Expression<Func<T, object?>> propertyExpression) =>
            element.SetBinding(uiProperty, BinderHelpers.ExtractPath(propertyExpression));

        public static FrameworkElement? GetFrameworkElement(this FrameworkElement element, string name) =>
            element.Visual?.GetFrameworkElementByName<FrameworkElement>(name);

        public static T? GetFrameworkElement<T>(this FrameworkElement element, string name) where T : FrameworkElement
        {
            return element.Visual?.GetFrameworkElementByName<T>(name);
        }

        public static IInputReceiver? GetParentInputReceiver(this FrameworkElement element)
        {
            var parentGue = element.Visual.Parent as GraphicalUiElement;

            while (parentGue != null)
            {
                if (parentGue is IInputReceiver receiver)
                {
                    return receiver;
                }
                if (parentGue is InteractiveGue interactiveGue &&
                    interactiveGue.FormsControlAsObject is IInputReceiver found)
                {
                    return found;
                }
                parentGue = parentGue.Parent as GraphicalUiElement;
            }
            return null;

        }


        public static void RemoveFromRoot(this FrameworkElement element)
        {
            element.Visual.Parent = null;
            element.Visual.RemoveFromManagers();
        }


    }



}

namespace MonoGameGum.Forms.Data
{
    [Obsolete("Use the version this class in Gum.Forms.Data instead")]
    public class Binding : Gum.Forms.Data.Binding
    {

        public Binding(LambdaExpression propertyExpression) : base(propertyExpression) { }
    }

    [Obsolete("Use the version this class in Gum.Forms.Data instead")]
    public static class BindingOperations
    {
        public static BindingExpressionBase? GetBindingExpression(Controls.FrameworkElement element, string uiPropertyName)
            => element.PropertyRegistry.GetBindingExpression(uiPropertyName);

        public static void ClearBinding(Controls.FrameworkElement element, string uiPropertyName)
            => element.PropertyRegistry.ClearBinding(uiPropertyName);
    }
}

namespace MonoGameGum.Forms.Controls.Primitives
{
    [Obsolete("Use the version this class in Gum.Forms.Primitives instead")]
    public class ButtonBase : Gum.Forms.Controls.Primitives.ButtonBase
    { 
        public ButtonBase() : base() { }
        public ButtonBase(InteractiveGue visual) : base(visual) { }
    }

    [Obsolete("Use the version this class in Gum.Forms.RangeBase instead")]
    public abstract class RangeBase : Gum.Forms.Controls.Primitives.RangeBase
    {
        public RangeBase() : base() { }
        public RangeBase(InteractiveGue visual) : base(visual) { }
    }

}



