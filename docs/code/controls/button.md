# Button

## Introduction

The Button control providing an event for handling clicks.

## Code Example: Adding a Button

The following code adds a button which increments every time it is clicked:

{% tabs %}
{% tab title="Separate Method" %}
```csharp
Button button;

void SomeInitializationFunction()
{
    ...
    button = new Button();
    button.AddToRoot();
    button.X = 50;
    button.Y = 50;
    button.Width = 100;
    button.Height = 50;
    button.Text = "Hello MonoGame!";
    int clickCount = 0;
    button.Click += HandleClick
}

void HandleClick(object sender, EventArgs args)
{
    clickCount++;
    button.Text = $"Clicked {clickCount} times";
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2QP2vDMBDFd4O_w1V0SHAo6dClJkMaStyhSwm0BS22dSRqpRNYclpq_N2rP4aYaJB0Pz09vdOQZwDsxe57zR7BdT2uItGoG-ysZ-ypd84QNHEpOUly0CrZfu9M77cbWHvI6WykgKomoXAXThem-cLWgUUS2K3g-Yzktt3RQu2nJaeBE_hxsSqKMqH01N0Bf4P9LWfREAUMF_EITmq0nPk7I0uhJUknayX_MOROLt6A8AdSE4ulV0_uWyEO5s0YN4cfXv4Q-pnqz6v6XQp38ux-PYMVyuMpJJ0rp_CcVaiUgVdDZl9rvImBJ01sC4rN_NtKlmdjnv0DxahSepgBAAA)
{% endtab %}

{% tab title="Lambda" %}
```csharp
// Initialize
var button = new Button();
button.AddToRoot();
button.X = 50;
button.Y = 50;
button.Width = 100;
button.Height = 50;
button.Text = "Hello MonoGame!";
int clickCount = 0;
button.Click += (_, _) =>
{
    clickCount++;
    button.Text = $"Clicked {clickCount} times";
};
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACl3NMYvCQBAF4D6Q__BuuUJJEK-wUSLcWaiFjQinsCDRDGa4ZBd0omLIf79NLBLdat_He0zpe4BaXuZFrsaQc0FhI2xYOM74QY7VNT7jUIhYgwiGbvhpQq8_0ebpg-8k2di1tdLFrauPhm3eveVfTiR19jXs4IL4lMpbc0P3mrRaUJZZrKyx8zinD61ch43gmPHxb2YL943QGc5qRxChtw-x7yOaalNqA_faSRC4QU2v1z61auaUoGzLFYRzujSXq4nyvcr3_gEi64GGRwEAAA)
{% endtab %}
{% endtabs %}



<figure><img src="../../.gitbook/assets/13_08 53 05.gif" alt=""><figcaption><p>Button responding to clicks by incrementing clickCount</p></figcaption></figure>

## Clicking Programmatically

Clicking can be performed programmatically by calling `PerformClick`. The following example shows how to click a button when the Enter key is pressed. The Click handler updates the button text with the current time:

```csharp
// Initialize
button.Click += HandleClick;

// ...

void HandleClick(object sender, EventArgs args)
{
    button.Text = $"Clicked at {DateTime.Now:T}";
}
```

```csharp
// Update
var keyboard = GumService.Default.Keyboard;
if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
{
    button.PerformClick();
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAACCmVRUU_CMBD-K5fFhy0hzSTxBcIDCAoxGqJLxGQv3XqDytaS9jpUwn-3bFMJ9qHNXb_77vvuDsHC3rsqGJBx2AsqrDI0NhgEE0ekFWTNM0xVqmotBcy5EiXeljLfhjp7x5zAohJoejCrUdHYrC1wf0WpOqQK_GkpWIIfBCO4Sl0c9_sNAwrgBIcpJ0xkhexJ7wfJsQX4lsegF0glSfJSfqHX1DJ5EoV7aAWGkQd2HcZCJPpZazpPrjz8Jv6L3y7iVylo43PX8VlyjnK9oQtkZ6CVtzRoLcwUoQHSkJ_s_ArvChqPLX4yOp_c0BtzO-Fte1M1N7DFz0xzIzy9X8YLmlrmyKZYcFcSe-h-PbMswh_sKb10doMifJS50VYXxFaKszvDK9xrs2ULtXNNuWWN0ujfUpZoCm2qdp9RM_PjN-gHLUoTAgAA)

Optionally you can pass the input device to the `PerformClick` method. The input device is then available in the Click handler through `InputEventArgs`, which is useful to determine how the button was activated:

```csharp
// Initialize
button.Click += HandleClick;

// ...

void HandleClick(object sender, EventArgs args)
{
    var device = "Unknown";
    if (args is Gum.Wireframe.InputEventArgs inputEventArgs)
    {
        if (inputEventArgs.InputDevice is MonoGameGum.Input.Keyboard)
            device = "Keyboard";
        else if (inputEventArgs.InputDevice is MonoGameGum.Input.GamePad)
            device = "GamePad";
    }
    button.Text = $"Clicked at {DateTime.Now:T} via {device}";
}
```

```csharp
// Update
var keyboard = GumService.Default.Keyboard;
if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
{
    button.PerformClick(keyboard);
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAACCp1TTWvjMBD9K4PpwYZg0sJeEnpom36xdAnblLbgi2KNG23sUZFkZ9vg_96R5dRJLgurg82Mn968mXneRvf2tq6iiTM1jqIKqyUaG02iy9o5TbDsXtOMMmq0knAnSJZ4Vap8HevlH8wdWCSJZgTXDZK7MG8WBD-SjLYZAZ9GGJDYqBzhHLJ6PD47e6I16Q2FYBpgqoDYXwRlgRWlz8pgYUSF6T29125gVwdhEm73tXZEh5jAMAsamP5Bk75lZl-m-5T-xI-lFkYmA40_R7J3qAPd_mBp8b_q-mgu_lG2Bx1UbcMrrCdd4F_H8JMA6baDEoSD7Uw4XCge4i-9mSxaaJSAbeBvvwnbaBQpUk6JUn0iLz_QMiPhBoIT4oSBfbkLKRf6t9ZuP_nC8B_jIX49ip-VdCvOnY73kneo3lbuCNl3E-TNDVoL1-TQgNOQ-96-hfcXuoYD_vJ836JTbqx-lzwDbsr7cN1vkOl5CY9o_CDSGRaiLgcXMLMq4h3Wp-e1XaGMH1RutNWFS19IpDfenRtt1oOJbNopTQb39xLnaAptqvDj7JiTbvbtFxjUizmEAwAA)
