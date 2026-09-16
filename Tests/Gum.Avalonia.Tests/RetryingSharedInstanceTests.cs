using System;
using Gum.Avalonia.Canvas;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The retry-after-failure seam <see cref="GameRenderDeviceHost"/> uses for its shared device: a
/// transient init failure must not poison the cached instance for every later caller (#4786).
/// </summary>
public class RetryingSharedInstanceTests
{
    private sealed class FakeDevice : IDisposable
    {
        public int Id;
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void GetOrCreate_DisposesAndDoesNotCache_WhenInitializeThrows()
    {
        RetryingSharedInstance<FakeDevice> shared = new();
        FakeDevice? created = null;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(() =>
            shared.GetOrCreate(
                () => created = new FakeDevice { Id = 1 },
                _ => throw new InvalidOperationException("boom")));

        thrown.Message.ShouldBe("boom");
        created.ShouldNotBeNull();
        created!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public void GetOrCreate_RetriesAfterAFailure_AndCachesTheSuccessfulInstance()
    {
        RetryingSharedInstance<FakeDevice> shared = new();
        int factoryCalls = 0;
        bool failNext = true;

        Should.Throw<InvalidOperationException>(() =>
            shared.GetOrCreate(
                () => new FakeDevice { Id = ++factoryCalls },
                _ =>
                {
                    if (failNext)
                    {
                        failNext = false;
                        throw new InvalidOperationException("boom");
                    }
                }));

        FakeDevice second = shared.GetOrCreate(() => new FakeDevice { Id = ++factoryCalls }, _ => { });
        FakeDevice third = shared.GetOrCreate(() => new FakeDevice { Id = ++factoryCalls }, _ => { });

        // One factory call for the failed attempt, one for the successful retry; the third
        // GetOrCreate must reuse the cached instance rather than invoking the factory again.
        factoryCalls.ShouldBe(2);
        second.Id.ShouldBe(2);
        ReferenceEquals(second, third).ShouldBeTrue();
    }
}
