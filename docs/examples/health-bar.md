---
description: Introduction
---

# Health Bar

### Introduction

Health bars are common UI elements in games. A similar element to health bars are progress bars. Even though the two are used in different situations, the layout for these two is the same.

### Creating the Component

First we'll define the component:

1. Open Gum
2. Open or create a new Gum project
3. Right-click on the **Components** folder
4. Name the component HealthBar
5. Resize the HealthBar component so it is wider than it is tall. For example, assign a Width of 200 and Height 32.



### Adding a Background

Next we'll add a background to our HealthBar Component

1. Drag+drop a ColoredRectangle into the HealthBar\
   ![](<../.gitbook/assets/image (5).png>)
2. Select the newly-created ColoredRectangleInstance
3. Select the Alignment tab
4.  Click the Fill Dock button\


    <figure><img src="../.gitbook/assets/image (7).png" alt=""><figcaption></figcaption></figure>
5.  Change the ColoredRectangleInstance color to black\


    <figure><img src="../.gitbook/assets/image (11).png" alt=""><figcaption></figcaption></figure>

Now we have a black background to our HealthBar

### Creating an Inner Container

The HealthBar displays its current health with another rectangle. This second rectangle will be contained inside a container, which will provide a boundary. To add an inner container:

1. Drag+drop a Container onto the HealthBar
2. Select the Alignment tab
3. Click the Fill Dock button
4. Change Width and Height to -8 to provide a 4 pixel margin on each side.

<figure><img src="../.gitbook/assets/image (6).png" alt=""><figcaption></figcaption></figure>

### Adding the Foreground Rectangle

Finally we'll add the foreground rectangle which displays the health:

1. Drag+drop another ColoredRectangle onto the ContainerInstance
2. Click the Alignment tab
3. Click the Fill Dock button
4. Change the following values:
   1. X Units to Pixels from Left
   2. X Origin to Left
   3. Width to Percentage of Container
   4. Width to 100

Now, the Width value can change between 0 and 100 to indicate the health percentage.

<figure><img src="../.gitbook/assets/image (8).png" alt=""><figcaption></figcaption></figure>

Exposing the Width of the inner rectangle may be needed to change instances of each HealthBar.
