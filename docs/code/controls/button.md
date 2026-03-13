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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2QP2vDMBDFd4O_w1V0SHAo6dClJkMaStyhSwm0BS22dSRqpRNYclpq_N2rP4aYaJB0Pz09vdOQZwDsxe57zR7BdT2uItGoG-ysZ-ypd84QNHEpOUly0CrZfu9M77cbWHvI6WykgKomoXAXThem-cLWgUUS2K3g-Yzktt3RQu2nJaeBE_hxsSqKMqH01N0Bf4P9LWfREAUMF_EITmq0nPk7I0uhJUknayX_MOROLt6A8AdSE4ulV0_uWyEO5s0YN4cfXv4Q-pnqz6v6XQp38ux-PYMVyuMpJJ0rp_CcVaiUgVdDZl9rvImBJ01sC4rN_NtKlmdjnv0DxahSepgBAAA" target="_blank">Try on XnaFiddle.NET</a>
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl3NMYvCQBAF4D6Q__BuuUJJEK-wUSLcWaiFjQinsCDRDGa4ZBd0omLIf79NLBLdat_He0zpe4BaXuZFrsaQc0FhI2xYOM74QY7VNT7jUIhYgwiGbvhpQq8_0ebpg-8k2di1tdLFrauPhm3eveVfTiR19jXs4IL4lMpbc0P3mrRaUJZZrKyx8zinD61ch43gmPHxb2YL943QGc5qRxChtw-x7yOaalNqA_faSRC4QU2v1z61auaUoGzLFYRzujSXq4nyvcr3_gEi64GGRwEAAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}
{% endtabs %}



<figure><img src="../../.gitbook/assets/13_08 53 05.gif" alt=""><figcaption><p>Button responding to clicks by incrementing clickCount</p></figcaption></figure>

## Clicking Programmatically

Clicking can be performed programmatically by calling PerformClick. The following example shows how to click a button when the Enter key is pressed:

```csharp
// Update
var keyboard = FormsUtilities.Keyboard;
if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
{
    button.PerformClick();
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2RwWoCMRCG74LvMOS0UlntoZeKB1u0ShGkWGohl-xmdFN3J5LMalvx3ZvdtSjmkDBfZubPPzm2WwBi5l_KQjwCuxK7NSmwSND5wMRTyWwJkvoYSJK0t0bDVJHO8Tk36TayyRemDB5Jo-vCeI_EI7fxoMLWkXSUBGH1epDVVZBWZZJOolEzZNio3PxiJdgowRAID9CoR50g3PB4pPXSvlnL13AV0h_6l_jzJv4wmrPA7vtXcIpmk_FN5hK_KyRFbQ3mKMXlsmF3w2v3g7OJcqcV1wb2ysEWfxKrnA6tJtYV_p1NHkyij1_PN6GrWUf_eRVelD5DHc1N6qy3a45XpOKJUwUerNvGM9qVXOX5eEyMrnOZ7Pl5C3TrINZ8SjWdk2i3Tu3WH3KJQcHmAQAA" target="_blank">Try on XnaFiddle.NET</a>

Optionally you can pass the input device to the PerformClick method:

```csharp
// Update
button.PerformClick(keyboard);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2QzWrDMBCE7wa_w6JTTEOSHnqpySEtaVNKoZSUpqCLbG1iNbZUpLX7E_Lula0Um-ggsd_u7DA6xBEAe3D3dcWugWyN445UWGVonWfspiYyGrLuSbnmujFKwkpoWeJtqfL9yGQfmBM41BLtGJYNalrYnQPhr4TrA9fgz3QKRaeCvJVxfWTBTWlFSpTqF1vD4ARz0PgFwX2UeOPAJwsp1-bFGBrCjR-_mvX1-1n9piQVnl3OBnCFalfQ2eQav1vEWRcNnpCzvhnYxXyYPj2FqD-loC5AIyzs8Sczwkq_6s7Yyr2SKn1IdJPHU6ff-ox262fCX_4Lk5TF0TGO_gD1L_hFogEAAA" target="_blank">Try on XnaFiddle.NET</a>
