// Gum uses some statics internally (ObjectFinder.Self).
// Disable parallel execution to avoid random test failures.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Read back by PackCommandTests.Pack_with_shake_keeps_the_roots_recorded_in_a_game_assembly.
[assembly: Gum.DataTypes.GumElementRoot("FromAssembly")]
