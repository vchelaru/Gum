using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Gum.ProjectServices;
using Gum.Services.Dialogs;

namespace HtmlToGumPlugin;

/// <summary>
/// The Import HTML options dialog: the source (a local file or a URL), the CSS root selector, the
/// screen name, the viewport, the responsive flag and the destination subfolder. Import checks the
/// source and leaves the cleaned-up choices in <see cref="Result"/>. Either head supplies the view.
/// </summary>
public class ImportHtmlOptionsViewModel : DialogViewModel
{
    private readonly IDialogService _dialogService;
    private readonly ImportOptions _defaults;

    /// <summary>Creates the dialog showing <paramref name="defaults"/>.</summary>
    public ImportHtmlOptionsViewModel(IDialogService dialogService, ImportOptions defaults)
    {
        _dialogService = dialogService;
        _defaults = defaults;
        AffirmativeText = "Import";
        NegativeText = "Cancel";
        // Before the properties: the IsUrl setter refreshes the command.
        BrowseCommand = new RelayCommand(Browse, () => IsLocalFile);
        IsUrl = defaults.IsUrl;
        HtmlPath = defaults.HtmlPath;
        Selector = defaults.Selector;
        ScreenName = defaults.ScreenName;
        Width = defaults.Width;
        Height = defaults.Height;
        NoResponsive = defaults.NoResponsive;
        DestinationSubfolder = defaults.DestinationSubfolder;
    }

    /// <summary>The window title.</summary>
    public string Title => "Import HTML options";

    /// <summary>Whether the source is a URL rather than a local file.</summary>
    public bool IsUrl
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                NotifyPropertyChanged(nameof(IsLocalFile));
                BrowseCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether the source is a local file; the other radio button.</summary>
    public bool IsLocalFile
    {
        get => !IsUrl;
        set => IsUrl = !value;
    }

    /// <summary>The local file path or the URL.</summary>
    public string HtmlPath { get => Get<string>() ?? ""; set => Set(value); }

    /// <summary>The CSS selector of the element to import.</summary>
    public string Selector { get => Get<string>() ?? ""; set => Set(value); }

    /// <summary>The name of the screen to create.</summary>
    public string ScreenName { get => Get<string>() ?? ""; set => Set(value); }

    /// <summary>The viewport width the page is laid out at.</summary>
    public int Width { get => Get<int>(); set => Set(value); }

    /// <summary>The viewport height the page is laid out at.</summary>
    public int Height { get => Get<int>(); set => Set(value); }

    /// <summary>Whether to pass --no-responsive to the converter.</summary>
    public bool NoResponsive { get => Get<bool>(); set => Set(value); }

    /// <summary>An optional folder under Screens to import into.</summary>
    public string DestinationSubfolder { get => Get<string>() ?? ""; set => Set(value); }

    /// <summary>The hint under the subfolder field.</summary>
    public string SubfolderHint => "Optional. Avoids name conflicts by importing under Screens/<subfolder>/.";

    /// <summary>Picks a local HTML file; enabled while the source is a local file.</summary>
    public RelayCommand BrowseCommand { get; }

    /// <summary>The checked choices once Import was pressed, or null.</summary>
    public ImportOptions? Result { get; private set; }

    /// <summary>Checks the source, keeps the cleaned-up choices in <see cref="Result"/> and closes.</summary>
    public override void OnAffirmative()
    {
        string source = HtmlPath.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            _dialogService.ShowMessage("Enter a local HTML file path or a URL.", "Import HTML");
            return;
        }
        if (IsUrl)
        {
            source = HtmlImportNaming.NormalizeUrl(source);
            HtmlPath = source;
        }
        else if (!File.Exists(source))
        {
            _dialogService.ShowMessage("File not found:\n" + source, "Import HTML");
            return;
        }

        string name = string.IsNullOrWhiteSpace(ScreenName) ? _defaults.ScreenName : ScreenName.Trim();
        Result = new ImportOptions
        {
            HtmlPath = source,
            IsUrl = IsUrl,
            Selector = string.IsNullOrWhiteSpace(Selector) ? "body" : Selector.Trim(),
            ScreenName = MainHtmlToGumPlugin.SanitizeScreenName(name),
            Width = Width >= 1 ? Width : _defaults.Width,
            Height = Height >= 1 ? Height : _defaults.Height,
            NoResponsive = NoResponsive,
            DestinationSubfolder = DestinationSubfolder.Trim(),
        };
        base.OnAffirmative();
    }

    private void Browse()
    {
        List<string>? files = _dialogService.OpenFile(new OpenFileDialogOptions
        {
            Title = "Choose local HTML file",
            Filter = "HTML (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
        });
        if (files is not { Count: > 0 })
        {
            return;
        }
        HtmlPath = files[0];
        if (string.IsNullOrWhiteSpace(ScreenName) || ScreenName == MainHtmlToGumPlugin.DefaultScreenName)
        {
            ScreenName = MainHtmlToGumPlugin.SanitizeScreenName(Path.GetFileNameWithoutExtension(files[0]));
        }
    }
}
