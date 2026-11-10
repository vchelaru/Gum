using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Fails on any change to the public or protected members ListBox and ListBoxItem declare, so moving
/// their selection logic into an internal helper (#7843) stays invisible to callers and subclasses.
/// On a deliberate surface change, the failing test writes the new snapshot next to the test output
/// (<c>*.actual.txt</c> in the temp folder); review it and paste it into the expected constant.
/// </summary>
public class ListBoxSurfaceSnapshotTests
{
    [Fact]
    public void ListBox_PublicAndProtectedMembers_MatchSnapshot()
    {
        AssertSnapshot(typeof(ListBox), ListBoxSnapshot);
    }

    [Fact]
    public void ListBoxItem_PublicAndProtectedMembers_MatchSnapshot()
    {
        AssertSnapshot(typeof(ListBoxItem), ListBoxItemSnapshot);
    }

    private static void AssertSnapshot(Type type, string expected)
    {
        string actual = Describe(type);
        string normalizedExpected = expected.Replace("\r\n", "\n").Trim();
        if (actual != normalizedExpected)
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), type.Name + "Surface.actual.txt"), actual);
        }
        actual.ShouldBe(normalizedExpected);
    }

    private static string Describe(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        List<string> lines = new() { "base " + type.BaseType };

        foreach (MemberInfo member in type.GetMembers(flags))
        {
            string? line = member switch
            {
                FieldInfo field when IsVisible(field.IsPublic, field.IsFamily, field.IsFamilyOrAssembly) =>
                    $"field {Access(field.IsPublic)}{(field.IsStatic ? " static" : "")} {field.FieldType} {field.Name}",
                PropertyInfo property => DescribeProperty(property),
                EventInfo evt when evt.AddMethod is MethodInfo add && IsVisible(add) =>
                    $"event {Access(add.IsPublic)}{Modifiers(add)} {evt.EventHandlerType} {evt.Name}",
                ConstructorInfo ctor when IsVisible(ctor.IsPublic, ctor.IsFamily, ctor.IsFamilyOrAssembly) =>
                    $"ctor {Access(ctor.IsPublic)}({Parameters(ctor)})",
                MethodInfo method when !method.IsSpecialName && IsVisible(method) =>
                    $"method {Access(method.IsPublic)}{Modifiers(method)} {method.ReturnType} {method.Name}({Parameters(method)})",
                Type nested when nested.IsNestedPublic || nested.IsNestedFamily || nested.IsNestedFamORAssem =>
                    $"nested {nested.Name}",
                _ => null
            };
            if (line != null)
            {
                lines.Add(line);
            }
        }

        return string.Join("\n", lines.OrderBy(line => line, StringComparer.Ordinal));
    }

    private static string? DescribeProperty(PropertyInfo property)
    {
        MethodInfo? getter = property.GetMethod;
        MethodInfo? setter = property.SetMethod;
        bool getterVisible = getter != null && IsVisible(getter);
        bool setterVisible = setter != null && IsVisible(setter);
        if (!getterVisible && !setterVisible)
        {
            return null;
        }
        MethodInfo accessor = getterVisible ? getter! : setter!;
        string accessors = (getterVisible ? $" get:{Access(getter!.IsPublic)}" : "") +
            (setterVisible ? $" set:{Access(setter!.IsPublic)}" : "");
        string indexParameters = string.Join(",", property.GetIndexParameters().Select(p => p.ParameterType));
        return $"property{Modifiers(accessor)} {property.PropertyType} {property.Name}[{indexParameters}]{accessors}";
    }

    private static bool IsVisible(MethodBase method) =>
        IsVisible(method.IsPublic, method.IsFamily, method.IsFamilyOrAssembly);

    private static bool IsVisible(bool isPublic, bool isFamily, bool isFamilyOrAssembly) =>
        isPublic || isFamily || isFamilyOrAssembly;

    private static string Access(bool isPublic) => isPublic ? "public" : "protected";

    private static string Modifiers(MethodInfo method)
    {
        if (method.IsStatic)
        {
            return " static";
        }
        if (!method.IsVirtual || method.IsFinal)
        {
            return "";
        }
        if (method.IsAbstract)
        {
            return " abstract";
        }
        return method.GetBaseDefinition().DeclaringType == method.DeclaringType ? " virtual" : " override";
    }

    private static string Parameters(MethodBase method) =>
        string.Join(", ", method.GetParameters().Select(p => p.ParameterType.ToString()));

    private const string ListBoxSnapshot = """
        base Gum.Forms.Controls.ItemsControl
        ctor public()
        ctor public(Gum.Wireframe.InteractiveGue)
        event public System.Action`1[Gum.Input.GamepadButton] ControllerButtonPushed
        event public System.Action`2[System.Object,Gum.Wireframe.SelectionChangedEventArgs] SelectionChanged
        field protected System.Boolean isItemTypeSetExplicitly
        field protected System.Collections.Generic.List`1[Gum.Forms.Controls.ListBoxItem] ListBoxItemsInternal
        method protected override Gum.Forms.Controls.FrameworkElement CreateNewItemFrameworkElement(System.Object)
        method protected override System.Boolean IsAuthoredRow(Gum.Forms.Controls.FrameworkElement)
        method protected override System.Void AdoptAuthoredRow(Gum.Forms.Controls.FrameworkElement, System.Int32)
        method protected override System.Void HandleCollectionItemMoved(System.Int32, System.Int32)
        method protected override System.Void HandleCollectionItemRemoved(System.Int32, Gum.Forms.Controls.FrameworkElement)
        method protected override System.Void HandleCollectionItemsRemoved()
        method protected override System.Void HandleCollectionNewItemCreated(Gum.Forms.Controls.FrameworkElement, System.Int32)
        method protected override System.Void HandleCollectionReplace(System.Int32)
        method protected override System.Void HandleCollectionReset()
        method protected override System.Void HandleCreatedItemVisual(Gum.Wireframe.GraphicalUiElement, System.Object)
        method protected override System.Void HandleInnerPanelChildMovedExternally(System.Int32)
        method protected override System.Void HandleItemsCollectionChanged(System.Object, System.Collections.Specialized.NotifyCollectionChangedEventArgs)
        method protected override System.Void ReactToVisualChanged()
        method public System.Void DoKeyboardAction(Gum.Wireframe.IInputReceiverKeyboard)
        method public System.Void HandleCharEntered(System.Char)
        method public System.Void HandleKeyDown(Gum.Forms.Input.Keys, System.Boolean, System.Boolean, System.Boolean)
        method public System.Void LoseFocus()
        method public System.Void ReceiveInput()
        method public System.Void ScrollIndexIntoView(System.Int32, Gum.Forms.Controls.ScrollIntoViewStyle)
        method public System.Void ScrollIntoView(System.Object, Gum.Forms.Controls.ScrollIntoViewStyle)
        method public override System.Void OnFocusUpdate()
        method public override System.Void OnFocusUpdatePreview(Gum.Wireframe.RoutedEventArgs)
        method public override System.Void OnGainFocus()
        method public override System.Void OnLoseFocus()
        method public override System.Void UpdateState()
        method public static System.Void ShowPopupListBox(Gum.Forms.Controls.ScrollViewer, Gum.Wireframe.GraphicalUiElement, System.Boolean)
        property Gum.Forms.Controls.DragDropReorderMode DragDropReorderMode[] get:public set:public
        property Gum.Forms.Controls.SelectionMode SelectionMode[] get:public set:public
        property Gum.Wireframe.IInputReceiver NextInTabSequence[] get:public set:public
        property System.Boolean CanListItemsLoseFocus[] get:public set:public
        property System.Boolean DoListItemsHaveFocus[] get:public set:public
        property System.Boolean LoseListItemFocusOnPrimaryInput[] get:public set:public
        property System.Boolean TakingInput[] get:public
        property System.Collections.Generic.List`1[Gum.Forms.Input.Keys] IgnoredKeys[] get:public
        property System.Collections.IList SelectedItems[] get:public
        property System.Collections.ObjectModel.ReadOnlyCollection`1[Gum.Forms.Controls.ListBoxItem] ListBoxItems[] get:public
        property System.Int32 SelectedIndex[] get:public set:public
        property System.Object SelectedObject[] get:public set:public
        property System.Single AdditionalOffsetToCheckForDPadNavigation[] get:public set:public
        property System.Type ItemFormsType[] get:protected set:protected
        property System.Type ListBoxItemFormsType[] get:public set:public
        property System.Type ListBoxItemGumType[] get:public set:public
        property override System.Boolean IsEnabled[] get:public set:public
        property override System.Boolean IsFocused[] get:public set:public
        property override System.String DisplayMemberPath[] get:public set:public
        property static Gum.Forms.Input.Keys AlternateRangeSelectionModifierKey[] get:public set:public
        property static Gum.Forms.Input.Keys AlternateToggleSelectionModifierKey[] get:public set:public
        property static Gum.Forms.Input.Keys RangeSelectionModifierKey[] get:public set:public
        property static Gum.Forms.Input.Keys ToggleSelectionModifierKey[] get:public set:public
        """;

    private const string ListBoxItemSnapshot = """
        base Gum.Forms.Controls.FrameworkElement
        ctor public()
        ctor public(Gum.Wireframe.InteractiveGue)
        event public System.EventHandler Clicked
        event public System.EventHandler Pushed
        event public System.EventHandler Selected
        field protected RenderingLibrary.Graphics.IText coreText
        method protected override System.Void ReactToVisualChanged()
        method protected override System.Void ReactToVisualRemoved()
        method protected override System.Void RefreshInternalVisualReferences()
        method public override System.String ToString()
        method public override System.Void UpdateState()
        method public virtual System.Void UpdateToObject(System.Object)
        property System.Boolean IsHighlighted[] get:public set:public
        property System.Boolean IsSelected[] get:public set:public
        """;
}
