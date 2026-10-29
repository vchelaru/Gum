namespace SkiaGum.GpuTests;

/// <summary>
/// One GL context shared by every GPU test class, disposed when the last of them finishes. Test classes join the
/// collection with <c>[Collection(GpuCollection.Name)]</c> and take a <see cref="WglGpuContext"/> in their constructor.
/// </summary>
[CollectionDefinition(Name)]
public class GpuCollection : ICollectionFixture<WglGpuContext>
{
    public const string Name = "Gpu";
}
