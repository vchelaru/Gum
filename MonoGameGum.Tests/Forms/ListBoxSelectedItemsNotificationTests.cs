using System.Collections.Generic;
using System.ComponentModel;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ListBox raises PropertyChanged for SelectedItems when the selection changes, so a binding reading it sees
/// the change.
/// </summary>
public class ListBoxSelectedItemsNotificationTests : BaseTestClass
{
    [Fact]
    public void SelectionChange_RaisesPropertyChangedForSelectedItems()
    {
        ListBox listBox = new();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        List<string?> raised = new();
        ((INotifyPropertyChanged)listBox).PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        listBox.SelectedIndex = 1;

        raised.ShouldContain(nameof(ListBox.SelectedItems));
    }
}
