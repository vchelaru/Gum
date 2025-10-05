using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumFormsSample.CustomRuntimes;
using GumRuntime;
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens;

internal class ComplexListBoxItemScreen : ContainerRuntime
{
    public ComplexListBoxItemScreen()
    {
        this.Width = 0;
        this.WidthUnits = DimensionUnitType.RelativeToParent;
        this.Height = 0;
        this.HeightUnits = DimensionUnitType.RelativeToParent;


        var listBox = new ListBox();
        this.Children.Add(listBox.Visual);
        listBox.X = 30;
        listBox.Y = 30;
        listBox.Width = 400;
        listBox.Height = 400;

        // assign the template before adding new list items
        listBox.VisualTemplate =
            new Gum.Forms.VisualTemplate(() =>
                // do not create a forms object because this template will be
                // automatically added to a ListBoxItem by the ListBox:
                new WeaponListBoxItemRuntime(fullInstantiation: true, tryCreateFormsObject: false));

        listBox.ListBoxItemFormsType = typeof(WeaponListBoxItem);

        for (int i = 0; i < 20; i++)
        {
            var weaponViewModel = new WeaponViewModel
            {
                Name = $"Weapon {i}",
                Damage = 10 + i,
                RemainingDurability = 100 - i,
                MaxDurability = 100,
                Level = i
            };
            listBox.Items.Add(weaponViewModel);
        }
    }
}

class WeaponViewModel 
{
    public string Name { get; set; }
    public int Damage { get; set; }
    public int RemainingDurability { get; set; }
    public int MaxDurability { get; set; }
    public int Level { get; set; }
}


class WeaponListBoxItem : ListBoxItem
{
    public WeaponListBoxItem(InteractiveGue gue) : base(gue) { }
    public override void UpdateToObject(object o)
    {
        var weaponViewModel = (WeaponViewModel)o;

        var view = this.Visual as WeaponListBoxItemRuntime;

        view.NameTextInstance.Text = weaponViewModel.Name;
        view.DamageTextInstance.Text = $"Damage: {weaponViewModel.Damage}";
        view.DurabilityTextInstance.Text = $"Durability: {weaponViewModel.RemainingDurability}/{weaponViewModel.MaxDurability}";
        view.LevelTextInstance.Text = $"Level: {weaponViewModel.Level}";
    }
}

public partial class WeaponListBoxItemRuntime : InteractiveGue
{
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime NameTextInstance { get; protected set; }
    public TextRuntime DamageTextInstance { get; protected set; }
    public TextRuntime DurabilityTextInstance { get; protected set; }
    public TextRuntime LevelTextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public WeaponListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetComponent("Controls/WeaponListBoxItem");

            element.SetGraphicalUiElement(this, SystemManagers.Default);
        }

    }
    public override void AfterFullCreation()
    {
        Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        NameTextInstance = this.GetGraphicalUiElementByName("NameTextInstance") as TextRuntime;
        DamageTextInstance = this.GetGraphicalUiElementByName("DamageTextInstance") as TextRuntime;
        DurabilityTextInstance = this.GetGraphicalUiElementByName("DurabilityTextInstance") as TextRuntime;
        LevelTextInstance = this.GetGraphicalUiElementByName("LevelTextInstance") as TextRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;

        if(FormsControlAsObject == null)
        {
            FormsControlAsObject = new WeaponListBoxItem(this);
        }
    }


}