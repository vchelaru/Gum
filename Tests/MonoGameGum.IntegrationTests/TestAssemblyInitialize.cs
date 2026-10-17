using Xunit;

// MonoGame Game / GraphicsDeviceManager use process-wide state (the GraphicsDevice,
// the OS window, LoaderManager.Self, GumService.Default). Running the integration
// test classes in parallel — xUnit's default — lets two tests instantiate Game
// concurrently, which crashes the test host with an AccessViolationException
// somewhere inside MonoGame's native graphics teardown. Force serial execution.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Default method order unless GUM_TEST_SHUFFLE_SEED is set (weekly shuffled-order workflow, #6106).
// xunit v3 has no class orderer; MainThreadTestFrameworkExecutor shuffles the class order.
[assembly: TestCaseOrderer(typeof(GumTestSupport.ShuffledTestCaseOrderer))]
