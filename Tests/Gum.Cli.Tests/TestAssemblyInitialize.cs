// Gum uses some statics internally (ObjectFinder.Self).
// Disable parallel execution to avoid random test failures.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
