# InteractiveGue

### Introduction

The InteractiveGue class provides a base class for custom runtimes which need to interact with the cursor. All visuals for Gum Forms use Visuals which inherit from InteractiveGue, such as ContainerRuntime.

InteractiveGue ultimately provides more events than most controls need for normal behavior. For example, Button does not include a Dragging event, but InteractiveGue (ultimately the Visual for all Buttons) does. Therefore, additional behavior can be added to Forms by subscribing to underlying events on their Visual.

## InteractiveGue Events

The following events are available on InteractiveGue

* Click
* Push
* LosePush
* DoubleClick
* RightClick
* RollOn (raised after the `RollOff` of the object the cursor left, if there is one)
* RollOff (raised before the `RollOn` of the object the cursor entered, if there is one)
* RollOver
* HoverOver
* Dragging
* RemovedAsPushed
* MouseWheelScroll

For more information on visual events, see the [Visual Events](../../events-and-interactivity/visual-events.md) page.
