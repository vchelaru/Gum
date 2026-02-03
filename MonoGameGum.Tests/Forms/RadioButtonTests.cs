using Gum.Wireframe;
using Gum.Forms.Controls;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class RadioButtonTests : BaseTestClass
{
    public RadioButtonTests()
    {
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        RadioButton sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public async Task IsChecked_ShouldUncheckOtherRadioButtons()
    {
        var radioButton1 = new RadioButton();
        radioButton1.AddToRoot();

        var radioButton2 = new RadioButton();
        radioButton2.AddToRoot();

        radioButton1.IsChecked.ShouldBe(false);
        radioButton2.IsChecked.ShouldBe(false);


        radioButton1.IsChecked = true;
        radioButton1.IsChecked.ShouldBe(true);
        radioButton2.IsChecked.ShouldBe(false);

        radioButton2.IsChecked = true;
        radioButton1.IsChecked.ShouldBe(false, "because checking the 2nd should uncheck the first");
        radioButton2.IsChecked.ShouldBe(true);
    }
}
