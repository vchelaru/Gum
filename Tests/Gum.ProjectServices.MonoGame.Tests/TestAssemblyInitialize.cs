// MonoGameScreenshotService drives process-wide statics (GumService.Default, SystemManagers.Default,
// ShapeRenderer.Self). Disable parallel execution so concurrent screenshot tests don't stomp on that
// shared state — mirrors Gum.ProjectServices.Raylib.Tests' TestAssemblyInitialize.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
