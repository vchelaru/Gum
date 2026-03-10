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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1TUWvbMBB-z684wh4cGIKxt5UOtpiGwDLGkoa9jYt9SY7KUjjJ9rqS_z5JcT2vSbvei093n77vu0OuHZsdLLgQ6-zWqx8G1Y1gRa2Vu6tR_VJbzQQPey7cI25WV-rGSnVeUFNrvFgdOqNDvdFcQKHROZgFrnfwIX1HDyMIcRBu0BM8sufUcEELNLgjgZ-7XjSCg8KSJAJiejuH64-Dmsppi7X2QTUxn5STZjZJpZNkjJ4YrsFQe1k-83t2k6v-UhyLjFffrfU5CxXeyn0gGHeN8V_o3C1s7WjNjjeaAsZLTaf2sbMn1gcGKsE2JMIlQWO5hLlhz6j5N52ZTjOrASD6ewvd2EGrRu3WJI6tUev3k24RMRoUqJDNNzSku5mXHou7VMgGQ_Yo9aksVzbOOmxHIo2bnuRLzJ-9P92zLrOEH0DSWa3olw8kb8ZzaNFBIYRxG-jhYXnvPFUqD4UVV6S-2vY4WO4GHQ3XMHnVYm8PZeDL4nuIpLDrkstb7tA96In65fZ_HOSC7Sv0_3mKaqoJJZtabSX8V2K22rYkn3U99HTynPifOk3FM5_HP4wYMvkLBAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAJVUUW_aMBB-51ecoj0ErbI07W0VUwdZEdKYpkHR3pBJDrBw7Mh2YF3Ff9_ZSbMQYG39Evvuu_u-u5xdWqE2MBWp0VavHfulOLs3PMeDNrvbXvk_NxsbXmxFap9x4zJn99rk5wY20soZLcnTK8qVFCmkklsLY8r1AT6Fb--pB7QKI_bcITxnT3AvUpxyxTdoYLlpSD2YGGZoPMBvHyYw-NyysQTXvJSOWEPmijlwxv1gqij9ahLDABQeLtPHbits_7YJ8mWhcuyn1i4RBlOnzSMliGpH9A86sVNdWlwIK1YSCeNMiZX7WMsz2lEGzEDv0RiRIey1yGCihBNcij94JjrUzFoAr-8G6rKJq-TSLtBYoRVbfOzXjfBrzw3kXKgfXKGsa545nu6CIW4V2aDYlyyba19r2-0TSb5qknzz-6vxo62QWRzwLUg4szn-dqF59Jd24LYIq9I5rYCc-hB1xNe-inQYDi-wVhEtTGXo8E4xOkNUnvcDiJc3sOzTlDWIM_nvokoMTTgF0d-8g6fZo3WYs4Tmei5yZN_14ditJxOW02RkwzfXdRrZwp46mjqT2lxnj64GTOxXVSEHsKZJwqvItzQomtOQUnU05LDlRYHKtjvOLbYnuv-qO_JQZNTb2F9t32DY1JvLF6ZGN6AO-2X3CwoSww-v4D95VahtyE080lIbeiKNWtOcoxnKsq2p0hzyd5UG45nO41-otd4J1gUAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAJVUTYvbMBC951cMOdl0EZTedkmh67AhsIHSTUJvQbYntogshZGcbFv2v1eSP-pN3O5WF0szT--9kUaujVAFrERG2ui9Zd8VZw_EKzxrOtxN6n-l2YL4sRSZ6XCLunIPmqrrAEu0sqSly0yOdSpFBpnkxsDCcX2E2_Cd_JqAG0cSJ24ROvY5nkSGK654gQS7ohf1YKfwhOQBfrpZwuzzIMbmuOe1tE41MDfKQTOKQ6iR9KMnhhkoPI_LR7YUJr7rN_myUFn2TWs7F4SZ1fTDEUzbxPQPdGlWuja4FUakEh3GUo1N-qW1R9o6BsxBn5BI5AgnLXJYKmEFl-InXpkONbMBwPu7gbZsp1VzabZIRmjFtp_i9iD8OHGCigv1lSuUbc1PlmeHEIgGRfYo9iXP19rXOkx7IsnTnuTRz_-6PymFzKOAH0DCmq3x2frDW-uicEdkS4SsxOyQ6mdweX2eXvgP2XuXbZSTdvmGeLdrgOpCvYXENcoBVjgdwQQZd0sfZhDtbmAX-6Z7XUHnpNmE-RjNRmX_QVSra6qUGxxefvyudtocc_e8Iv8K1qJCKNrJeG-16B50oT6efsPBnPj5HfqvHiBLJHKKEi01ub8Jqb3rB6R7WQ89NZ4D_6XTELzy-fIb2-zOKAEFAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1U32_aMBB-5684RXsIorL2462MSWtQEdLQpkHR3pCTHHCrY6OzA90q_vfaIaQZ0K5-sXP3-fu-O86UlvQKJpSxsWbpxC8txS3LAneG7_ud8rW0GLHcrCmzR9yoLMSt4eI8IBKjHRvlM51NmSrKIFPSWhh5rg9wXe2dxw74tWHaSodwZB_iljKcSC1XyLBYNaIB7BWmyAEQjndjGHxpxcQQl7JUzqtWzAflSjPuVqGDZFgNMQxA4-6yfOzWZLv95lIoC7UTP41xQ2LMnOE_niCqE9EzdGwnprQ4J0upQo9xXOIhva_tsXGeAXMwW2SmHGFrKIexJkdS0V88M13VLFqA4O8K6rK9VimVnSNbMlrMP3XrRoS1lQyFJP1DalR1zVMns_sqELeKbFDia57PTKi1nQ5ESqYNybdwfvF-siaVxxW-Bam-xQwfXGjeFJXvA0gN5LAAnzK76MR6ZorU3JiHWjSpP9u6S8MQk3ZAHvS-77fP8DHsvV63AT33Mqwjqxh7YRv8xu-icIZH2kct7n3n7MrBtW90spZ65X_F3gDixRUsun4oX9C7VDfm1xBB75QY8-_pb7-3PLza4uP1lulUWmwPS_dN43e3yf1zjMOrmVGBsKoPl2exRjegE_XL6f84GLLcvUH_nwcrEoWS48Qow_7fh_XSDxHyjSrbng6eK_5Tp1XwzOf-Cf1V7XQxBQAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UTY_aMBC98ytGaA9BrCy2q15KqdQFLUUqalVY2hsyyQDTNTayHaBF-e-1k5A1X9v1xc7M83tvxo5TQ3IBQ4q1Mmpu2S_J2aPmK9wq_dyupa-lWV_z9ZJic8D10xV7VHp1HmBdJa1WwmVq63QmKIZYcGOg77ju4EM-1_Y1cGOtacMtwoG9hxuKccglX6CG6aIS9WCnMELtAX75NIDOpyDGejjnqbBONWculHPNqJGHCkk_KmLogMTtZfnILsk02tUmXxZKy34oZXukMbZK_3EE9TJRf4EOzFClBidkaCbQYaxOsUhnpT2trGPABNQGtaYEYaMogYEkS1zQXzwzndfMAoD3dwtl2U4r5cJMUBtSkk3uG2Uj_NhwDStO8juXKMqaR5bHz3kgCoqsUOxzkoyVrzVMeyLBZxXJV7--ur-7JJFEOT6A5N9sjDvrmzdC4foAXAJZXIFLqW39xLogYx_U7qBZfIWqJYAVTWA_KbFLh75737qK-YK0WHoH960ANFcaIpIWyGVabTd9hHd-bjYbFejlSELegbNvfNXRTd2vYU9ZPfCYnTkpSnen1V1yuXBXodmBaHoL04a72dfUguYdJfy4KbvpqPJmkoH9sRgm32a_3ZwBdzXKBHfniIEPZ8Ftzl4_3mJ7UOmMGwzvaeNNN_9pnbiXIPI_7JhWCItycfk3KNEV6ET9cvo_Dnqab9-gf_RWsK5ArqOuEkq7h0_Lubu_qB9EGnoqPOf8p07z4JnP7B-X8HXyrAUAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI2VTW_iMBCG7_wKi1OQVtYm3LbqSiSoiAPaVUvR9rQyyRCsGjsaO7Af6n9f56PgNSi1L0Qzb97nnRBNas1lSVY8R6XVztAfktEHZAc4KXy9G9VDbbpAVu15rt91i_pAHxQergs0U9KgErYzquqt4DnJBdOaLKxXTL60v6O_I2JPhfzIDJB39zkceQ4rJlkJSH6WZ2gjtoQnwEbQXD4vyf1Xp0bnsGO1MJbaOnfklhlN2lKHbM7ZmNwTCafb-MjsuZ7cnW9qxgJp6KNSZs4RcqPwtzUY943xRbrUK1Vr2HDNtwKsxmANXfutj4fKWAcoiDoCIi-AHBUvyFJyw5ngf-AqdDszdQRNvk-kH9uyaib0BlBzJelmOukfRHOODMmBcfmdSRD9zE-G5a9tIXKGPKvorCjWqpnVbTdGJaq6isNdsj0XRdTd5Ydqq8mgVyehL1YUf_4YkfgIZAVXaW2MkrMe9HgpuSRXSdfwyzR_7rfK2OdJZmMvUXzhuvcN0NNgeurT0zB6OkDPgumZT8_C6NkAPQ6mxz499unJTfrVq-X0kmB64tOTMPrQWzcNpk99-jSMPnXstkyDuyQmQWvnuSrsGo6abbnmByBlf3F7B_Xqs8ij325_kGCO7BTA_29R00wAwyhTQqH96qDcCXUCTEXtZuoyt_5-0rZ4lfPtH49QDEspBwAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UTY_aMBC98ytGnBJRWWyrXkq3Uhe0iANS1WVpb5VJBhitY6OxDf0Q_752ErKm0HZ9sTPz5r03_oi3pDcwp4KNNWsnvmop7llWeDD8NOr5f6XFlOVuS4U94aa-EveGq8uAGBvt2KiQ6e38SlEBhZLWwjRw3cC7eu796kEYO6a9dAgn9gnuqcC51HKDDN82nWgEB4UH5AiIy8cZ3H5IYmKCa-mVC6o1c6Nca2Z5HWok4-iI4RY0Hq7LZ25LNh91RbEt1E58NsZNiLFwhn8Egn6b6D9DZ3ZuvMUlWVopDBjHHpv0sbXHxgUGLMHskZlKhL2hEmaaHElFP_HCdN2zSADR3yto2w5aXiq7RLZktFi-yduNiGMvGSpJ-pPUqNqeH5wsnupAljTZocTHslyY2GuajkQ2XBClloSHcEQtVxJK4SlUfKHSbUPB6-HwL3rjLakyS4vSJtaGISPtgALJcBSm93DzNsyDQd6BnrfrZHflnTO6NXpXf6QW42ggYoHfXTzPBgR9GACdA8_66Qw35QnnsVutpMX0yPIXXYLHXRkeRRbv7oIqhE27uH4jWnQHGp2rX0__x8GE5eEF-mfPRowVSs7GRhkO_wDWa2XCPt0pn3pqPNf8fzqtgxc-j78BAQgI4bcEAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1U0W7aMBR95yuu0B6CVll0e6Ni0gYqQhrStFK6N2SSC7Hq2OjagW4V_75rJ80yyLr6Jc49x-ec69gpnTI7WKiUrLNbL34YKW5JFni09HjTK1-DxYzkPlepe-HNykLcWiouC2JijSerGenty41WKaRaOgcz1rqGUXz2nnvAY0_qID3Ci_oUDyrFhTRyhwTrXWMayOxwhxQIYXo_h_GnVk1McStL7dk1KlfO0TMZxFJlGUYjDGMweOy2T3yu3OCmWRTaQuPFd2v9VBGm3tJPFujXQP8Pde4WtnS4Uk5tNDLHU4kVfKrjkfWsgBnYAxKpDOFgVQZzo7ySWv3Ci9CxZ9EihHxXULfNXqXUboXklDVi9XFQb0QYB0lQSGW-SYO67vnOy_QxFpJWkw1LfM6ypQ29tuEgpOWmEfka5v9cP8mVzpLIb1Hiu1jikw-bt-D-wecITvMmEDBmj_2z7DVWB48vbdMKFg8q8zmTPgyHF9hCGVWUBaMdmHyqseuOlSupS5zk0uz4Y70fQ7K-gvWAz15DvGjqXb_KyNl57Qie21KjoRie-q_uWEVvNbiRDttffvCms3S_z_huJeEKLFWBsKsn3QerZjekM_du-D8JpiSPb_D_6_aJiUZJycRqS_wrIbPlA4H0hbeulanKHPXPk8biRc7TbzgWYon-BAAA)

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAEAI1UUW_aMBB-51ecoj0ErbK67a0Vk1pYEdKQqpWyvSGTHORUY6OzA-0q_vtsJ81Swrr6JfZ9n7_vzj6ntKTXMKWMjTUrJ35pKW5YbnBv-OGyV74FizHLbUGZfeGNy424MbzpBsTQaMdGeaS3LZeKMsiUtBbGXusTXMRv77kHfmyZdtIhvKiPcEcZTqWWa2RYrBvTQPYOd8iBEKb3Exh8bcXECFeyVM67RuXKOXqm_RiqLMNohGEAGven7VNXkO1fNptCWaid-GGMGxFj5gw_eYGkBpK_1ImdmtLinCwtFXqO4xIr-FCnx8Z5BczB7JCZcoSdoRwmmhxJRb-xk3SsWbQIIb8zqMv2XqVUdo5syWgx_9KvDyKMnWTYSNK3UqOqa75zMnuIgbRVZMMSV3k-M6HWNhyElFw2It_D_J_7hwWpPI38FiWuxQwfXTi82dMWgTS4AmFpHsFjZp8c5e48-dqDlemsWrVta4K4VTLDwqjcd48X_-YvptoNBTIKIZLunp-Uu8KzP5-fd8HgNSykXvub-jiAdHEGi75vvIbYqehDEiehlmhMFrTZX8BzW_KQvHlkNbVV4FJabN99_13ddL_N_etKwyOY0QZhXU9Ot1bNbkhH7qfh_2QwYrl_h_-r9yeGCiWnQ6MM-58J65VvCeRrVbZzqnKO-seZxmAnz8MfsOLNSgAFAAA)

<figure><img src="../../../../.gitbook/assets/13_08 09 13.gif" alt=""><figcaption><p>TextBox responding to text input</p></figcaption></figure>
