using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// The content cache is shared by every thread that loads content (for example sprites applying cached
/// pixel data). An unsynchronized Dictionary corrupts under concurrent writes and then throws "Operations
/// that change non-concurrent collections must have exclusive access" on unrelated later calls.
/// </summary>
public class LoaderManagerCacheConcurrencyTests
{
    private sealed class Probe : IDisposable
    {
        public void Dispose() { }
    }

    [Fact]
    public void AddAndGet_ManyThreadsOnDistinctKeys_KeepEveryEntryAndNeverThrow()
    {
        const int threadCount = 8;
        const int keysPerThread = 5000;
        LoaderManager sut = new();
        using System.Threading.Barrier start = new(threadCount);

        Task[] tasks = Enumerable.Range(0, threadCount).Select(thread => Task.Run(() =>
        {
            start.SignalAndWait();
            for (int i = 0; i < keysPerThread; i++)
            {
                string key = $"t{thread}_{i}";
                sut.AddDisposable(key, new Probe(), LoaderManager.ExistingContentBehavior.Replace);
                sut.TryGetCachedDisposable<Probe>(key).ShouldNotBeNull();
                sut.GetDisposable(key).ShouldNotBeNull();
            }
        })).ToArray();

        Task.WaitAll(tasks, TimeSpan.FromSeconds(30)).ShouldBeTrue();
        sut.CachedDisposables.Count.ShouldBe(threadCount * keysPerThread);
    }
}
