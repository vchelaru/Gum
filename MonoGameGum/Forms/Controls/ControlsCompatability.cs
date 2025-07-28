using Gum.Forms.Data;
using Gum.Wireframe;
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
    public class VisualTemplate : Gum.Forms.VisualTemplate 
    {
        public VisualTemplate(Type type) : base(type) { }

        public VisualTemplate(Func<GraphicalUiElement> creationFunc) : base(creationFunc) { }

        public VisualTemplate(Func<object, GraphicalUiElement> creationFunc) : base(creationFunc) { }

        public VisualTemplate(Func<object, bool, GraphicalUiElement> creationFunc) : base(creationFunc) { }
    }

    public class FrameworkElementTemplate : Gum.Forms.FrameworkElementTemplate
    {
        public FrameworkElementTemplate(Type type) : base(type) { }
        public FrameworkElementTemplate(Func<Controls.FrameworkElement> creationFunc) : base(creationFunc) { }
    }

    public class Window : Gum.Forms.Window 
    {
        public Window() : base() { }
        public Window(InteractiveGue visual) : base(visual) { }
    }

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

}

namespace MonoGameGum.Forms.Controls
{
    public class Button : Gum.Forms.Controls.Button
    {
        public Button() : base() { }
        public Button(InteractiveGue visual) : base(visual) { }
    }

    public class  ComboBox : Gum.Forms.Controls.ComboBox
    {
        public ComboBox() : base() { }
        public ComboBox(InteractiveGue visual) : base(visual) { }
    }

    public class FrameworkElement : Gum.Forms.Controls.FrameworkElement
    {
        public FrameworkElement() : base() { }
        public FrameworkElement(InteractiveGue visual) : base(visual) { }
    }

    public class ItemsControl : Gum.Forms.Controls.ItemsControl 
    {
        public ItemsControl() : base() { }
        public ItemsControl(InteractiveGue visual) : base(visual) { }
    }

    public class Label : Gum.Forms.Controls.Label 
    {
        public Label() : base() { }
        public Label(InteractiveGue visual) : base(visual) { }
    }

    public class ListBox : Gum.Forms.Controls.ListBox 
    {
        public ListBox() : base() { }
        public ListBox(InteractiveGue visual) : base(visual) { }
    }

    public class ListBoxItem : Gum.Forms.Controls.ListBoxItem 
    {
        public ListBoxItem() : base() { }
        public ListBoxItem(InteractiveGue visual) : base(visual) { }
    }

    public class Menu : Gum.Forms.Controls.Menu 
    {
        public Menu() : base() { }
        public Menu(InteractiveGue visual) : base(visual) { }
    }

    public class MenuItem : Gum.Forms.Controls.MenuItem 
    {
        public MenuItem() : base() { }
        public MenuItem(InteractiveGue visual) : base(visual) { }
    }

    public class Panel : Gum.Forms.Controls.Panel 
    {
        public Panel() : base() { }
        public Panel(InteractiveGue visual) : base(visual) { }
    }

    public class ScrollBar : Gum.Forms.Controls.ScrollBar 
    { 
        public ScrollBar() : base() { }
        public ScrollBar(InteractiveGue visual) : base(visual) { }
    }

    public class ScrollViewer :Gum.Forms.Controls.ScrollViewer 
    {
        public ScrollViewer() : base() { }
        public ScrollViewer(InteractiveGue visual) : base(visual) { }
    }

    public class Slider : Gum.Forms.Controls.Slider 
    {
        public Slider() : base() { }
        public Slider(InteractiveGue visual) : base(visual) { }
    }


    public abstract class TextBoxBase : Gum.Forms.Controls.TextBoxBase 
    {
        public TextBoxBase() : base() { }
        public TextBoxBase(InteractiveGue visual) : base(visual) { }
    }

    public class StackPanel : Gum.Forms.Controls.StackPanel
    {
        public StackPanel() : base() { }
        public StackPanel(InteractiveGue visual) : base(visual) { }
    }


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
    public class Binding : Gum.Forms.Data.Binding 
    {

        public Binding(LambdaExpression propertyExpression) : base(propertyExpression) { }
    }

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
    public class ButtonBase : Gum.Forms.Controls.Primitives.ButtonBase 
    { 
        public ButtonBase() : base() { }
        public ButtonBase(InteractiveGue visual) : base(visual) { }
    }

    public abstract class RangeBase : Gum.Forms.Controls.Primitives.RangeBase 
    {
        public RangeBase() : base() { }
        public RangeBase(InteractiveGue visual) : base(visual) { }
    }

}



