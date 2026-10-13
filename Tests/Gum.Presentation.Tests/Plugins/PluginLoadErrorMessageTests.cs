using System;
using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins;

public class PluginLoadErrorMessageTests
{
    [Fact]
    public void Build_NamesTheRunningExecutableAndPluginFolder()
    {
        string message = PluginLoadErrorMessage.Build(
            new InvalidOperationException("boom"),
            executablePath: @"C:\Tools\Gum\Gum.exe",
            pluginFolder: @"C:\Tools\Gum\Plugins\",
            presentationAssemblyPath: @"C:\Tools\Gum\Gum.Presentation.dll");

        message.ShouldContain(@"C:\Tools\Gum\Gum.exe");
        message.ShouldContain(@"C:\Tools\Gum\Plugins\");
        message.ShouldContain(@"C:\Tools\Gum\Gum.Presentation.dll");
        message.ShouldContain("boom");
    }

    [Fact]
    public void Build_IncludesEveryLevelOfTheInnerExceptionChain()
    {
        Exception exception = new Exception(
            "outer", new Exception("middle", new MissingMethodException("innermost detail")));

        string message = PluginLoadErrorMessage.Build(exception, "Gum.exe", "Plugins", "Gum.Presentation.dll");

        message.ShouldContain("outer");
        message.ShouldContain("middle");
        message.ShouldContain("innermost detail");
        message.ShouldContain(nameof(MissingMethodException));
    }

    [Fact]
    public void Build_IncludesLoaderExceptionsOfAReflectionTypeLoadException()
    {
        System.Reflection.ReflectionTypeLoadException exception = new System.Reflection.ReflectionTypeLoadException(
            new Type?[] { null },
            new Exception?[] { new Exception("loader detail"), null });

        string message = PluginLoadErrorMessage.Build(exception, "Gum.exe", "Plugins", "Gum.Presentation.dll");

        message.ShouldContain("loader detail");
    }
}
