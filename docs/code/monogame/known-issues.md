# Known Issues

Gum projects can be loaded and used in MonoGame projects with most features available. The following lists the missing features and known issues. These features are actively being worked on and in the future these will be improved. If you are waiting for a particular feature for your game and would like to see it prioritized, please make a request on the Gum Discord or in a GitHub issue.

## Missing Features

* Variable References between components do not update on their own. References within one component update live. To propagate a change to a style component into other components, call `ApplyAllVariableReferences` and `RefreshStyles`. See [Runtime Variable References](../styling/runtime-variable-references.md).
* Shape fill and effects (gradient, drop shadow, dashed stroke) need a shape support package, `Gum.Shapes.MonoGame` or `Gum.Shapes.KNI`. For more information see the [Shapes](../standard-visuals/shapes-apos.shapes.md) page.
