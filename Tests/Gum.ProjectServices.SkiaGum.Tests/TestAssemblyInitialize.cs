// SkiaGum's SVG export drives process-wide statics (ObjectFinder.Self,
// SystemManagers.Default, GumService.Default). Disable parallel execution so
// concurrent export tests don't stomp on that shared state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
