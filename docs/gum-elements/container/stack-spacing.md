# Stack Spacing

### Introduction

The Stack Spacing variable controls the additional padding between children when a container uses a Children Layout of either Top to Bottom Stack or Left to Right Stack. Stack Spacing serves as an alternative to adjusting the position of each item in a stack.

### Setting Stack Spacing

A larger Stack Spacing value increases the spacing between each child. By default Stack Spacing is set to 0 which means that no spacing is added between items in a stack.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Stack Spacing of 0 results in no space between children</p></figcaption></figure>

Changing the stack spacing adds gaps between each child as shown in the following animation.

<figure><img src="../../.gitbook/assets/01_09 24 31.gif" alt=""><figcaption><p>Stack Spacing used to add gaps between children</p></figcaption></figure>

Stack Spacing can also be a negative value resulting in overlapping children. The items in the following animation are partially transparent to show the overlap.

<figure><img src="../../.gitbook/assets/01_09 25 48.gif" alt=""><figcaption><p>Negative stack spacing results in overlaping children</p></figcaption></figure>

### Stack Spacing and Stacking Direction

Stack Spacing can be used for either Top to Bottom or Left to Right Stacking.

<figure><img src="../../.gitbook/assets/30_13 02 52.gif" alt=""><figcaption><p>Stack Spacing can apply spacing vertically or horizontally</p></figcaption></figure>

### Stack Spacing and Wrapping

Stack Spacing can be used on container instances which stack and wrap their children. As stack spacing increases, the amount of space allocated to each object also increases, resulting in wrapping occurring earlier.

If wrapping occurs, then stack spacing applies spacing between rows and columns as shown the following animation:

<figure><img src="../../.gitbook/assets/30_13 06 17.gif" alt=""><figcaption><p>Increasing Stack Spacing results in spacing between children both vertically and horizontally</p></figcaption></figure>

