using MonoGameGum.TestsCommon;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Self-test for <see cref="RuntimeEventRecorder"/>: the allocation tests lean on it to name what the runtime did
/// inside a failing call (#8190), so it must report an exception thrown and caught on the calling thread.
/// </summary>
public class RuntimeEventRecorderTests
{
    [Fact]
    public void Describe_ExceptionThrownInWindow_ListsItOnTheCallingThread()
    {
        using RuntimeEventRecorder recorder = new();
        long threadId = RuntimeEventRecorder.CurrentOsThreadId();

        DateTime from = DateTime.UtcNow;
        try
        {
            throw new InvalidOperationException("recorded-by-8190");
        }
        catch (InvalidOperationException)
        {
        }
        DateTime to = DateTime.UtcNow;

        string description = recorder.Describe(from.AddMilliseconds(-50), to.AddMilliseconds(50), threadId);

        description.ShouldContain("InvalidOperationException");
        if (threadId != -1)
        {
            description.ShouldContain("TEST");
        }
    }
}
