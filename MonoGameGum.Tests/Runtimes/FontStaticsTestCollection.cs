using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Every test class that mutates <c>CustomSetPropertyOnRenderable.InMemoryFontCreator</c> or
/// <c>PropertyAssignmentError</c> (both process-wide statics) must be tagged
/// <c>[Collection(FontStaticsTestCollection.Name)]</c>. xUnit runs different collections in
/// parallel by default -- without this, two of these classes racing on the same static can flip
/// the creator or drop/duplicate a PropertyAssignmentError subscription mid-test, producing a
/// flaky failure that never reproduces when the test is run alone. This definition only disables
/// parallelization among collection members; unrelated test classes still run in parallel.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class FontStaticsTestCollection
{
    public const string Name = "Font statics (InMemoryFontCreator / PropertyAssignmentError)";
}
