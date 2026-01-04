using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ListBoxItemTests : BaseTestClass
{
    [Fact]
    public void CustomVisualTemplate_ShouldHaveSetFromObjectCalled()
    {
        ListBox listBox = new();

        listBox.VisualTemplate = new Gum.Forms.VisualTemplate(() =>        
        {
            return new InGameListBoxItemRuntime();
        });

        listBox.Items.Add("1");

        listBox.ListBoxItems.Count.ShouldBe(1);

        var firstItem = listBox.ListBoxItems[0];
        var asCustom = firstItem as CustomListBoxItem;

        asCustom.ShouldNotBeNull();

        asCustom.HasCalledUpdateToObject.ShouldBeTrue();
    }
}


public class InGameListBoxItemRuntime : ContainerRuntime
{
    public TextRuntime? nameText { get; protected set; }

    public InGameListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if (fullInstantiation)
        {
            nameText = new TextRuntime();
            nameText.Text = "Default";

            this.Height = 20;


            this.AddChild(nameText);

            AfterFullCreation();
        }
    }

    public override void AfterFullCreation()
    {
        if (FormsControlAsObject == null)
        {
            FormsControlAsObject = new CustomListBoxItem(this);
        }
    }
}

public class CustomListBoxItem : ListBoxItem
{

    public CustomListBoxItem(InteractiveGue gue) : base(gue) { }

    public bool HasCalledUpdateToObject { get; private set; }

    public override void UpdateToObject(object o)
    {
        HasCalledUpdateToObject = true;

    }
}
