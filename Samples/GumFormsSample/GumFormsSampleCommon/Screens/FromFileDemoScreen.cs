using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultFromFileVisuals;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens;

public class FromFileDemoScreen : BindableGue
{
    [ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("DemoScreenGum", typeof(FromFileDemoScreen));
    }

    GraphicalUiElement _root;
    public void Initialize(GumProjectSave gumProject)
    {


        PopulateListBox();

        PopulateComboBox();

        InitializeRadioButtons();
    }

    private void PopulateComboBox()
    {
        //var comboBox = (InteractiveGue)_root.GetGraphicalUiElementByName("ComboBoxInstance");
        var comboBoxForms = this.GetFrameworkElementByName<ComboBox>("ComboBoxInstance");

        comboBoxForms.Items.Add("Easy");
        comboBoxForms.Items.Add("Medium");
        comboBoxForms.Items.Add("Hard");
        comboBoxForms.Items.Add("Impossible");
    }

    private void InitializeRadioButtons()
    {
        var radioButton = (InteractiveGue)this.GetGraphicalUiElementByName("RadioButtonInstance");
        var radioButtonForms = radioButton.FormsControlAsObject as RadioButton;
        radioButtonForms.IsChecked = true;
    }

    private void PopulateListBox()
    {
        var listBoxVisual = (InteractiveGue)this.GetGraphicalUiElementByName("ResolutionBox");
        var listBox = listBoxVisual.FormsControlAsObject as ListBox;

        listBox.Items.Add("400x300");
        listBox.Items.Add("600x800");
        listBox.Items.Add("1024x768");
        listBox.Items.Add("1280x720");
        listBox.Items.Add("1920x1080");
        listBox.Items.Add("2560x1440");
        listBox.Items.Add("3840x2160");
        listBox.Items.Add("7680x4320");
    }


}
