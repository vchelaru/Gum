# 5 - Exposing Variables

## Introduction

The ability to expose variables in Gum makes components flexible. For this example we will continue using the HealthBar component from the previous tutorial.

## Recap

The last tutorial created a HealthBar component with two rectangles - Background and Fill. The instances were set up to be positioned correctly according to the size of the HealthBar.

We also created a GameScreen and added a few HealthBar instances. Although we can position and size each HealthBar instance, they all display the same health percent. This tutorial discusses how to use exposed variables to allow each instance to display a different value.

## Exposing the Text variable

By default components only exposes _top level_ variables. Variables on instances inside the component are not available when editing an instance. In programming terms these variables are considered _protected_.

However, we can _expose_ variables inside of our component so that they can be modified in our screen.

To do this:

1. Select Fill inside the HealthBar component
2. Find the `Width` variable in the Variables tab
3. Right-click on the `Width` variable **Expose Variable**
4. Enter the name HealthPercent for the variable name - note that typically variables are exposed without spaces in them, but Gum will display them with spaces in the variables tab

<figure><img src="../../../.gitbook/assets/30_06 19 05.png" alt=""><figcaption><p>Right-click Expose Variable</p></figcaption></figure>

Notice the variable now displays its exposed variable name.

<figure><img src="../../../.gitbook/assets/30_06 20 21.png" alt=""><figcaption><p>Width variable exposed as HealthPercent</p></figcaption></figure>

Although this variable belongs to the Fill instance, it is now exposed as a variable on HealthBar. You can now see this variable by selecting the HealthBar component.

<figure><img src="../../../.gitbook/assets/30_06 22 07.png" alt=""><figcaption><p>Health Percent under HealthBar</p></figcaption></figure>

Similarly, now each instance can be modified since each now exposes a Health Percent variable.

<figure><img src="../../../.gitbook/assets/30_06 23 52.png" alt=""><figcaption><p>Health Percent adjusted per-instance</p></figcaption></figure>

## Conclusion

This tutorial shows how to use exposed variables to customize component instances. You can expose other instance variables in your components to customize instances. Other examples of variables which may be exposed include:

* Visibility of icons on a Button component
* Font sizes on a Label component
* Sprite visibility showing the number of connected gamepads on a JoinGame component

It's best to experiment with exposed variables to get a feel for how you can use them in your own components.
