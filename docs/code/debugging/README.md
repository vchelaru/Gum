# Debugging

This section collects tools and techniques for understanding, inspecting, and iterating on your Gum UI while your game is running.

* [Hot Reload](hot-reload.md) — push changes saved in the Gum tool into the running game without restarting, so you can adjust layouts, colors, and component structure and see the results immediately.
* [Runtime Snapshot](runtime-snapshot.md) — export the live UI tree to a Gum project you can open and inspect in the tool; the reverse of hot reload, and especially useful for code-only UIs.

## See also

Some debugging guidance lives alongside the subsystem it relates to:

* [Troubleshooting Events](../events-and-interactivity/troubleshooting-events.md) — diagnosing why clicks, hovers, and other input are not reaching a control.
* [Measuring Draw Calls](../performance-and-optimization/lastframedrawstates.md) — inspecting how many draw calls your UI produces.
* [Measuring Layout Calls](../performance-and-optimization/measuring-layout-calls.md) — finding excessive layout work.
* [Files and Fonts Troubleshooting](../files-and-fonts/troubleshooting.md) — resolving missing textures, fonts, and content-path problems.
