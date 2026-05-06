using Xunit;

// Bundle tests share global mutable state with the production tool code via ObjectFinder.Self
// (specifically ObjectFinder.Self.GumProjectSave, which walker tests assign before each Walk
// call). CLAUDE.md documents ObjectFinder.Self as an intentional process-wide singleton with
// no plans to migrate. If two test classes set GumProjectSave in parallel, whichever runs
// second wins and the first sees the wrong project — a silent failure mode worse than a crash.
// Serialize this assembly until/unless ObjectFinder gains scoped-instance support.
// The full suite still runs in well under a second.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
