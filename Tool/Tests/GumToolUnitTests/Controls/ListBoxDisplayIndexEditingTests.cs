using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.Controls;

/// <summary>
/// A pooled ListBoxDisplay (see gum-tool-variable-grid: SingleDataUiContainer recycles
/// displayer controls across InstanceMembers) must not carry a stale "editing index" from
/// a previous bind into a new one - and the very first bind is itself such a transition,
/// since the backing field starts at -1 rather than null. Either case previously made the
/// very next "Add" go through the edit-in-place branch instead of appending, indexing a
/// list that doesn't have that many entries yet.
/// </summary>
public class ListBoxDisplayIndexEditingTests : BaseTestClass
{
    [StaFact]
    public void HandleAddTextItem_ShouldAppend_OnFreshlyConstructedDisplay()
    {
        var backingValue = new List<int>();
        var display = new ListBoxDisplay();
        display.InstanceMember = MakeIntListMember(() => backingValue, v => backingValue = (List<int>)v);

        InvokeHandleAddTextItem(display, "5");

        backingValue.ShouldBe(new List<int> { 5 });
    }

    [StaFact]
    public void HandleAddTextItem_ShouldAppend_AfterRebindingToADifferentShorterList()
    {
        // Simulates SingleDataUiContainer pooling: this ListBoxDisplay was previously used to
        // edit index 2 of some other (longer) list-typed InstanceMember, then got reassigned
        // to a new, shorter list without the user ever confirming or cancelling that edit.
        var firstBackingValue = new List<int> { 1, 2, 3 };
        var display = new ListBoxDisplay();
        display.InstanceMember = MakeIntListMember(() => firstBackingValue, v => firstBackingValue = (List<int>)v);
        SetIndexEditing(display, 2);

        var secondBackingValue = new List<int>();
        display.InstanceMember = MakeIntListMember(() => secondBackingValue, v => secondBackingValue = (List<int>)v);

        InvokeHandleAddTextItem(display, "9");

        secondBackingValue.ShouldBe(new List<int> { 9 });
    }

    private static InstanceMember MakeIntListMember(Func<object?> get, Action<object> set)
    {
        var member = new InstanceMember { Name = "TestListMember" };
        member.CustomGetTypeEvent += _ => typeof(List<int>);
        member.CustomGetEvent += _ => get();
        member.CustomSetPropertyEvent += (_, args) => set(args.Value!);
        return member;
    }

    private static void InvokeHandleAddTextItem(ListBoxDisplay display, string text)
    {
        var method = typeof(ListBoxDisplay).GetMethod(
            "HandleAddTextItem",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        method!.Invoke(display, new object[] { text });
    }

    private static void SetIndexEditing(ListBoxDisplay display, int index)
    {
        var field = typeof(ListBoxDisplay).GetField(
            "indexEditing",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.ShouldNotBeNull();
        field!.SetValue(display, (int?)index);
    }
}
