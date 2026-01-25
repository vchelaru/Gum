# Migrating to 2026 February

This release is still being worked on, planned changes are listed here.



## \[Breaking] Cursor Visual Interaction Uses Only HasEvents

Previous versions of the Gum runtime would interact with a visual if its `HasEvents` property was set to true and also if it has any events such as `Click` assigned. This behavior was confusing and did not respect the `HasEvents` property. Now, a visual will react to (and consume) events if its `HasEvents` property is set to true.

Most projects will not be affected by this; however, projects which explicitly set `HasEvents` to true on a visual will now have events consumed by that visual.

This is most likely a problem if a Standard Element in the Gum tool (such as NineSlice) has its Has Events variable set to true on the Standard Element itself, which makes this value true for all instances of Standard Element.

FlatRedBall continues to use the old behavior, so this change does not break FlatRedBall projects.

Furthermore, the old behavior can still be enabled by explicitly calling this code **after initializing Gum**.

```csharp
ICursor.VisualOverBehavior = VisualOverBehavior.OnlyIfEventsAreNotNullAndHasEventsIsTrue;
```

Note that this old behavior can cause confusion when working with visual elements so keeping the old behavior is not recommended.

Furthermore, the following runtimes now default to HasEvents set to false. Previously these were set to true by default:

* NineSliceRuntime
* PolygonRuntime
* TextRuntime
