using Avalonia.Headless.XUnit;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Shell;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Avalonia head must supply every contract the headless core consumes, and its shell must
/// construct without a window server. Both run on Windows, macOS, and Linux in CI.
/// </summary>
public class HeadCompositionTests
{
    [Fact]
    public async Task AddGumAvalonia_ProvidesEveryHeadContract()
    {
        IServiceProvider services = TestAppBuilder.Services;

        List<string> missing = new List<string>();
        foreach (Type contract in GumCoreServiceCollectionExtensions.HeadProvidedContracts)
        {
            // Resolve on a worker with a deadline so a construction that blocks names itself
            // instead of hanging the whole run.
            Task<object?> resolve = Task.Run(() => services.GetService(contract));
            Task finished = await Task.WhenAny(resolve, Task.Delay(TimeSpan.FromSeconds(20)));
            if (finished != resolve)
            {
                missing.Add($"{contract.Name}: did not resolve within 20 s");
            }
            else if (await resolve == null)
            {
                missing.Add($"{contract.Name}: resolved to null");
            }
        }

        missing.ShouldBeEmpty(string.Join(", ", missing));
    }

    [Fact]
    public void ComposingTheHead_LoadsNoWindowsOnlyUiAssembly()
    {
        string[] windowsOnly = { "PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms", "System.Xaml" };

        _ = TestAppBuilder.Services.GetRequiredService<MainWindow>();

        List<string> loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName().Name ?? string.Empty)
            .Where(name => windowsOnly.Contains(name))
            .ToList();
        loaded.ShouldBeEmpty(string.Join(", ", loaded));
    }

    [AvaloniaFact]
    public void MainWindow_ConstructsAndShows_Headless()
    {
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();

        window.Show();

        window.IsVisible.ShouldBeTrue();
        window.Title.ShouldNotBeNullOrEmpty();
        window.Close();
    }

    [Fact]
    public void ParseFilter_TurnsWpfFilterIntoPickerTypes()
    {
        List<global::Avalonia.Platform.Storage.FilePickerFileType> types =
            AvaloniaDialogService.ParseFilter("PNG Files (*.png)|*.png|Gum project (*.gumx;*.gumj)|*.gumx;*.gumj");

        types.Count.ShouldBe(2);
        types[0].Name.ShouldBe("PNG Files (*.png)");
        types[0].Patterns.ShouldBe(new[] { "*.png" });
        types[1].Patterns.ShouldBe(new[] { "*.gumx", "*.gumj" });
    }
}
