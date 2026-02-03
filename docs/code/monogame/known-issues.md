# Known Issues

Gum projects can be loaded and used in MonoGame projects with most features available. The following lists the missing features and known issues as of January 2025. These features are actively being worked on and in the future these will be improved. If you are waiting for a particular feature for your game and would like to see it prioritized, please make a request on the Gum Discord or in a GitHub issue.

## Missing Features

* Variable References - note that variable references propagate variable assignments, so you can use variable references in your project, but they will not update in real-time in your game
* Skia is not directly supported, a NuGet package exists to add ColoredCircle, Arc, and RoundedRectangle. For more information see the [Apos.Shapes](../standard-visuals/shapes-apos.shapes.md) page.
