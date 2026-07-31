// RaylibScreenshotService drives process-wide statics (a single hidden raylib window,
// GumService.Default, SystemManagers.Default). Disable parallel execution so concurrent
// screenshot tests don't stomp on that shared state — mirrors
// Gum.ProjectServices.SkiaGum.Tests' TestAssemblyInitialize.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
