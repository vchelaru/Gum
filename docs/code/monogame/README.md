# MonoGame

MonoGame projects can take full advantage of the Gum tool and runtimes to create flexible layouts which are useful for HUDs, Menus, and UI of any complexity.

The Gum UI Tool is a WYSIWYG editor for creating layouts. Projects that are created in the Gum UI tool can be loaded into your MonoGame project with just a few lines of code.

The Gum runtime (called MonoGameGum) is a NuGet package which adds the classes necessary to load and interact with Gum projects.

### Do I Need to Use the Gum Tool?

Absolutely not! The Gum tool can be useful for doing complex layouts, previewing how things may be positioned, and helping you learn how to code with Gum - but it is not required! You can use Gum purely in code to do your own layouts or use only parts of Gum (such as font rendering).

This section uses screenshots of the Gum tool so you can see what is possible with Gum, but everything you see here can also be done purely in code.

### Sample Projects

If you prefer to dive in and see things working, the Gum repository includes sample projects that show how to work with Gum purely in code, and also how to load a Gum project into a MonoGame project.

You can clone the repository and open the projects in your favorite IDE (like Visual Studio) and try them out.

The direct link to the samples is here: [https://github.com/vchelaru/Gum/tree/master/Samples](https://github.com/vchelaru/Gum/tree/master/Samples)

For more information about the Sample projects, see the [Samples page](samples/).

### What is the Gum UI Tool?

The Gum UI Tool (usually called "Gum") is a visual editor for creating layouts using the Gum layout engine. Gum has been used on many commercial games to create all types of UI ranging from standard game HUDs to complex forms-based UIs. Gum can be used for any type of game.

<figure><img src="../../.gitbook/assets/image (33).png" alt=""><figcaption><p>A sample StyleDemo component in the Gum UI Tool</p></figcaption></figure>

The Gum tool is simple to use - you can create your screens by drag+dropping, moving, and resizing objects with the mouse.

<figure><img src="../../.gitbook/assets/25_05 53 08.gif" alt=""><figcaption><p>Buttons added in a stacking Container instance</p></figcaption></figure>

Gum is an _object oriented_ design tool, so projects can contain reusable components which contain other components and _standard elements_ (text, sprite, container, etc). Gum also provides inheritance, and behaviors (a concept similar to interfaces in C#). For example, the following PauseMenu component inherits from UserControl - a base type defining the standard visuals for a framed UI element.

<figure><img src="../../.gitbook/assets/image (34).png" alt=""><figcaption><p>PauseMenu in Gum</p></figcaption></figure>

Gum produces a set of XML files (and PNG/FNT files for fonts) which can be added to any MonoGame project and loaded with a few lines of code. For information on loading projects, see the [Loading .gumx (Gum Project)](broken-reference) page.

<figure><img src="../../.gitbook/assets/image (35).png" alt=""><figcaption><p>Example Gum project in Windows Explorer</p></figcaption></figure>

### What is the Gum Layout Engine

The Gum Layout Engine is the core technology behind the Gum UI Tool and the Gum MonoGame runtimes. Gum layouts use a few rules to determine the position and size relationships between parents and children. These rules allow for the creation of virtually any type of layout.

Projects which perform their layouts purely in code can still take advantage of the Gum UI Tool to preview their layouts and to learn about the capiblities of the GraphicalUiElement (the C# object type providing access to all Gum properties).

A full list of Gum properties can be found in the [Gum Element General Properties](../../gum-tool/gum-elements/general-properties/) page, but we will cover a few concepts here to give you an idea of how Gum works.

By default, Gum elements are positioned relative to the top-left of their parent. Similarly, the origin of a Gum element is also its own top-left corner. Therefore, a rectangle that has an X of 100 and a Y of 50 appears as shown in the following image:

<figure><img src="../../.gitbook/assets/image (36).png" alt=""><figcaption><p>Rectangle with X = 100 and Y = 50</p></figcaption></figure>

The Gum tool helps you visualize the relationship between a child and its parent when the child is selected. Notice that the child draws a line from its origin (its top-left corner) to the top left of its parent. In this case the parent is the entire screen, which has a dotted outline.

<figure><img src="../../.gitbook/assets/image (37).png" alt=""><figcaption><p>Selected rectangle showing its origin and its "units" (relative point on the parent)</p></figcaption></figure>

A child's position is relative to its parent's position, so changing the position of the parent ultimately changes the absolute position of its child as well. The following animation shows a parent container which has a white rectangle as its child. Notice the child can be moved relative to the parent. If the parent is moved, then the child's absolute position changes as well.

<figure><img src="../../.gitbook/assets/25_06 09 20.gif" alt=""><figcaption><p>A child rectangle and a parent container</p></figcaption></figure>

This basic type of layout is similar to layouts provided by most visual APIs including SpriteBatch in MonoGame.

The Gum layout engine is built around the concept of "units". As mentioned above, by default the X and Y values of an element is measured in units from the top-left corner. In other words, the default X Units is absolute pixels from left for X and absolute pixels from top for Y. The Gum tool exposes this as a set of buttons.

<figure><img src="../../.gitbook/assets/image (38).png" alt=""><figcaption><p>X Units and Y Units in the Gum UI Tool</p></figcaption></figure>

At runtime, these properties exist as enums which can be changed. For example, to set an element's XUnits to be relative to the horizontal center of its parent, the following code can be used:

```csharp
uiElement.XUnits = GeneralUnitType.PixelsFromMiddle;
```

As mentioned above, the Gum tool is very useful for testing out how different properties behave quickly. For example, the following animation shows how X and X Units can be used to change the position of an object relative to its parent:

<figure><img src="../../.gitbook/assets/25_06 16 54.gif" alt=""><figcaption><p>Changing X and X Units immediately updates a child's position</p></figcaption></figure>

The position, units, and origin values can be used to create common layouts. The following code could be used to center a child in its parent:

```csharp
//assuming child is a valid GraphicalUiElement:
child.X = 0;
child.XUnits = GeneralUnitType.PixelsFromMiddle;
child.XOrigin = HorizontalAlignment.Center;
```

This same layout could be achieved in the Gum UI tool by setting the following values:

* X = 0
* X Units = Pixels from Center
* X Origin = Center

<figure><img src="../../.gitbook/assets/image (39).png" alt=""><figcaption><p>Child centered horizontally by changing X, X Units, and X Origin</p></figcaption></figure>

An element's size can also be controlled through units. For example, a child rectangle could set to provide an 8 pixel border inside of its parent container using the following code.

```csharp
child.X = 0;
child.XUnits = GeneralUnitType.PixelsFromMiddle;
child.XOrigin = HorizontalAlignment.Center;
child.Y = 0;
child.YUnits = GeneralUnitType.PixelsFromMiddle;
child.YOrigin = VerticalAlignment.Center;
child.Width = -16;
child.WidthUnits = DimensionUnitType.RelativeToContainer;
child.Height = -16;
child.HeightUnits = DimensionUnitType.RelativeToContainer;
```

Similarly, the following could be done in the Gum UI tool::

* X = 0
* X Units = Pixels from Center
* X Origin = Center
* Y = 0
* Y Units = Pixels from Center
* Y Origin = Center
* Width = -16
* Width Units = Relative to Container
* Height = -16
* Height Units = Relative to Container

<figure><img src="../../.gitbook/assets/image (40).png" alt=""><figcaption><p>Child positioned in the center of its parent with 8 pixel border</p></figcaption></figure>

Note that the Width and Height are set to -16, which is twice the desired border. This is because the border of 8 pixels must appear on both the left and right sides for Width and both the top and bottom sides for Height.

Notice that each of these properties combines to produce a center layout. While this may seem verbose, this type of fine control provides ultimate flexibility. You aren't limited to simple alignments such as left, right, and center. Rather, you can create layouts where any part of the object is positioned relative to any other part of its parent.

This type of center layout is responsive to changes in the parent, so if the parent changes position or size, the child moves along with it as expected.

<figure><img src="../../.gitbook/assets/25_06 28 26.gif" alt=""><figcaption><p>Changing a parent also changes its childs width if the units are relative to the parent</p></figcaption></figure>

Hierarchies can go many levels deep, and each parent child relationship follows the same rules. For example, the following animation shows a relationship where the blue and red children occupy the left and right half of the white rectangle, including a border of 8 pixels around and inbetween the rectangles. Notice that changing the parent size automatically cascades size and position changes to the children.

<figure><img src="../../.gitbook/assets/25_06 34 26.gif" alt=""><figcaption><p>A parent container with a children hierarchy of multiple levels responding to layouts</p></figcaption></figure>

Children are in control of their own layouts using "unit" values, but parent elements can also apply rules to control layout by setting the Children Layout property. For example, a parent can stack its children from top-to-bottom by setting Children Layout to Top to Bottom Stack. Notice that changing the stacking from top-to-bottom changes the Y positioning of the children. The X position is intentionally staggered to be able to see the position of each rectangle when stacking is turned off.

<figure><img src="../../.gitbook/assets/25_06 43 35.gif" alt=""><figcaption><p>Children Layout changing between Regular and Top to Bottom Stack</p></figcaption></figure>

Children can still control their position values relative to the stacking, so a child can change its X and Y value. If a child changes its Y or Height value in a Top to Bottom Stack, then subsequent children are adjusted react to this change immediately.

<figure><img src="../../.gitbook/assets/25_06 47 31 (1) (1).gif" alt=""><figcaption><p>Changing position and size affects all children later in the stack</p></figcaption></figure>

Stacking can be combined with wrapping and stack spacing to create list boxes and inventory grids quickly.

<figure><img src="../../.gitbook/assets/25_06 50 09.gif" alt=""><figcaption><p>Stack, wrap, and children spacing can create grids easily</p></figcaption></figure>

Layout dependencies can go from parent-> child and child-> parent to create powerful layouts. We recommend that new users spend some time in the Gum UI Tool to see what can be created and to browse the documentation to see more example images and animations.

<figure><img src="../../.gitbook/assets/25_06 58 41.gif" alt=""><figcaption><p>Gum layout is the best! Seriously! Just try it out and you'll be surprised how easy it is to create flexible, responsive layouts.</p></figcaption></figure>

### What's Next?

If you're ready to use Gum, then head on over to the Setup page. The MonoGame section provides information specific to MonoGame, but the rest of the Gum documentation site provides information which applies both to the Gum UI Tool and coding of Gum in MonoGame projects.

Also, if you have any questions, head on over to [our Discord](https://discord.gg/a6YcjnhgJN) - we're eager to help new users get their Gum projects up and running quickly!
