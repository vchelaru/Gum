// Gum uses some statics internally (ObjectFinder.Self, StandardElementsManager.Self).
// Disable parallel execution to avoid random test failures.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
// Default order unless GUM_TEST_SHUFFLE_SEED is set (weekly shuffled-order workflow, #5820, #5840).
[assembly: TestCollectionOrderer("GumTestSupport.ShuffledTestCollectionOrderer", "Gum.Presentation.Tests")]
[assembly: TestCaseOrderer("GumTestSupport.ShuffledTestCaseOrderer", "Gum.Presentation.Tests")]
