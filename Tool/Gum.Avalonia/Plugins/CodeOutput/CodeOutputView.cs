using System;
using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaDataUi;
using CodeOutputPlugin;
using CodeOutputPlugin.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices.CodeGeneration;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace Gum.Avalonia.Plugins.CodeOutput;

/// <summary>
/// The Avalonia Code tab: the generated-code preview with its object/state choice, and the code
/// generation settings (the setup prompt until a code project root is set, then the settings grid
/// from the shared <see cref="CodeOutputSettingsMembers"/>) with the Generate button. Twin of the WPF
/// <c>CodeWindow</c>.
/// </summary>
public sealed class CodeOutputView : Grid, ICodeOutputTabHost
{
    private static readonly IValueConverter SetupTitleConverter =
        new FuncValueConverter<bool, string>(needsSetup => needsSetup ? "Code Generation Setup" : "Code Generation");

    private readonly CodeOutputSettingsMembers _settingsMembers;
    private readonly DataUiGrid _grid;
    private readonly TextBox _code;
    private readonly Button _generate;

    /// <summary>Builds the view over the Code tab's view model and settings members.</summary>
    public CodeOutputView(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers)
    {
        _settingsMembers = settingsMembers;
        DataContext = viewModel;

        Grid previewHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 2) };
        previewHeader.Children.Add(new TextBlock { Text = "Preview", VerticalAlignment = VerticalAlignment.Center });
        StackPanel previewChoice = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        previewChoice.Children.Add(CreateToggle("Object", "Selected Object", nameof(CodeWindowViewModel.IsSelectedObjectSelected)));
        previewChoice.Children.Add(CreateToggle("State", "Selected State", nameof(CodeWindowViewModel.IsSelectedStateSelected)));
        Grid.SetColumn(previewChoice, 1);
        previewHeader.Children.Add(previewChoice);

        _code = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,DejaVu Sans Mono,monospace"),
            VerticalContentAlignment = VerticalAlignment.Top,
        };
        _code.Bind(TextBox.TextProperty, new Binding(nameof(CodeWindowViewModel.Code)));
        ScrollViewer.SetVerticalScrollBarVisibility(_code, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(_code, ScrollBarVisibility.Auto);

        DockPanel preview = new DockPanel();
        DockPanel.SetDock(previewHeader, Dock.Top);
        preview.Children.Add(previewHeader);
        preview.Children.Add(_code);

        TextBlock settingsTitle = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        settingsTitle.Bind(TextBlock.TextProperty, new Binding(nameof(CodeWindowViewModel.NeedsSetup)) { Converter = SetupTitleConverter });

        _generate = new Button { Content = "Generate", Padding = new Thickness(4, 1), Margin = new Thickness(0, 0, 8, 0) };
        _generate.Click += (_, _) => GenerateCodeClicked?.Invoke(this, EventArgs.Empty);
        StackPanel generatePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        generatePanel.Children.Add(_generate);
        generatePanel.Children.Add(CreateToggle("This", "This element only", nameof(CodeWindowViewModel.IsSelectedOnlyGenerating)));
        generatePanel.Children.Add(CreateToggle("All", "All elements in project", nameof(CodeWindowViewModel.IsAllInProjectGenerating)));
        generatePanel.Bind(IsVisibleProperty, new MultiBinding
        {
            Converter = BoolConverters.And,
            Bindings =
            {
                new Binding(nameof(CodeWindowViewModel.CanGenerateCode)),
                new Binding(nameof(CodeWindowViewModel.NeedsSetup)) { Converter = BoolConverters.Not },
            },
        });

        Grid settingsHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 2) };
        settingsHeader.Children.Add(settingsTitle);
        Grid.SetColumn(generatePanel, 1);
        settingsHeader.Children.Add(generatePanel);

        Button manual = new Button { Content = "Manual", Padding = new Thickness(8), Margin = new Thickness(0, 0, 8, 0) };
        manual.Click += (_, _) => _settingsMembers.ChooseManualSetup();
        Button auto = new Button { Content = "Auto", Padding = new Thickness(8) };
        auto.Click += (_, _) => _settingsMembers.ApplyAutoSetup();
        StackPanel setupButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        setupButtons.Children.Add(manual);
        setupButtons.Children.Add(auto);
        setupButtons.Bind(IsVisibleProperty, new Binding(nameof(CodeWindowViewModel.NeedsSetup)));

        _grid = new DataUiGrid();
        _grid.Bind(IsVisibleProperty, new Binding(nameof(CodeWindowViewModel.NeedsSetup)) { Converter = BoolConverters.Not });
        _grid.PropertyChange += (_, _) => CodeOutputSettingsPropertyChanged?.Invoke(this, EventArgs.Empty);

        Grid settingsBody = new Grid();
        settingsBody.Children.Add(setupButtons);
        settingsBody.Children.Add(_grid);

        DockPanel settings = new DockPanel();
        DockPanel.SetDock(settingsHeader, Dock.Top);
        settings.Children.Add(settingsHeader);
        settings.Children.Add(settingsBody);

        Grid generateUi = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,*") };
        GridSplitter splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1);
        Border settingsBorder = new Border { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1, 0, 0, 0), Child = settings };
        Grid.SetColumn(settingsBorder, 2);
        generateUi.Children.Add(preview);
        generateUi.Children.Add(splitter);
        generateUi.Children.Add(settingsBorder);
        generateUi.Bind(IsVisibleProperty, new Binding(nameof(CodeWindowViewModel.IsGenerateCodeUiVisible)));

        TextBlock noGeneration = new TextBlock
        {
            Text = "Standard Elements cannot be generated because the Gum NuGet packages linked by your project already include Standard Element runtime classes",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4),
        };
        noGeneration.Bind(IsVisibleProperty, new Binding(nameof(CodeWindowViewModel.IsNoGenerationAvailableUiVisible)));

        Children.Add(generateUi);
        Children.Add(noGeneration);

        _settingsMembers.SettingsChanged += (_, _) => CodeOutputSettingsPropertyChanged?.Invoke(this, EventArgs.Empty);
        _settingsMembers.RebuildRequested += (_, _) => RebuildSettings();
        RebuildSettings();
    }

    /// <inheritdoc/>
    public event EventHandler? CodeOutputSettingsPropertyChanged;

    /// <inheritdoc/>
    public event EventHandler? GenerateCodeClicked;

    // This head has no Generate All button (the WPF head's is disabled as well).
    event EventHandler? ICodeOutputTabHost.GenerateAllCodeClicked
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    object ICodeOutputTabHost.Control => this;

    /// <inheritdoc/>
    public CodeOutputProjectSettings? CodeOutputProjectSettings
    {
        get => _settingsMembers.ProjectSettings;
        set
        {
            // The setup prompt depends on the project settings, so a new value rebuilds the rows.
            if (_settingsMembers.ProjectSettings == value)
            {
                return;
            }
            _settingsMembers.ProjectSettings = value;
            RebuildSettings();
        }
    }

    /// <inheritdoc/>
    public CodeOutputElementSettings? CodeOutputElementSettings
    {
        get => _settingsMembers.ElementSettings;
        set
        {
            _settingsMembers.ElementSettings = value;
            _grid.Instance = value;
            RebuildSettings();
        }
    }

    /// <summary>The settings grid, for tests.</summary>
    internal DataUiGrid SettingsGrid => _grid;

    /// <summary>The code preview, for tests.</summary>
    internal TextBox CodeTextBox => _code;

    /// <summary>The Generate button, for tests.</summary>
    internal Button GenerateButton => _generate;

    private void RebuildSettings() => _grid.SetCategories(_settingsMembers.BuildCategories());

    private static ToggleButton CreateToggle(string text, string tip, string property)
    {
        ToggleButton toggle = new ToggleButton { Content = text, Padding = new Thickness(6, 1) };
        ToolTip.SetTip(toggle, tip);
        toggle.Bind(ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });
        return toggle;
    }
}

/// <summary>
/// The Avalonia head's Code Output plugin: <see cref="CodeOutputPluginBase"/> with
/// <see cref="CodeOutputView"/>. The delete dialog's "delete custom code" option is not offered yet,
/// so deleting an element here keeps its hand-written code file.
/// </summary>
[Export(typeof(PluginBase))]
public class MainCodeOutputPlugin : CodeOutputPluginBase
{
    /// <summary>Creates the plugin.</summary>
    [ImportingConstructor]
    public MainCodeOutputPlugin(
        IGuiCommands guiCommands,
        IDialogService dialogService,
        INameVerifier nameVerifier,
        LocalizationService localizationService,
        IProjectState projectState,
        ITypeManager typeManager,
        IOutputManager outputManager,
        ISelectedState selectedState,
        IRetryService retryService,
        IMessenger messenger,
        IFileCommands fileCommands)
        : base(guiCommands, dialogService, nameVerifier, localizationService, projectState, typeManager,
            outputManager, selectedState, retryService, messenger, fileCommands)
    {
    }

    /// <inheritdoc/>
    protected override ICodeOutputTabHost CreateTabHost(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers) =>
        new CodeOutputView(viewModel, settingsMembers);
}
