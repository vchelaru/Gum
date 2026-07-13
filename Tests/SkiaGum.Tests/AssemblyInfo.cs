using Xunit;

// SkiaGum tests share global mutable state (GumService.Default, SystemManagers.Default,
// FrameworkElement.DefaultFormsTemplates) via the SkiaGum.Standalone GumService and other
// process-wide statics -- the same pattern every other Gum test project already serializes
// against (see MonoGameGum.Tests, RaylibGum.Tests, SilkNetGum.Tests, etc.). Without this,
// xunit's default parallel test-class execution intermittently corrupts another class's
// in-flight assertion when two classes call GumService.Default.Initialize() concurrently
// (observed: GumServiceTests.FrameworkElement_AddToRoot_ShouldAddVisualToRoot flaking against
// a concurrently-run Forms test class).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
