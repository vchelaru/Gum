# 6 - Parent

Introduction

The Components tutorial shows how Text and ColoredRectangle instances can be sized and positioned according to the Button that they are a part of. Instances can use other instances as their parents. Gum does not place a limit to the depth of the parent/child hierarchy, enabling flexible and responsive layouts through.

Parent/child relationships are useful for

* Automatically adjusting margins and backgrounds
* Word-wrapping text relative to a parent
* Stacking objects on top of each other or side by side
* Placing objects next to other objects which are dynamically sized
* Creating tables and other complicated layout objects

## Creating a Component

For this example we'll create a Component for displaying distance. This component has two Text instances:

* ValueText - the Text instance responsible for displaying the value for the distance. For example "100".
* UnitsDisplay - the Text instance responsible for displaying the units for the distance. For example "km" for kilometers.

The two Text instances will have different colors so that ValueText stands out.

{% hint style="info" %}
A single Text instance can support inline styling. We use two text instances here to show how to work with the Parent variable, but for more info see the [Text property](../../gum-elements/text/text.md#using-bbcode-for-inline-styling) page.
{% endhint %}

To create our component:

1. Create a new component named **MeasurementDisplay**
2. Drop two Text objects in the newly-created component
3. Name the first **ValueText**
4. Name the second **UnitsDisplay**

<figure><img src="../../../.gitbook/assets/15_05 38 09.png" alt=""><figcaption><p>MeasurementDisplay component</p></figcaption></figure>

## Positioning according to a parent

Next we'll make the **UnitsDisplay** use the **ValueText** as its parent:

1. Select the **UnitsDisplay** Text instance
2. Change its `Parent` to `ValueText`
3. Change its `X Units` to `Pixels From Right`. This makes the text object positioned according to its parent's right edge
4. Change its `X` to `10` . This means the **UnitsDisplay** text is 10 units offset from the right edge of its parent **ValueText**
5. Verify its `Y` is `0`

![UnitsDisplay positioned relative to the right-side of ValueText](<../../../.gitbook/assets/15_05 42 39.png>)

The ValueText actual width should be based on its contents, so we'll do the following:

1. Select `ValueText`
2. Change `Width Units` to `Relative to Children`
3. Change `Width` to `0`

By setting these values, ValueText is now sized according to its children, which in this case are its letters.&#x20;

![Width and Width Units set to size ValueText according to its contained Text variable](<../../../.gitbook/assets/15_05 48 40.png>)

You may have noticed that UnitsDisplay is also a child of the ValueText. However, since UnitsDisplay is explicitly positioned outside of the bounds of its parent ValueText, then ValueText ignores this child when calculating its own Width. For a detailed discussion of Width Units and whether children are ignored, see the [Width Units](../../gum-elements/general-properties/width-units.md#ignored-width-values) page.

## Adjusting Colors

Now that we have adjusted the position, size, and parent values on our Text instances, let's modify the color of the UnitsDisplay:

1. Select the **UnitsDisplay**
2. Change `Red` to `200`
3. Change `Green` to `150`
4. Change `Blue` to `0`

<figure><img src="../../../.gitbook/assets/15_05 51 36.png" alt=""><figcaption><p>Text with changed color values</p></figcaption></figure>

## Changing ValueText

Now that we have set up a parent/child relationship between ValueText and UnitsDisplay, UnitsDisplay automatically adjusts its position in response to changes in ValueText. Any change on ValueText resulting in the right-side of the parent changing automatically adjusts the position of UnitsDisplay.

For example, if we change the Text property on ValueText, it grows or shrinks in response.

![Changing the Text property results in ValueText changing its effective width](<../../../.gitbook/assets/15_05 55 04.gif>)

## Width and Effective Width

As mentioned above, changing the Text property causes ValueText to grow or shrink. However, regardless of its size, the `Width` property is still set to 0.

<figure><img src="../../../.gitbook/assets/image (153).png" alt=""><figcaption><p>Width is 0 despite the size being larger than 0</p></figcaption></figure>

The `Width` variable is used in combination with its `Width Units` to calculate an _effective width._ In this case, the effective width is determined by the Text property on ValueText. It's important to note that all Gum objects have effective values for x, y, width, and height, all of which are determined by their respective _units_ values.

**Children always depend on their parents' effective values rather than their explicitly set values**. Gum helps us visualize the effective values when we mouse over one of the _resize handles_ on the selected object. For example, the following image shows the `Width`, `Width Units`, and effective width values of our **ValueText**.

<figure><img src="../../../.gitbook/assets/15_06 02 39.png" alt=""><figcaption><p><code>Width</code> is 0, <code>Width Units</code> is <code>Relative To Children</code>, effective width is 65</p></figcaption></figure>
