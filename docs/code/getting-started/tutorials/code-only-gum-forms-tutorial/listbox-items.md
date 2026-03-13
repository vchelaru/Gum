# ListBox Items

## Introduction

`ListBoxes` are one of the more complex controls in Gum Forms. This document discusses how to work with and customize `ListBox` items.

{% hint style="info" %}
This document uses the ListBox control for consistency but most of the same code applies to ComboBox. This document mentions where ComboBox differs from ListBox.
{% endhint %}

## Default ToString Implementation

By default whenever an instance is added to the `ListBox` `Items` property, the `ListBox` creates a `ListBoxItem` internally to display the object. Notice that the following code adds integers to a `ListBox's` `Items` property which internally creates UI elements to represent the integers.

```csharp
// Initialize
var listBox = new ListBox();
for (int i = 0; i < 10; i++)
{
    listBox.Items.Add(i);
}
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2NsQrCMBCG9z7FkSmlUnS1OqiDFBxEHbMEG_FocoH2qmLpu5vU4uAtx33_ffx9AiDKdt85sQRuOjOLAAkZtcW3CVQ8dANOIx01GQtrIPOEM-trPQKZFop-cb6pqos_ec8jVxRliy1v_WtSD99rzG--AYnEgCGcF2GtYBF3lqWKekUQZtLzko1rY4HE6A5_tbs72kpOz2khkiH5AHazW9TeAAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 12 29.png" alt=""><figcaption><p>ListBox displaying integers</p></figcaption></figure>

{% tabs %}
{% tab title="Full Code" %}
{% hint style="info" %}
The following code produces similar results, although explicitly creating ListBoxItems is less common:

```csharp
var listBox = new ListBox();
for (int i = 0; i < 10; i++)
{
    var listBoxItem = new ListBoxItem();
    listBoxItem.UpdateToObject(i);
    listBox.Items.Add(listBoxItem);
}
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2PQQuCQBCF7_6KwdOKIXXNOlSHEIKi7OZl042mdDZ0rEj877kWYs5lmPe-99itLAA7KNZlZk-B81KNjICEjDLFt2pU-yFzyCTSTpJKYQ6knnBgGd9aQTh-RJ3tLZIk1HutudUjMuEUC17q1y-6-V6tf9Y5CCQGbMyx36wZTMx2XSeiKiJoplcRsMr-a4zSVhmyR3nHeyJZhXp7uqqYBQ4Yz0CFea7opQxUD76zumDaQY5vW7X1AZ5S-1A2AQAA" target="_blank">Try on XnaFiddle.NET</a>
{% endhint %}
{% endtab %}

{% tab title="Diff" %}
{% hint style="info" %}
The following code produces similar results, although explicitly creating ListBoxItems is less common:

```diff
var listBox = new ListBox();
for (int i = 0; i < 10; i++)
{
-   listBox.Items.Add(i);
+   var listBoxItem = new ListBoxItem();
+   listBoxItem.UpdateToObject(i);
+   listBox.Items.Add(listBoxItem);
}
mainPanel.AddChild(listBox);
```
{% endhint %}
{% endtab %}
{% endtabs %}

By default the ListBoxItem displays the `ToString()` of whatever is added to the `Items` property. In the case of integers, the string representation of the integer is displayed. However, if we display an object, such as information about a weapon, the default display is not very useful.

For example, consider the following code:

```csharp
//Define WeaponDefinition
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

// Initialize
// Later, create the ListBox:
var listBox = new ListBox();
listBox.Width = 400;
listBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrVVwW7iMBC95yssTkGqLCjdyyJWaomKkMqqKi3d28okQ7BqbNZ2Qncr_r3jJIQEorSHrQ-JM-955s147CSGy5jMeKiVUStLf0lGbzXbwE7pl6GXtMF0otl2zUNT8pRUEwQnyeZgwim9VXpTckoDHStptRKIeKFgxpBnYFslA1hxyS1X0nvzCI5tshQ8JMZqt_4n-idvJAY7JMY99lUSl5YEbMNiCIAJ2058gD8J1xDdQQqinXqveXgadu95BSOX7zLvk-_Z-yBd85RZIIdCBZCinxmTKFCT33FZv4yNpZmDdgw3fZqS0Y-KjWJhWCJsQS5CZ0H9bmbKY7pReiYjImHXHN-3a266w3KR2w-Qlj4oZQOsS2iV_osOOgXQOVKnZqYSAwtu-FIAcqxOIIexKnnmyqIHiIhKQWseAUkVj8jUbS0T_B-cic5yphWC03dBirQxVsKEWYA22Bp0MUDl5dKUabJhXN4ziTuZ5zy3LHzJDH4lyZJFr6PoUblc_VNPght7o14LP3f5V9VJQaDPPLJrpF31eufg1AI2OUbxnZez3j7Qj_m7kXU3ljxgMe5Q56IGVht7RL7VwXozj0i_DucNjOZead53_7PoOV4KUavmfq9d9GWz6MsvFH39Cu2SP6jzoFnyoFlyrf3Gay4iv8ii2oRLZqB6ELqfOlpP2wjvGt_dCI8ck4uLSfM5K9gl6agxi94Mf6Ag0Gz3ifi1y4iOBTDtj5VQGn8JWq6E2oG-EUlVU64583-qNDOe6dx77_lOCMXaBgAA" target="_blank">Try on XnaFiddle.NET</a>

In this case `ToString` is called on `WeaponDefinition`, but this doesn't provide useful information to the user:

<figure><img src="../../../../.gitbook/assets/13_08 14 47.png" alt=""><figcaption><p>ToString displays the class name</p></figcaption></figure>

We can change the displayed string by modifying the `ToString` method in `WeaponDefinition`.

{% tabs %}
{% tab title="Full Code" %}
```csharp
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
    public override string ToString() =>
        $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrVVUW_aMBB-z6-w0B6CVEVQtpchJrVERUhlqgqle5tMcgSrjs1sJ3RD-e87JyEkJUr7sPkBzN3n-76785lEMxGRBQuU1HJrvB-CeneKxnCQ6mXsJF1ub6bofscCXeGkkDN0zpL4ZMKtdydVXGEqgzeVwijJ0eMEnGpNnoHupfBhywQzTArn6BBc-2TDWUC0Ufb8d4xPjiQCMybafmR1EBOG-DSmEfhAuekGPsKvhCkI7yEF3g19UCzooJUpKMVCOIlcyWW-cftk8i0H2vWpd7TqM3KsSczCOCKqUKIJT_mxISvrjZ3McUqeok62xEPyNf8-1UixlBogp474kKLgBRVIo8jPqGpUjsYeLEFZhN0-zVFkzeZhB2jCTQkuqXNSt5-bCk67qshkQgQc2vlds2O6P64O2caDMN6jlMbHTAMj1W8M0CsdvTN0rhcy0bBmmm04IMaoBAo3VqXIXBqMAOG5CalkIZnbO0Q5-wMXovOcvRrA6rsiZdrIlVCu16A03kFvPULl1dGUKhJTJh6owCtT5Lw0NHjJDW4tyQrl3YThStpc3beRONPmVr6Wce6LX_UgJcB7ZqHZIezzYHDpnBvAaUIW10a5GKIT_Jy_XfkYYcl9GmGHelcNZ32CJuRL09mcmgkZNt3FpKB5UJmz_j8WvcTXJ-zUPBx0i75uF339H0XfvEK35HfqPGqXPGqX3Lh-0x3joVtmUb-EG6qhPgj9D43W0z7Et8a1L8KKYXJRuWmfsxJdgc4ac_Z29zsKfEUPH-BvPEbelANV7lRyqfC_R4ktlwdQtzypayo05_HfKs2NFzoz5y9xuupLQwcAAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}

{% tab title="Diff" %}
```diff
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
+   public override string ToString() => 
+       $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/13_08 15 43.png" alt=""><figcaption></figcaption></figure>

By implementing a custom `ToString` method on the `WeaponDefinition` class we can customize how it is displayed in the `ListBox`. While this is handy, it does limit us because we may want to modify `ToString` for other purposes such as customizing information in the debugger.&#x20;

## DisplayMemberPath

`DisplayMemberPath` can be used to change which property is used to display each item. If `DisplayMemberPath` is not assigned (or changed to an empty string), then the `ToString` method is used. If this value is assigned, then it must be the name of a property on the source item. This property has the benefit of not relying on `ToString` which might be used for debugging or might change for other reasons in the future.

For example, the following code shows how to display the weapon's name:

{% tabs %}
{% tab title="Full Code" %}
```csharp
//Define WeaponDefinition
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

// Later, create the ListBox:
var listBox = new ListBox();
listBox.Width = 400;
listBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
listBox.DisplayMemberPath = nameof(WeaponDefinition.Name);
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrWVTW_iMBCG7_kVFqcgVRaU3csiVmqJipDKCvWD7m1lkgGsGpsdO9BuxX_vOAkhfGzaw64PEGZezzwznpjUSj1nIxmjsWbm-E8t-A2KJWwMPneDtM7NByhWCxnbUme0GZBzkC53JnrkNwaXpaY08L7RDo0iTxArYS17ArEyOoKZ1NJJo4O3gNFapVMlY2Yd-v0_KD57Y3NwXWb9x7YqktqxSCzFHCIQytUL7-B3KhGSW1iDqpeOUcbHabdBUChyfF95m33LvnfoKNfCAds1KoI1xRkJTYDIfs3L_mVqas09oFf4x8ch632v2Dg1RqTKFeIidZY0bGamPKdfZWTWYxo25_OHbiFts1tu8ucB2vE7Y1xEfYmdwVcK0Cgcjb10aEcmtTCRVk4VkMZhCrmbupJXbhxFgISZNSDKBNjayIQN_dEKJf_ACXRWM68IPN8FK8qmXKlQdgJoaTT4pEPk5da1QLYUUo-FppPMa753In7ODGGlyFLFr5Lkwfhaw-NISlp3bV6KOLf5r2qQQsCfZOIWJPvSap06hw5oyClL6KOczPZOvq_fr2y6qeWRmNMJNS4OnNXB7rGvh87DYe6x9qE7H2Ayt0rztvmPoe_pUkhqmduteujL89CX_xH66gXqkT_oc-c8cqceOZJ2pcTrCJZTwLHIpkgTkZmFx_zck_5tgvsLqZKwiFqd46mwUH2Xmp96Ox9XCV1Xob9UHiT1Z148nH9VC3Up2jNm2c-7PyCIUGw-kf_gPuN9BQLDvlEG6V8F9UyZDeC1SqtMOXMW_5g0M55wboN3KzZhVB0HAAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}

{% tab title="Diff" %}
```diff
//Define WeaponDefinition
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

// Later, create the ListBox:
var listBox = new ListBox();
listBox.Width = 400;
listBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
+listBox.DisplayMemberPath = nameof(WeaponDefinition.Name);
mainPanel.AddChild(listBox);
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/13_08 18 32.png" alt=""><figcaption></figcaption></figure>

Although this approach is more flexible than using `ToString`, it does not allow us to customize how each item is displayed. The next section shows how to create derived forms classes to customize their `UpdateToObject` method.

## FrameworkElementTemplate

We can customize the way WeaponDefinition is displayed without making any changes to WeaponDefinition itself. Instead we can create a new class that is responsible for converting a WeaponDefinition instance into a string to be displayed by each ListBoxItem.

To do this, we need to perform the following steps:

1. Create a new class that inherits from ListBoxItem. The purpose of this class is to convert our WeaponDefinition into a string.
2. Associate this class with our ListBox by assigning the ListBox's FrameworkElementTemplate property

The following code has been modified to create and use a WeaponDefinitionListBoxItem:

{% tabs %}
{% tab title="First Tab" %}
```csharp
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

public class WeaponDefinitionListBoxItem : ListBoxItem
{
    public override void UpdateToObject(object objectInstance)
    {
        if(objectInstance is WeaponDefinition definition)
        {
            var textToDisplay =
                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
            base.UpdateToObject(textToDisplay);
        }
        else
        {
            base.UpdateToObject($"Error updating to {objectInstance}");
        }
    }
}

// Later, create the ListBox
var listBox = new ListBox();
listBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
//...
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrVW30_bMBB-z19xqnhIJRQV2F5WdRI0gCrBhqDA3iY3ubYejt3ZTgur8r_PTkJqt6HwsPmhce--3H33U8kV5TO4pokUSkx19IOT6EKSDFdCPvWDfJ86upRkMaeJanCCi0ujvMyzV5G5RhdCZg2mEURDwbUUzGiChBGl4BHJQvAYp5RTTQUP1gGYs8gnjCagtLTvfzP2YQ0z1H1Q9qdwQZRriElGZhgjYXo_8BZ_51RieoVLZPuhN5Im226LIKgR7fSvqNJn4nmkMYMv4Pzz4xJLlJKmCEtBU7hfpETjWHyf_MJEh6J8QPUYcaUJT7Bbvl4ZsYdOIfQRQHfpQNpcKwO-EXuWRILGZz0WMVULRl5g4OntOeisN4YiW40CXImT_SLNZiCrJCtgS-bivOQXnb7naEIURlup8Ih1N_iqVvYgU_hGZG0GDzrnUgoJuRXb1tIC1n4ei86Oo926244_MhW2z9fSSro0ruB1QGJcmv65JtykRsLPWTM3JdqMxB1Ki7DX-xEMvjqyyJSQ5EzX4Np16TTc7oTGMgyA46rdf6jnVDlx2TlErqNbIXRsSpJoIU3loVMrnNqM1LXIFT5QRScMDUbLHCu1yUoVudDGAqZbfT2yZSeM_sEd0mXMkQOw_A6hDtv4yglTDyiVbZuHE8M8cBs2I5TfEG4muIr5TpPkqRSETpANKjpN07GwsYbbllg1orWdemBdIzVgswHPGWYmQ2PMTFOaevvjYq28BQ31ywLFNNyzNLotnh9pqueG4Kdeb1dpX1I2vtB63tmm7ZNR7lNT7JjMTG90Dj2lu0oH8NlX-utzAEe-ulqZRtzbTFD3H5O-M5lN93I-6u0nfdxO-vg_kj59xv2U38nzSTvlk3bKXuMP55SlYR2F2_7lfnRn9ENDXS3U0O6iMTXBzepL-4TX6Aa04ehs5231OwxiSVYf8O-twWjIkMhwKJiQ5iNE8ikTK5RnLHc5VZxL-9tMS-EOzyL4C7koU59MCQAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}

{% tab title="Diff" %}
```diff
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
-   public override string ToString() => 
-       $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}

+public class WeaponDefinitionListBoxItem : ListBoxItem
+{
+    public override void UpdateToObject(object objectInstance)
+    {
+        if(objectInstance is WeaponDefinition definition)
+        {
+            var textToDisplay =
+                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
+            base.UpdateToObject(textToDisplay);
+        }
+        else
+        {
+            base.UpdateToObject($"Error updating to {objectInstance}");
+        }
+    }
+}

// Later, create the ListBox
var listBox = new ListBox();
-listBox.DisplayMemberPath = nameof(WeaponDefinition.Name);
+listBox.FrameworkElementTemplate =
+    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
//...
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/13_08 22 27.png" alt=""><figcaption><p>WeaponDefinitionListBoxItem displaying custom text</p></figcaption></figure>

This code example creates a ListBoxItem named WeaponDefinitionListBoxItem. As the name suggests, it is specifically created to display WeaponDefinition instances. Of course, you could create a more generalized version of this class which might handle a variety of different types of items.

## Customizing ComboBox Text

As mentioned above, the ComboBox control is similar to ListBox. For example, we can change the type from ListBox to ComboBox to get nearly identical behavior as shown in the following code block:

```csharp
var comboBox = new ComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
comboBox.Width = 400;
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
mainPanel.AddChild(comboBox);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrVW30_bMBB-z19xqnhIJRQV2F6GOgkaQJVgQ1Bgb5ObXFsPx-5sp4VV_d9nJ2lqtyHwsPmhce6-3H33U80V5VO4oYkUSkx09IOT6FKSDJdCPp8GeZs6upJkPqOJqnGCiyujvMqzjchco0shsxpTC6KB4FoKZjRBwohS8IRkLniME8qppoIHqwDMmedjRhNQWtrvvxn7sIIp6lNQ9mftgijXEJOMTDFGwnQ78A5_51Rieo0LZO3QW0mTXbfrIKgQzfSvqdLn4mWoMYMv4Lz5cYkFSklThIWgKTzMU6JxJL6Pf2GiQ1E8oHwMudKEJ9gtPi-N2EMnEPoIoPt0IK2vpQHfiD0LIkHjix6JmKo5I6_Q9_T2HHRWW0ORrcYaXImT_XWaTUGWSVbAFszFeclfd049R2OiMNpJhUesu8WXtbIHmcI3ImsyeNC5kFJIyK3YtpYWsPLzuO7sOdqvu-34I1Nh-9yUVtKFcQWbAYlxYfrnhnCTGgk_p_XcFGgzEvcoLcJeH4bQ_-rIIlNCkjNdgSvXhdNwtxNqy9AHjstm_6GeUeXEZecQuY7uhNCxKUmihTSVh06lcGozVDciV_hIFR0zNBgtcyzVJitl5EIbC5ju9PXQlp0w-gf3SBcxRw7A8juEKmzjKydMPaJUtm0eTwzzwG3YjFB-S7iZ4DLme02S50IQOkHWqOgsTUfCxhruWkpENhZmRitDg-rVNbOBbJfgBcPMJGmEmelLU3J_Yqydt6Chfp2jmIQte6Pb5PqJpnpmOH7q9Rq09jNlgwyt772V2jwexVI1FY_J1DRI59BTuvu0D599pb9D-3Dkq8u9acS97Rh1_zXre5PctJX0Ua-d9XEz6-P_yfrsBds5v5Ppk2bOJ82cvf4fzChLw00Y7hgUe9Kd1Q8Nd7lYQ7uTRtREN60uzZNeoWvQlqSzpXfV7zCIJVl-wL-3DqMBQyLDgWBCmj8jkk-YWKI8Z7nLqeRc2N9lWgj3eK6Dv0CJZIBUCQAA" target="_blank">Try on XnaFiddle.NET</a>

This code compiles and works mostly the way we want it, but not perfectly. Notice that WeaponDefinition's ToString is still called to display the main text on the combo box.

<figure><img src="../../../../.gitbook/assets/13_08 25 10.gif" alt=""><figcaption><p>ComboBox displaying WeaponDefinition ToString</p></figcaption></figure>

Fortunately we can create a class that is derived from ComboBox which overrides the UpdateToObject method just like we did earlier for ListBoxItem.

```csharp
public class WeaponDefinitionComboBox : ComboBox
{
    public override void UpdateToObject(object objectInstance)
    {
        if (objectInstance is WeaponDefinition definition)
        {
            var textToDisplay =
                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
            base.UpdateToObject(textToDisplay);
        }
        else
        {
            base.UpdateToObject($"Error updating to {objectInstance}");
        }
    }
}
```

Now we can use this new ComboBox-deriving class:

{% tabs %}
{% tab title="Full Code" %}
```csharp
var comboBox = new WeaponDefinitionComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
// ...
```
{% endtab %}

{% tab title="Diff" %}
```diff
-var comboBox = new ComboBox();
+var comboBox = new WeaponDefinitionComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
// ...
```
{% endtab %}
{% endtabs %}
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACu1WUU_bMBB-7684VTykEooKbC9DnQQNoEqwISiwt8lNrq2HY3e208Kq_vedkzZ12rTwsL1M8wMxd5_vvrvznZsZLkdww2OtjBra8Jtk4aVmKc6Ufj5tZPvU4ZVmkzGPTYlTUl2R8ipLVyLahpdKpyWmFIRdJa1WgjSNWDBj4AnZRMkIh1xyy5VszBtAa5INBI_BWO3OfyH7MIcR2lMw7s_CB3FpIWIpG2GETNj9wDv8mXGNyTVOUeyH3moeb7pdNBpLRD39a27suXrpWUzhE3j_VeNSU9SaJwhTxRN4mCTMYl99HfzA2AYq_0Dx6UljmYyxlR8vjLjFhxBUEcC36UBSbgsDVSNuTZkGiy-2ryJuJoK9Qqeid-ugOV8bCl01FuBLvOwvknQEukiyATEVPq6S_EXztOJowAyGG6moEGut8UWt3EJhcEdkdQYPmhdaKw2ZE7urZRXMq3lcNLccvV33rkoHikpNRV9t_1f8n6q4m3FHVF73XZVW8ym5gtVIjHBKE-OGSWoGDd9H5aTM0TQE71E7hNs-9KDz2ZOFdJdYJuwSvHSdOw02e7-0DB2QOKv3H9gxN15cbvKitOGdUjaiksRWaep1aC4VXm165kZlBh-54QOBhLE6w0JNWSkiV5YsYLIxyXquG5jgv3CLdB5z6AEcv0NYhk2-MibMI2rjrs3jCTFv-CMqZVzeMkkzu4j53rL4ORcEXpAlKjxLkr5ysQabluJVqxaGdnWyb3Z1ZP0MXghMKWl9TOme0hWozkxndxc0sK8TVMNgz8vRqnP9xBM7Js4f2u0arTtmXNBBXUw72iV_VukGRGxEF6Z5WFH6L2oHPlaV1Ve0A0dVdfFykri9bqvWn2Z9T8lN9pI-au9nfVzP-vhvsj57wf2c38j0ST3nk3rOlX7ojrlIglUYflvkc9Pv3Xc1ezFoAzej-pyiGy039Z2_RJegNUlvam-q32AQaTZ7h__KeAy7ApkOukooTT9HtRwKNUN9LjKfU8E5t7_JNBdu8Vw0fgNgcvkcVgsAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 26 04.gif" alt=""><figcaption></figcaption></figure>

## SelectedObject

Once items are added to a ListBox, the selection can be accessed through the SelectedObject property. For example, we could handle equipping the selected weapon using code similar to the following block:

```csharp
var equipButton = new Button();
equipButton.Click += (_, _) =>
{
    var weapon = listBox.SelectedObject as WeaponDefinition;
    if(weapon != null)
    {
        EquipWeapon(weapon);
    }
};
mainPanel.AddChild(equipButton);
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACrVWUW_TMBB-z684Kh5SMUUbgxemIrF1TJU2QLQM3iY3uXZmTlxspx1U-e-c4zR1mizwAH5oHN_l7rvz3X3NNc-WcMNjJbVcmOhbxqL3iqW4kerhLMj7xNGVYqt7HutaT2byioRXebo7om30Xqq01qkPoguZGSUFSYJYMK3hK7KVzMa44Bk3XGbBNgBaq3wueAzaKPv9B7IPW1iiOQNtfwpfiWcGxixlSxwjE6Zf8TP-yLnC5BrXKPpVPyke97iVa1SKJ7gDOZPTchMOYfS2VLTr-WBr0Rew9SAWSboE5ZBoEGuxbcAqBmdBEQSVn-48XXNtzuXjxGAKb8B7ayawBrmWPIEvq4QZnMmP8-8Ym1CWD3CPSaYNy2Iclp87I3bxBYRNDeBtOJDUW2egacSuNVNg8NHM5JjrlWA_YdSQV9naG4qqxHknvTn09Frp9J3MmcboIBUNYMO9vrtxu1BofCKyLoPPB5dKSQW5PbblYSRsm3ksBi1HRevebWud0A3b5-5qFV-TK9h14hjXVKg3LKPUKLhb1g1aalPvTVFZDbv9MqHi9M4iukKWC1MpV65Lp-FhJdSWYQQZbrr9h-aeay8u2_CYmeizlGZMVxIbqejmYVAJvLuZ6BuZa7zlms8Fko5ROToxZcVFLg1ZwOSgrif22pngv7AFuow58hQsviOowiZfORP6FpW2ZXN7SsgDv2BTxrNPLKNR4WKeGhY_lAehF2StFb1Lkpm0sYaHlgSb11au7d43UAqjGRWhTc0HCZuyv8CW8WqFyeAJXxf3XCRh-XXLnxsJO4_ureHTHe1H-6XAlG5khik1AdVXsz2tladUQ_NzhXIR9gypYYfnrzwx9wTw1fFxW2g_0jbG0Hpu0UR3J5ZEQRkcsyXV4uCoIfQ5YgSvm8ImL4zgpCl2XEDHx_uOHf5j0FPKbNKL-eS4H_TLbtAv_yPod4_YD_kPeT7thnzaDbmr-F0Uh-Vfds55bgx1kWsB9-J3gKdT996lPRt061zQbHyAFyMI747grkHzbaqrOnhUp3mKopxdjiCAtUm0SVOWeCsjzyiCXIg9s7ZdtqYIEVA1PN7A1tlxhHrAhnuGK3rz7CXCz3XJff78_auB7cgytDwz41RIy2rTPb0r7Vppj9Nj3kPxHxCMFdv8hf8GxVEBIFPhhRRS0T9ZlS2E3KA6F7mPyWEu7R8iLQ9bOIvgN_hcpVGRCwAA" target="_blank">Try on XnaFiddle.NET</a>

Keep in mind that the ListBoxItem is responsible for converting what was added to the ListBox Items property into a string, but the Items and SelectedObject property are still referencing the WeaponDefinition instances.

## Conclusion

This document shows how to work with ListBox and ComboBox Items. A future tutorial discusses how to further customize the appearance of ListBoxItems.

The next tutorial covers how to use various input hardware including the mouse, keyboard, and gamepad with Gum UI.
