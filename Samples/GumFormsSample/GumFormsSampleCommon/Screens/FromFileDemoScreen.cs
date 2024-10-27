using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultFromFileVisuals;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens;

internal class FromFileDemoScreen
{
    GraphicalUiElement _root;
    public void Initialize(ref GraphicalUiElement root)
    {

        var gumProject = GumProjectSave.Load("FormsGumProject/GumProject.gumx");
        ObjectFinder.Self.GumProjectSave = gumProject;
        gumProject.Initialize();
        RegisterFormRuntimeDefaults();

        FileManager.RelativeDirectory = "Content/FormsGumProject/";

        // This assumes that your project has at least 1 screen

        _root = gumProject.Screens.Find(item => item.Name == "DemoScreenGum").ToGraphicalUiElement(
            SystemManagers.Default, addToManagers: true);
        root = _root;

        PopulateListBox();

        PopulateComboBox();

        InitializeRadioButtons();
    }

    private void PopulateComboBox()
    {
        var comboBox = (InteractiveGue)_root.GetGraphicalUiElementByName("ComboBoxInstance");
        var comboBoxForms = comboBox.FormsControlAsObject as ComboBox;

        comboBoxForms.Items.Add("Easy");
        comboBoxForms.Items.Add("Medium");
        comboBoxForms.Items.Add("Hard");
        comboBoxForms.Items.Add("Impossible");
    }

    private void InitializeRadioButtons()
    {
        var radioButton = (InteractiveGue)_root.GetGraphicalUiElementByName("RadioButtonInstance");
        var radioButtonForms = radioButton.FormsControlAsObject as RadioButton;
        radioButtonForms.IsChecked = true;
    }

    private void PopulateListBox()
    {
        var listBoxVisual = (InteractiveGue)_root.GetGraphicalUiElementByName("ResolutionBox");
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

    private void RegisterFormRuntimeDefaults()
    {
        foreach(var component in ObjectFinder.Self.GumProjectSave.Components)
        {
            if(component.Categories.Any(item => item.Name == "ButtonCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileButtonRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "CheckBoxCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileCheckBoxRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "ComboBoxCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileComboBoxRuntime));
            }
            else if(component.Categories.Any(item => item.Name == "ListBoxCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileListBoxRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "PasswordBoxCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFilePasswordBoxRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "RadioButtonCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileRadioButtonRuntime));
            }
            else if(component.Categories.Any(item => item.Name == "ScrollBarCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileScrollBarRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "SliderCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileSliderRuntime));
            }
            else if (component.Categories.Any(item => item.Name == "TextBoxCategory"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileTextBoxRuntime));
            }
        }
    }
}
