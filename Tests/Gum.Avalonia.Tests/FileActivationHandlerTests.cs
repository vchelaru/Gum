using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Platform.Storage;
using Gum.Avalonia.Services;
using Gum.Startup;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// macOS delivers a Finder-opened document as a file activation, not a command-line argument
/// (#5130); the handler turns it into an open request for the shared router.
/// </summary>
public class FileActivationHandlerTests
{
    [AvaloniaFact]
    public void HandleActivated_FileActivation_SendsLocalFilePathsToRouter()
    {
        string projectPath = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        Mock<IStorageFile> file = new Mock<IStorageFile>();
        file.SetupGet(f => f.Path).Returns(new Uri(projectPath));
        Mock<IProjectOpenRequestRouter> router = new Mock<IProjectOpenRequestRouter>();
        List<string> requested = new List<string>();
        router.Setup(r => r.RequestOpenAsync(It.IsAny<IEnumerable<string>>()))
            .Callback<IEnumerable<string>>(paths => requested.AddRange(paths))
            .Returns(Task.CompletedTask);
        FileActivationHandler handler = new FileActivationHandler(new Lazy<IProjectOpenRequestRouter>(() => router.Object));

        handler.HandleActivated(null, new FileActivatedEventArgs(new IStorageItem[] { file.Object }));

        requested.ShouldBe(new[] { projectPath });
    }

    [AvaloniaFact]
    public void HandleActivated_NonFileActivation_DoesNothing()
    {
        Mock<IProjectOpenRequestRouter> router = new Mock<IProjectOpenRequestRouter>();
        FileActivationHandler handler = new FileActivationHandler(new Lazy<IProjectOpenRequestRouter>(() => router.Object));

        handler.HandleActivated(null, new ActivatedEventArgs(ActivationKind.Reopen));

        router.Verify(r => r.RequestOpenAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }
}
