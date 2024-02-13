# Runtime Objects (GueDeriving)

### Introduction

MonoGameGum provides common _runtime_ types which can be used to build your layouts. The runtime objects correspond to the _standard_ objects in the Gum tool. These provide a type-safe, expressive way of creating and working with Gum. The following runtime objects exist:

* ColoredRectangleRuntime
* ContainerRuntime
* NineSliceRuntime
* SpriteRuntime
* TextRuntime

Notice that at the time of this writing, only a subset of Gum standard types are available. This is likely to expand over time.

<figure><img src="../../.gitbook/assets/image (4) (1) (1).png" alt=""><figcaption><p>Gum "Standard" types</p></figcaption></figure>

All runtime objects inherit from GraphicalUiElement, which is the base class for all Gum objects. Therefore, all runtime objects share the same properties for position (such as X and XUnits), size (such as Width and WidthUnits), and rotation.

For a reference to the type of properties available on GraphicalUiElement, see the [Gum Elements General Properties](../../gum-elements/general-properties/) section. For information about working with GraphicalUiElement properties in code, see the [GraphicalUiElement](../../gum-code-reference/graphicaluielement/) reference.
