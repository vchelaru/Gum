# Forms Controls

## Introduction

Forms controls are a collection of classes which provide common UI behavior. You will probably be familiar with many controls. The most common controls include:

* Button&#x20;
* CheckBox
* ComboBox
* Image
* Label
* ListBox
* RadioButton
* ScrollViewer
* Slider
* StackPanel
* TextBox

All controls are in the `MonoGameGum.Forms.Controls` namespace.

{% hint style="info" %}
Forms naming is based on naming from WPF. If you are familiar with WPF or similar libraries like Avalonia, you may find many of the names and concepts familiar.
{% endhint %}

## Common Control Properties

All controls share a few common properties and characteristics. The following list provides a high-level introduction to forms control similarities. If the list doesn't make sense yet don't worry, we'll cover each topic in this and following tutorials.

* All controls can be added to a StackPanel. Technically, any control can be added to any other control, but for this tutorial we'll keep things simple by adding only to a StackPanel.
* All controls have a Visual property which can be used to position, size, and perform advance layout behavior. This Visual property is ultimately a GraphicalUiElement which provides access to all Gum layout properties.
* All controls can receive input from the mouse (usually through the Gum Cursor object). Most controls can also receive focus and input from gamepads.
* All controls support binding by assigning their BindingContext property. Children controls inherit BindingContext from their parents if the BindingContext is not explicitly assigned.

For the rest of this tutorial we'll add a few of the most common controls to our project and show how to work with them. Note that for this tutorial I've removed the Button control from the previous tutorial.

We also assume that your project has a `mainPanel` to hold all controls.

## Forms Control vs Visual

As we'll see below, each forms control has a specific purpose. Buttons are clicked, Labels display read-only strings, and TextBoxes can be used to input text. Each control provides properties and events specific to its purpose, standardizing the way each works.&#x20;

However, each control also wraps a Visual object which gives you full layout control. The Visual property is of type GraphicalUiElement, and it has access to the full Gum layout engine. For example, a button could be made to be as wide as its parents using the following code:

```csharp
// assuming MyButton is a valid Button:
MyButton.Visual.Width = 0;
MyButton.Visual.WidthUnits = DimensionUnitType.RelativeToContainer;
```

For more information about all of the properties available to GraphicalUiElement, see the [General Properties](../../../../gum-tool/gum-elements/general-properties/) section of the Gum tool - all properties in the tool are also available in code.

{% hint style="info" %}
Some Visual properties are also provided at the Forms Control level for convenience. These are:

* `X`
* `Y`
* `Width`
* `Height`

For example, the following two lines of code are equivalent:

```csharp
MyButton.Width = 100;
MyButton.Visual.Width = 100;
```
{% endhint %}

## Code Organization

For the sake of brevity, we will add all of our controls in the game's Initialize method after `mainPanel` has been created. Of course a full game may require more advanced organization but we'll keep everything in the Game Initialize for simplicity.

## Label

Labels are text objects which can display a string. Labels do not have any direct interaction such as responding to clicks. The following code adds a label to the project:

```csharp
var label = new Label();
mainPanel.AddChild(label);
label.Text = $"I was created at {System.DateTime.Now}";
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1TwWrbQBC9-ysG04MMZaH01pBCaxFjqEupHdNbGUtje8lqx8yupKbB_97dtaKqsZNmLxrNe_vem0GqnbY7WOhC2PHWqx8W1Y1gRS3L3dWofglWM8HDXheu57HlWQBndfXYCqW6YancWUNN2XphE5DRod4YXUBh0DmICu_gQ3qOHkYQzkF0g57g0TCnRhe0QIs7Evi563NEcnBYkkRCLG_ncP1x0FM5bbE2Prgm5ZNz8swmqXWyjKcXhmuw1F62z_xeu8lVfymORdar78w-10KFZ7kPAuMOGP-lzt2Ca0dr7fTGUOB4qekEH7t4wj4oUAnckIguCRrWJcyt9hqN_k1nodPMakCI-d5CN3bwqtG4NYnTbNX6_aRbRDwNClSo7Te0ZLqZlx6Lu9TIBkP2LPWpLFccZx3CUcjgphf5Eutn70_32pRZ4g8o6V2t6JcPIm_Gc2jRQSGEcRvo4WF57zxVKg-Nla5IfeX2OFjuBh0N1zB51WJvD2XQy-L3EEVh1xWXt9yxe9IT98vwfxLkgu0r_P_5FNXUEEo2ZcMS_iuxW8MtyWdTDzOdMif9p0lT8yzn8Q_6bFx8HgQAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 21 48.png" alt=""><figcaption><p>Label displaying when it was created</p></figcaption></figure>

The rest of this tutorial assumes that the Label is not removed. It is used to show when events have occurred.

## Button

Button controls are usually added when a user needs to perform a command. Buttons can be clicked with the mouse and gamepad, and their click event can be manually invoked for custom support such as pressing Enter on the keyboard.

The following code creates two buttons. One is disabled so it does not respond to click events:

```csharp
var button = new Button();
mainPanel.AddChild(button);
button.Text = "Click Me";
button.Click += (_, _) => 
    label.Text = $"Button clicked @ {System.DateTime.Now}";

var disabledButton = new Button();
mainPanel.AddChild(disabledButton);
disabledButton.Text = "Disabled Button";
disabledButton.IsEnabled = false;
disabledButton.Click += (_, _) =>
    label.Text = "This never happens";
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAJVUwY7aMBC98xWjqIegrixVvXVFtYV0EVKpqsKi3pBJBrBw7Mh2oNsV_96xk01DgO6uL3HmPc97MxmntEJtYCpSo61eO_ZLcXZveI4HbXa3vfJ_MBsbXmxFahueVnpM4LjMn0O0Zffa5PYswEZaOaMlIb2iXEmRQiq5teAzfIBP4dl76gGtwog9dwjPggnuRYpTrvgGDSw3jQ9PJoUZGk_w24cJDD63YizBNS-lI9WQuVIOmnE_hCpJv5rEMACFh8vysdsK279tDvmyUDn2U2uXCIOp0-aREkQ1EP2jTuxUlxYXwoqVROI4U2IFH2t7RjvKgBnoPRojMoS9FhlMlHCCS_EHz0yHmlmL4P3dQF02aZVc2gUaK7Rii4_9uhF-7bmBnAv1gyuUdc0zx9NdCMStIhsW-5Jlc-1rbcM-keSrJsk3v796frQVMosDv0UJ72yOv11oHn2lHbgtwqp0TisgUB-ijvkaq0SH4eUF1epEi1MFOrpTjM4YFfJ-APHyBpZ9mrKGcWb_XVSZoQmnQ_Q17-Bp9mgd5iyhuZ6LHNl3fTh268mE5TQZ2fDNdZ2ebHFPgabOpA7X2aOrByb2q6qYA1jTJOFV5lsaFM1pSKk6GnLY8qJAZdsd5xbbE91_1R15KDLqbeyvtm8wbOrN5QtTsxtSR_0y_IKDxPDDK_RP_irUNuQmHmmpDf0ijVrTnKMZyrLtqfIc8nedhuCZz-NfFn5KxekFAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 14 20.gif" alt=""><figcaption><p>Buttons only respond to Click if IsEnabled is set to true (default)</p></figcaption></figure>

{% hint style="info" %}
Notice that the Label and two Buttons are stacked top-to-bottom. This is the default behavior layout behavior of StackPanels.

As mentioned earlier, layout-related properties can be accessed through a control's Visual property.

These tutorials focus on the Forms controls themselves but for more information you can look at the different properties available in the [General Properties](../../../../gum-tool/gum-elements/general-properties/) pages.
{% endhint %}

## CheckBox

CheckBox controls allow the user to toggle a bool value by clicking on it. Just like Button, the CheckBoxes support clicking with mouse and gamepad and changing the IsChecked property in code.

The following code creates a CheckBox with two method handlers (Checked/Unchecked):

```csharp
var checkBox = new CheckBox();
mainPanel.AddChild(checkBox);
checkBox.Text = "Click Me";
checkBox.Checked += (_, _) => label.Text = "CheckBox checked";
checkBox.Unchecked += (_, _) => label.Text = "CheckBox unchecked";
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAJVU0W7aMRR95yuueEq0ytK0t1ZMWoOKkIo0rYD2hkxyCRbGF1070G3qv892QpZCtnZ-iXPP8TnnOnYqq0wJM5UzWdo48d1I8cByjyfi3d2g-hcsJiwPW5XblkeGJh6cVPtzyU_FA_HeXhVERsYxaY8MDtVaqxxyLa2FoPARbuNz8GsAfhxYHaVDOBuO8ahynEkjS2RYlW2OQPYOT8iBEKaLKYw-d2pijBtZaeddo3LtHD2TNJZqyzBaYRiBwVO_feK2yqZ37aLQFhonvhG5sWLMHfEPLzBsgOEf6tTOqLK4VFatNXqO4wpr-KWJx-S8AhZAR2RWBcKRVAFTo5ySWv3Eq9CxZ9EhhHw30LTtvSqp7RLZKjJi-SltNiKMo2TYS2W-SoO66fnJyXwXC0mnyZYlvhTFnEKvXTgIabluRR7D_K_rs63SRRL5HUp8F3N8dmHz5lSWfovcFiHfYr5b0zN4nE7Di_wRvfdo7Zw1r2-Yn1d1WOdSGyHzB2UHMxz2cKKN_0ofRpCsbmCVhkP3uoNzknoRFn0yC5P_h1BlrqXW0mL346fvOk6LQ-GvVxJuwVztEcpm0n-2GnZLunDvh99IMGZ5eof_qwsoMo2Sk4w0sf-bsNn484B8r6tupjpz1L9MGotXOV9-A3_xlGMUBQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 17 43.gif" alt=""><figcaption><p>CheckBox responds to clicks</p></figcaption></figure>

## ComboBox

ComboBox provides a collapsible way to display and select from a list of options.

The following code creates a ComboBox which raises an event whenever an item is selected.

```csharp
var comboBox = new ComboBox();
for (int i = 0; i < 20; i++)
{
    comboBox.Items.Add($"Item {i}");
}
comboBox.SelectionChanged += (_, _) =>
{
    label.Text = "Selected: " + comboBox.SelectedObject;
};
mainPanel.AddChild(comboBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1U32_aMBB-5684RXsIorL2462MSWtQEdLQpkHR3pCTHODV8aGzA90q_vfaIaQZ0K5-8eXu8_d9d9iUVpkVTFTGZGnpxC8jxS3LAnfE9_1O-VpZjFhu1iqzDY4MjXxxVBbHlA_FLXFhzxIiIeOYtK90NmWqVQaZltZCYPgA19XeeeyAXxtWW-kQjoJD3KoMJ9LIFTIsVo2PAPYKU-QACOHdGAZfWjkxxKUstfOqFfNBudKMu1XqIBlWQwwDMLi7LB-7tbLdfnMotIXGiZ9EbqgYM0f8xxNEdSF6ho7thEqLc2VVqtFjHJd4KO9re0zOM2AOtEVmlSNsSeUwNsopqdVfPDNd9SxagODvCuq2vVYptZ0jW0VGzD9160GEtZUMhVTmhzSo656nTmb3VSJuNdmgxNc8n1HotV0ORFqmDcm3EL94PlkrnccVvgWpvsUMH1wY3hS1nwNIA8phAb5Eu-jEekZFSjf0UIsm9Wdbd0kMsTIOlAe97_vtM3wMe6_XbUDPswzryCrGXtgGv_G7KMTwqPZRi3vfOTtycO0HnaylWflfsTeAeHEFi66_lC_oXeob82uIoHdKjPn39LffWx5eHfHxeMt0Ki22L0v3TdfvbpP75xiHVzNTBcKqDi7fxRrdgE7UL5f_42DIcvcG_X8erEg0So4T0sT-34fN0l8i5Btdtj0dPFf8p06r5JnP_ROF066BRAUAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 34 57.gif" alt=""><figcaption><p>ComboBox responding to items being selected</p></figcaption></figure>

## ListBox

ListBox provides a way to display a list of items. Each item can be selected.

The following code creates a ListBox which raises an event whenever an item is selected.

```csharp
var listBox = new ListBox();
listBox.Visual.Width = 150;
listBox.Visual.Height = 300;

for (int i = 0; i < 20; i++)
{
    listBox.Items.Add($"Item {i}");
}
listBox.SelectionChanged += (_, _) =>
{
    label.Text = 
        $"Selected item is {listBox.SelectedObject} at index {listBox.SelectedIndex}";
};
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UUW_aMBB-51ecUB-CWll01V7WddIKKkNatWmlbG_IJAfcamxkO9AN5b_vnITULdDVL77cfb7vu_PFuSM9h1tKrXFm5sUvLcWNlUvcGPtw2cpfC4uBlasFpa7BGW0GHBzky52LTXFj7NLtOUTPaG-N4khrlU8VpZAq6RyEDOfwodxb2xbwWllaS4-wI-zjmlK8lVrO0cJk3ugIYGa4QxsAwbwfwtWnyCf6OJO58sxaZq6YS86kU7oqyrCaxHAFGjeH6RO_INe5bA6FslB78cMY3yeLqTf2Dydo14H2E3Tobk3ucEyOpgoZ422OVbio5VnjOQNmYNZoLWUIa0MZDDV5kor-4p7osmYRAYK-M6jLZq5cKjdG68hoMb7o1I0Iay0tLCXp71Kjqmu-8zJ9KB1JVGSDEp-zbGRCrXE4JFJy2iT5Guyj53sLUllS4iNI-S1G-OhD8-5QcR9AaiCPS-CQ2bRfSFfk_LV53HFWXzFrDRBVE8RPyvyC0efvu0cxX5Dmi6DgohuBZsZCQtoDcaR7ydtHeBf209NOA3q6kjjvkOW7UHVy0g42bKloRxqLPSVV6XxbvYXUcx6F0ytIJmcw6fBkH2OLmvcsENZJ3U1OVTaTHGyfk2H2bfqb9wIk16gzfNxHDIO7iKa5eP16q-NRpVPpMJ7Tzpsm_36V8UuQhB92REuEeW0c_g1qdAN6wX44_B8FfSs3b-B_9laInkJpk55RxvLDZ_WM5xfttcpjTZXmMv9LpaVzT2fxDxup_eq_BQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 37 21.gif" alt=""><figcaption><p>ListBox responding to items being selected</p></figcaption></figure>

## RadioButton

RadioButton controls allow the user to view a set of options and pick from one of the available options. Radio buttons are mutually exclusive within their group. Radio buttons can be grouped together by putting them in common containers, such as StackLayouts.

The following creates six radio buttons in two separate groups.

```csharp
var group1 = new StackPanel();
mainPanel.AddChild(group1);

var group2 = new StackPanel();
// move group 2 down slightly:
group2.Y = 10;
mainPanel.AddChild(group2);

var radioButtonA = new RadioButton();
radioButtonA.Text = "Option A";
group1.AddChild(radioButtonA);

var radioButtonB = new RadioButton();
radioButtonB.Text = "Option B";
group1.AddChild(radioButtonB);

var radioButtonC = new RadioButton();
radioButtonC.Text = "Option C";
group1.AddChild(radioButtonC);


var radioButton1 = new RadioButton();
radioButton1.Text = "Option 1";
group2.AddChild(radioButton1);

var radioButton2 = new RadioButton();
radioButton2.Text = "Option 2";
group2.AddChild(radioButton2);

var radioButton3 = new RadioButton();
radioButton3.Text = "Option 3";
group2.AddChild(radioButton3);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI2VTW_iMBCG7_wKi1OQVtYm3LbqSiSoiAPaVUvR9rQyyQBWjR2NHdgP9b-vnaTBG1BqX2LNvJ7nHSeaVJrLPVnxHJVWO0N_SEYfkB3hrPD1blQNpekCWXngue50SqqFTS6q43vIbumDwqO-CtBMSYNK2MyorLaC5yQXTGviKsTkS_0c_R0Ru0rkJ2aAvAPncOI5rJhke0Dyc9_5cGJLeAJ0Ard9XpL7r16MzmHHKmEsta7ckGtmNKlDDdKtrjC5JxLOt_GROXA9uesOubZAGvqolJlzhNwo_G0LjNvE-CJd6pWqNGy45lsBVmOwgib91tpDZWwFKIg6ASIvgJwUL8hScsOZ4H_gynTdM_UEzt8n0rZtWRUTegOouZJ0M520F-HWiSE5Mi6_Mwmi7fnJsPy1DkRek52KzopirVyvftoV2qOqyji8SnbgooiaU31TdTQZrNVI6IsVxZ8_RiR9BLKCq7QyRslZC3q8hHySr6Rr-GXcy_1WGnufZDbuOYovXP_cAD0Npqd9ehpGTwfoWTA969OzMHo2QI-D6XGfHvfpyU361afl5ZJgetKnJ2H0oa9uGkyf9unTMPrUK7dlGvwhMQkaO89lYcdw5Kblmh-B7NvN7RnUqjtRj347_YGDObJzAP-_QU0zAQyjTAmF9q-DcifUGTAVle-p8VzX7zutg1c-3_4B6WgLNTwHAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 39 16.gif" alt=""><figcaption><p>RadioButtons responding to clicks in two different groups</p></figcaption></figure>

## ScrollViewer

ScrollViewer provides a scrollable panel for controls. ScrollViewers are similar in concept to ListBoxes, but they can contain any type of item rather than only ListBoxItems.

The following code creates a ScrollViewer and adds buttons using AddChild.

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.Width = 200;
mainPanel.AddChild(scrollViewer);

for(int i = 0; i < 15; i++)
{
    var button = new Button();
    button.Text = "Button " + i;
    scrollViewer.AddChild(button);
}
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UTY_aMBC98ytGnBJRWWyrXkq3Uhe0iANS1WVpb5VJBhitY6OxDf0Q_752ErKm0HZ9sTPz5r03_oi3pDcwp4KNNWsnvmop7llWeDD8NOr5f6XFlOVuS4U94aa-EveGq8uAGBvt2KiQ6e38SlEBhZLWwjRw3cC7eu796kEYO6a9dAgn9gnuqcC51HKDDN82nWgEB4UH5AiIy8cZ3H5IYmKCa-mVC6o1c6Nca2Z5HWok4-iI4RY0Hq7LZ25LNh91RbEt1E58NsZNiLFwhn8Egn6b6D9DZ3ZuvMUlWVopDBjHHpv0sbXHxgUGLMHskZlKhL2hEmaaHElFP_HCdN2zSADR3yto2w5aXiq7RLZktFi-yduNiGMvGSpJ-pPUqNqeH5wsnupAljTZocTHslyY2GuajkQ2XBClloSHcEQtVxJK4SlUfKHSbUPB6-HwL3rjLakyS4vSJtaGISPtgALJcBSm93DzNsyDQd6BnrfrZHflnTO6NXpXf6QW42ggYoHfXTzPBgR9GACdA8_66Qw35QnnsVutpMX0yPIXXYLHXRkeRRbv7oIqhE27uH4jWnQHGp2rX0__x8GE5eEF-mfPRowVSs7GRhkO_wDWa2XCPt0pn3pqPNf8fzqtgxc-j78BAQgI4bcEAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 41 52.gif" alt=""><figcaption><p>ScrollViewer containing buttons</p></figcaption></figure>

## Slider

Slider controls allow the user to select a value between a minimum and maximum value.

The following code creates a Slider which raises an event whenever its Value changes.

```csharp
var slider = new Slider();
slider.Width = 200;
slider.Minimum = 0;
slider.Maximum = 100;
slider.ValueChanged += (_,_) => 
    label.Text = $"Slider value: {slider.Value:0.0}";
mainPanel.AddChild(slider);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UUW_aMBB-51ec0B6CVll0e6Ni0gYqQhrStFK6N2SSg1h1bHR2oFvFf9_ZSbMMsq5-iX3f5_u-O9spnTI7WKiUrLNbL34YKW5JFni09HjTK1-DxYzkPlepa3jW2BmDs7J4CfFU3Foq3EVATKzxZDUjvX250SqFVEvnIGS4hlH89p57wGNP6iA9wovgFA8qxYU0cocE613jI5BZ4Q4pEML0fg7jT62YmOJWltqzasxcKUfNZBBDlWQYTWIYg8Fjt3zic-UGN82mUBYaL75b66eKMPWWfnKCfg30_1DnbmFLhyvl1EYjczyVWMGn2h5ZzxkwA3tAIpUhHKzKYG6UV1KrX3hhOtYsWoTg7wrqslmrlNqtkJyyRqw-DupGhHGQBIVU5ps0qOua77xMH2MgaRXZsMTnLFvaUGsbDom03DRJvob5P_dPcqWzJPJblLgWS3zyoXkLrh98juA0N4GAMXvsn3mvsdp4XLRFK1g8qMznTPowHF5gC2VWURaMdmDyqcauO3aupC5xkkuz48N6P4ZkfQXrAd-9hnhR1Lt-5ZG9894RPLdTjYZieOq_2rGK3ipwIx22T37wprt0v8_4bSXhCSxVgbCrJ90Xq2Y3pDP1bvg_DqYkj2_Q_-v1iYlGScnEakv8KyGz5QuB9IVb1_JUeY75z53G4IXP02-mLfckEQUAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 43 36.gif" alt=""><figcaption><p>Slider responding to cursor input</p></figcaption></figure>

## TextBox

TextBox controls allow the user to see and edit string values. TextBoxes support typing with the keyboard, copy/paste, selection, and multiple lines of text.

TextBoxes are automatically focused when clicked, but IsFocused can be explicitly set to give focus.

The following code creates a TextBox which raises an event whenever its text is changed. The text is then copied over to a label.

```csharp
var textBox = new TextBox();
textBox.Placeholder = "Enter text here...";
textBox.Width = 200;
textBox.TextChanged += (_, _) => 
    label.Text = $"Text box text is now: {textBox.Text}";
mainPanel.AddChild(textBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UUW_aMBB-51ecoj0ErbK67a0Vk1pYEdKQqpWyvSGTHMSq8aGzA-0q_vtsJ81Swrr6hcvd5-_7zj5TWmXWMFUZk6WVE7-MFDcsN7gnfrjslW-VxZjltlCZbXBkaOyL43LzkvKhuCHe2E5CDMk4Ju0rvW251CqDTEtrITB8gov423vugV9bVjvpEF4ER7hTGU6lkWtkWKwbHwHsFe6QAyCE9xMYfG3lxAhXstTOq0bmSjlqpv2YqiTDaohhAAb3p-VTVyjbv2w2hbbQOPGDyI0UY-aInzxBUheSv9CJnVJpca6sWmr0GMclVuVDbY_JeQbMgXbIrHKEHakcJkY5JbX6jR3TsWfRAgR_Z1C37bVKqe0c2SoyYv6lXx9EWDvJsJHK3EqDuu75zsnsISbSVpMNSlzl-YxCr-1yINJy2ZB8D_E_9w8LpfM04luQ-C1m-OjC4c2etgjKgCsQlvQIvkb75Mi78-BrX6xEZ9VXW7YGiFstMyxI5356PPk3fzHVbiiQUQiRdPf8VLkrPPrz-Xm3GLSGhTRrf1MfB5AuzmDR94PXADsdfUhiEHqJwsqCof0FPLcpD8mbR1ZDWw0upcX23fffNU3329y_rjQ8gpnaIKzr4PRo1egGdKR-uvwfByOW-3fov3p_YqhRcjokTez_TNis_EggX-uy7anyHPmPncZkx-fhD5UBBBQTBQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 09 13.gif" alt=""><figcaption><p>TextBox responding to text input</p></figcaption></figure>
