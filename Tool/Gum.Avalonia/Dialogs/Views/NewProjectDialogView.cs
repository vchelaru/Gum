using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Automation;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using Gum.Avalonia.Controls;
using Gum.Avalonia.Converters;
using Gum.Avalonia.Themes;
using Gum.Dialogs;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// File &gt; New Project: name, location and file format, the Forms options, an optional C# code project, the
/// files that will be written, and the equivalent <c>gumcli new</c> command, in one scrolling column. Bound to
/// <see cref="NewProjectDialogViewModel"/>; the window keeps the title and the OK/Cancel row in place.
/// </summary>
public sealed class NewProjectDialogView : StackPanel
{
    /// <summary>Name of the Folders section's expander.</summary>
    public const string FoldersExpanderName = "PART_FoldersExpander";

    /// <summary>Name of the line under Location that names the project folder.</summary>
    public const string FolderNoticeName = "PART_FolderNotice";

    /// <summary>Name of the project folder path under the heading of that line.</summary>
    public const string FolderNoticePathName = "PART_FolderNoticePath";

    /// <summary>Name of the summary in the collapsed Folders header.</summary>
    public const string FoldersSummaryName = "PART_FoldersSummary";

    /// <summary>Name of the Command line section's expander.</summary>
    public const string CommandLineExpanderName = "PART_CommandLineExpander";

    /// <summary>Name of the button that copies the command line.</summary>
    public const string CopyButtonName = "PART_CopyCommandLine";

    /// <summary>Name of the Include recommended packages check box.</summary>
    public const string RecommendedPackagesCheckBoxName = "PART_RecommendedPackages";
    public const string GenerateCodeCheckBoxName = "PART_GenerateCode";

    /// <summary>Name of the info icon in the existing-code-project banner.</summary>
    public const string WarningInfoName = "PART_WarningInfo";

    /// <summary>How long the copy icon shows its check after a copy; tests lengthen it to capture the state.</summary>
    public static TimeSpan CopiedCheckDuration { get; set; } = TimeSpan.FromMilliseconds(1200);

    private const double BaseWidth = 440;
    private const double MaxWidthFitsWindow = 850;
    private const double CheckBoxTextWidth = 340;
    private const string FontSizeKey = "ControlContentThemeFontSize";
    private const string MonospaceFonts = "Cascadia Mono, Consolas, Menlo, monospace";

    private readonly FluentIcon _copyIcon;
    private readonly DispatcherTimer _copiedTimer;

    /// <summary>Builds the view.</summary>
    public NewProjectDialogView()
    {
        // 440 at the default size, and wider as the tool zooms in, so the two-column rows keep the room their text needs.
        this.Bind(WidthProperty, this.GetResourceObservable(FontSizeKey, value => (object)Math.Min(MaxWidthFitsWindow, Math.Max(BaseWidth, BaseWidth * ((value is double size ? size : 12d) / 12d)))));
        Spacing = 0;
        DialogWindow.SetDialogTitle(this, "New Project");

        _copyIcon = CreateIcon(Icon.Copy, 1.15);
        _copyIcon.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        _copiedTimer = new DispatcherTimer { Interval = CopiedCheckDuration };
        _copiedTimer.Tick += (_, _) =>
        {
            _copiedTimer.Stop();
            _copyIcon.Icon = Icon.Copy;
            _copyIcon.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        };

        Children.Add(CreateStatus());

        StackPanel form = new StackPanel();
        form.Bind(IsEnabledProperty, new Binding(nameof(NewProjectDialogViewModel.IsEditable)));
        form.Children.Add(CreateNameAndFormat());
        form.Children.Add(CreateLocation());
        form.Children.Add(CreateExistingProjectWarning());
        form.Children.Add(CreateTargetNotEmptyWarning());
        form.Children.Add(CreateRefusal());
        form.Children.Add(CreateFormsSection());
        form.Children.Add(CreateCodeProjectSection());
        form.Children.Add(CreateFoldersSection());
        form.Children.Add(CreateCommandLineSection());
        Children.Add(form);

        TextBlock validation = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 220 };
        validation.Bind(TextBlock.TextProperty, new Binding(nameof(NewProjectDialogViewModel.ValidationMessage)));
        validation.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
        DialogWindow.SetAuxiliaryActions(this, validation);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is NewProjectDialogViewModel viewModel)
            {
                viewModel.CommandLineCopyRequested += HandleCopyRequested;
            }
        };
    }

    #region Sections

    // Busy progress, the file-creation error, and the restore failure with its three ways forward. Above the
    // form so it is on screen whatever the scroll position.
    private Control CreateStatus()
    {
        StackPanel panel = new StackPanel { Spacing = 6 };

        StackPanel busy = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 8) };
        busy.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsBusy)));
        busy.Children.Add(new ProgressBar { IsIndeterminate = true });
        busy.Children.Add(Bound(new TextBlock { FontWeight = FontWeight.SemiBold }, TextBlock.TextProperty, nameof(NewProjectDialogViewModel.ProgressText)));
        busy.Children.Add(Bound(new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.7 },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.ProgressDetail)));
        panel.Children.Add(busy);

        TextBlock error = Bound(new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8) },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.ErrorMessage));
        error.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.HasError)));
        error.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
        panel.Children.Add(error);

        StackPanel restoreFailed = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 8) };
        restoreFailed.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsRestoreFailed)));
        restoreFailed.Children.Add(Bound(new TextBlock { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.RestoreFailureHeading)));
        SelectableTextBlock failure = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
        failure.Bind(TextBlock.TextProperty, new Binding(nameof(NewProjectDialogViewModel.RestoreFailureText)));
        failure.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
        restoreFailed.Children.Add(new ScrollViewer { Content = failure, MaxHeight = 120, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        actions.Children.Add(CommandButton("Retry", nameof(NewProjectDialogViewModel.RetryRestoreCommand)));
        actions.Children.Add(CommandButton("Continue without restore", nameof(NewProjectDialogViewModel.ContinueWithoutRestoreCommand)));
        actions.Children.Add(CommandButton("Back", nameof(NewProjectDialogViewModel.BackCommand)));
        restoreFailed.Children.Add(actions);
        panel.Children.Add(restoreFailed);
        return panel;
    }

    private Control CreateNameAndFormat()
    {
        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };

        StackPanel name = new StackPanel();
        name.Children.Add(FieldLabel(NewProjectDialogText.ProjectName, NewProjectDialogText.ProjectNameTip));
        TextBox nameBox = new TextBox { MinWidth = 90 };
        nameBox.Bind(TextBox.TextProperty, new Binding(nameof(NewProjectDialogViewModel.ProjectName)) { Mode = BindingMode.TwoWay });
        name.Children.Add(nameBox);
        grid.Children.Add(name);
        DialogWindow.FocusWhenOpened(nameBox, nameBox.SelectAll);

        StackPanel format = new StackPanel();
        Grid.SetColumn(format, 1);
        format.Children.Add(FieldLabel(NewProjectDialogText.FileFormat, NewProjectDialogText.FileFormatTip, alignRight: true));
        ComboBox formats = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 190 };
        formats.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(NewProjectDialogViewModel.FileFormats)));
        formats.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(NewProjectDialogViewModel.SelectedFileFormat)) { Mode = BindingMode.TwoWay });
        format.Children.Add(formats);
        grid.Children.Add(format);
        return grid;
    }

    private Control CreateLocation()
    {
        StackPanel panel = new StackPanel();
        panel.Children.Add(FieldLabel(NewProjectDialogText.Location, NewProjectDialogText.LocationTip));
        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        TextBox location = new TextBox();
        location.Bind(TextBox.TextProperty, new Binding(nameof(NewProjectDialogViewModel.Location)) { Mode = BindingMode.TwoWay });
        row.Children.Add(location);
        Button browse = CommandButton("Browse…", nameof(NewProjectDialogViewModel.BrowseCommand));
        browse.VerticalAlignment = VerticalAlignment.Stretch;
        browse.VerticalContentAlignment = VerticalAlignment.Center;
        browse.Padding = new Thickness(12, 0);
        AutomationProperties.SetName(browse, "Browse for a folder");
        Grid.SetColumn(browse, 1);
        row.Children.Add(browse);
        panel.Children.Add(row);
        panel.Children.Add(CreateFolderNotice());
        return panel;
    }

    // Where the project folder goes, in full: the path wraps instead of being cut, so the destination is always visible.
    private Control CreateFolderNotice()
    {
        FluentIcon icon = CreateIcon(Icon.FolderAdd, 1.1);
        icon.VerticalAlignment = VerticalAlignment.Top;
        icon.Margin = new Thickness(0, 1, 0, 0);
        icon.Opacity = 0.7;
        icon.Bind(FluentIcon.IconProperty, new Binding(nameof(NewProjectDialogViewModel.IsFolderNoticeCreating))
        {
            Converter = new FuncValueConverter<bool, Icon>(creating => creating ? Icon.FolderAdd : Icon.Folder),
        });

        Run heading = new Run();
        heading.Bind(Run.TextProperty, new Binding(nameof(NewProjectDialogViewModel.FolderNoticeHeading)) { Mode = BindingMode.OneWay });
        Run suffix = new Run();
        suffix.Bind(Run.TextProperty, new Binding(nameof(NewProjectDialogViewModel.FolderNoticeSuffix))
        {
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<string?, string>(text => string.IsNullOrEmpty(text) ? string.Empty : " " + text),
        });

        TextBlock text = new TextBlock { Name = FolderNoticeName, TextWrapping = TextWrapping.Wrap, Opacity = 0.85, Focusable = false };
        text.Inlines!.Add(heading);
        text.Inlines.Add(suffix);

        // On its own line, so the path always starts at the left edge of the text; the command line's mono style.
        FolderPathPanel path = new FolderPathPanel { Name = FolderNoticePathName, Opacity = 0.85 };
        path.Bind(FolderPathPanel.TextProperty, new Binding(nameof(NewProjectDialogViewModel.FolderNoticePath)));
        StackPanel lines = new StackPanel { Spacing = 1 };
        lines.Children.Add(text);
        lines.Children.Add(path);

        Grid notice = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 6, Margin = new Thickness(0, 5, 0, 0) };
        notice.Children.Add(icon);
        Grid.SetColumn(lines, 1);
        notice.Children.Add(lines);
        // While a slow check is out, the old answer is hidden but keeps its room, so the dialog does not jump.
        notice.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsFolderNoticeSpaceReserved)));
        notice.Bind(OpacityProperty, new Binding(nameof(NewProjectDialogViewModel.IsFolderNoticeVisible))
        {
            Converter = new FuncValueConverter<bool, double>(visible => visible ? 1d : 0d),
        });
        return notice;
    }

    private Control CreateExistingProjectWarning()
    {
        StackPanel text = new StackPanel();
        StackPanel heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        heading.Children.Add(new TextBlock { Text = NewProjectDialogText.WarningHeading, FontWeight = FontWeight.SemiBold });

        FluentIcon info = CreateInfoIcon(tipBinding: new Binding(nameof(NewProjectDialogViewModel.ExistingCodeProjectTip)), subject: "the existing code project warning");
        info.Name = WarningInfoName;
        heading.Children.Add(info);
        text.Children.Add(heading);
        text.Children.Add(Bound(new TextBlock { TextWrapping = TextWrapping.Wrap }, TextBlock.TextProperty, nameof(NewProjectDialogViewModel.ExistingCodeProjectText)));

        Control banner = CreateBanner(text);
        banner.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsExistingCodeProjectWarningVisible)));
        return banner;
    }

    private Control CreateTargetNotEmptyWarning()
    {
        StackPanel text = new StackPanel();
        text.Children.Add(new TextBlock { Text = NewProjectDialogText.TargetNotEmptyHeading, FontWeight = FontWeight.SemiBold });
        text.Children.Add(Bound(new TextBlock { TextWrapping = TextWrapping.Wrap }, TextBlock.TextProperty, nameof(NewProjectDialogViewModel.TargetNotEmptyText)));

        Control banner = CreateBanner(text);
        banner.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsTargetNotEmptyWarningVisible)));
        return banner;
    }

    // The project file or code project already exists: gumcli new refuses, so OK is off while this shows.
    private Control CreateRefusal()
    {
        StackPanel text = new StackPanel();
        text.Children.Add(new TextBlock { Text = NewProjectDialogText.RefusedHeading, FontWeight = FontWeight.SemiBold });
        text.Children.Add(Bound(new TextBlock { TextWrapping = TextWrapping.Wrap }, TextBlock.TextProperty, nameof(NewProjectDialogViewModel.RefusalText)));

        Control banner = CreateBanner(text, "Frb.Brushes.Error");
        banner.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsRefused)));
        return banner;
    }

    private Control CreateFormsSection()
    {
        StackPanel section = new StackPanel();
        section.Children.Add(Divider());
        CheckBox includeForms = new CheckBox { Content = LabelWithTip(NewProjectDialogText.IncludeForms, NewProjectDialogText.IncludeFormsTip, maxTextWidth: CheckBoxTextWidth) };
        includeForms.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeFormsControls)) { Mode = BindingMode.TwoWay });
        section.Children.Add(includeForms);
        section.Children.Add(HelpText(NewProjectDialogText.IncludeFormsHelp));

        StackPanel options = new StackPanel();
        options.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeFormsControls)));
        ThemeSelectionView themeSelection = new ThemeSelectionView(labelAbove: true, previewBeside: true, showRequirements: false, labelContent: LabelWithTip(NewProjectDialogText.Theme, NewProjectDialogText.ThemeTip));
        themeSelection.Bind(DataContextProperty, new Binding(nameof(NewProjectDialogViewModel.ThemeSelection)));
        options.Children.Add(themeSelection);
        CheckBox includeDemo = new CheckBox { Content = LabelWithTip(NewProjectDialogText.IncludeDemo, NewProjectDialogText.IncludeDemoTip, maxTextWidth: CheckBoxTextWidth), Margin = new Thickness(0, 2, 0, 0) };
        includeDemo.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeDemoScreenGum)) { Mode = BindingMode.TwoWay });
        options.Children.Add(includeDemo);
        section.Children.Add(options);
        return section;
    }

    private Control CreateCodeProjectSection()
    {
        StackPanel section = new StackPanel();
        section.Children.Add(Divider());
        CheckBox includeCode = new CheckBox { Content = LabelWithTip(NewProjectDialogText.CreateCodeProject, NewProjectDialogText.CreateCodeProjectTip, maxTextWidth: CheckBoxTextWidth) };
        includeCode.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.IncludeCodeProject)) { Mode = BindingMode.TwoWay });
        section.Children.Add(includeCode);
        section.Children.Add(HelpText(NewProjectDialogText.CreateCodeProjectHelp));

        StackPanel options = new StackPanel();
        options.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IncludeCodeProject)));
        options.Children.Add(FieldLabel(NewProjectDialogText.Platform, NewProjectDialogText.PlatformTip));
        ComboBox platform = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        platform.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(NewProjectDialogViewModel.Platforms)));
        platform.ItemTemplate = new FuncDataTemplate<NewProjectPlatformOption>((option, _) => new TextBlock { Text = option?.Label });
        platform.SelectedValueBinding = new Binding(nameof(NewProjectPlatformOption.Name));
        platform.Bind(SelectingItemsControl.SelectedValueProperty, new Binding(nameof(NewProjectDialogViewModel.PlatformName)) { Mode = BindingMode.TwoWay });
        options.Children.Add(platform);

        TextBlock platformNote = Bound(new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.PlatformNote));
        platformNote.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.PlatformNote)) { Converter = NotNullConverter.Instance });
        platformNote.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
        options.Children.Add(platformNote);

        CheckBox sourceLinked = new CheckBox { Content = NewProjectDialogText.UseLocalSource, Margin = new Thickness(0, 8, 0, 0) };
        sourceLinked.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.SourceLinked)) { Mode = BindingMode.TwoWay });
        sourceLinked.Bind(IsEnabledProperty, new Binding(nameof(NewProjectDialogViewModel.IsSourceLinkedEnabled)));
        options.Children.Add(sourceLinked);
        options.Children.Add(new TextBlock { Text = NewProjectDialogText.UseLocalSourceNote, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(21, 0, 0, 4) });

        CheckBox restore = new CheckBox { Content = NewProjectDialogText.RestorePackages };
        restore.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.RunRestore)) { Mode = BindingMode.TwoWay });
        options.Children.Add(restore);
        options.Children.Add(new TextBlock { Text = NewProjectDialogText.RestorePackagesNote, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(21, 0, 0, 4) });

        StackPanel recommended = new StackPanel();
        recommended.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsRecommendedPackagesVisible)));
        CheckBox recommendedPackages = new CheckBox { Name = RecommendedPackagesCheckBoxName, Content = NewProjectDialogText.RecommendedPackages };
        recommendedPackages.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.RecommendedPackagesChecked)) { Mode = BindingMode.TwoWay });
        recommendedPackages.Bind(IsEnabledProperty, new Binding(nameof(NewProjectDialogViewModel.IsRecommendedPackagesEnabled)));
        recommended.Children.Add(recommendedPackages);
        TextBlock recommendedNote = Bound(new TextBlock { Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(21, 0, 0, 4) },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.RecommendedPackagesNote));
        recommended.Children.Add(recommendedNote);
        options.Children.Add(recommended);

        StackPanel generateDemo = new StackPanel();
        generateDemo.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.IsGenerateCodeVisible)));
        CheckBox generateDemoCode = new CheckBox { Name = GenerateCodeCheckBoxName, Content = NewProjectDialogText.GenerateCode };
        generateDemoCode.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.GenerateCodeChecked)) { Mode = BindingMode.TwoWay });
        generateDemoCode.Bind(IsEnabledProperty, new Binding(nameof(NewProjectDialogViewModel.IsGenerateCodeEnabled)));
        generateDemo.Children.Add(generateDemoCode);
        generateDemo.Children.Add(Bound(new TextBlock { Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(21, 0, 0, 4) },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.GenerateCodeNote)));
        options.Children.Add(generateDemo);
        section.Children.Add(options);
        return section;
    }

    private Control CreateFoldersSection()
    {
        StackPanel content = new StackPanel();

        StackPanel links = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 4 };
        links.Children.Add(Link("Expand all", () => (DataContext as NewProjectDialogViewModel)?.SetFolderTreeExpanded(true)));
        links.Children.Add(Link("Collapse all", () => (DataContext as NewProjectDialogViewModel)?.SetFolderTreeExpanded(false)));
        content.Children.Add(links);

        TreeView tree = new TreeView { MaxHeight = 230, Padding = new Thickness(0), FontFamily = new FontFamily(MonospaceFonts), FontWeight = FontWeight.Normal };
        tree.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(NewProjectDialogViewModel.FolderTree)));
        tree.ItemTemplate = new FuncTreeDataTemplate<NewProjectLayoutNode>(
            (node, _) => CreateTreeRow(node),
            node => node.Children);
        tree.Styles.Add(CreateTreeItemStyle());
        ScrollViewer.SetHorizontalScrollBarVisibility(tree, ScrollBarVisibility.Auto);
        // The bars take their own space beside the rows instead of floating over them.
        ScrollViewer.SetAllowAutoHide(tree, false);
        Border treeBox = new Border { CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 4), Child = tree };
        treeBox.WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Field.Background");
        content.Children.Add(treeBox);

        // Shows beside the heading while the section is collapsed, so the answer is there without opening it.
        TextBlock summary = Bound(new TextBlock { Name = FoldersSummaryName, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Focusable = false },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.FoldersSummary));
        summary.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.FoldersSummary)) { Converter = NotNullConverter.Instance });
        return CreateSection(FoldersExpanderName, NewProjectDialogText.FoldersHeading, content, summary);
    }

    private Control CreateCommandLineSection()
    {
        StackPanel content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = NewProjectDialogText.CommandLineDescription,
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 6),
        });

        Grid box = new Grid();
        SelectableTextBlock command = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily(MonospaceFonts),
            FontWeight = FontWeight.Normal,
            Margin = new Thickness(10, 8, 38, 8),
        };
        command.Bind(TextBlock.TextProperty, new Binding(nameof(NewProjectDialogViewModel.CommandLine)));
        Border background = new Border { CornerRadius = new CornerRadius(3), MinHeight = 34, Child = command };
        background.WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Field.Background");
        box.Children.Add(background);

        Button copy = new Button
        {
            Name = CopyButtonName,
            Content = _copyIcon,
            Padding = new Thickness(5),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 3, 3, 0),
        };
        copy.Bind(Button.CommandProperty, new Binding(nameof(NewProjectDialogViewModel.CopyCommandLineCommand)));
        ToolTip.SetTip(copy, "Copy");
        AutomationProperties.SetName(copy, "Copy command line");
        box.Children.Add(copy);
        content.Children.Add(box);

        TextBlock note = Bound(new TextBlock { Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) },
            TextBlock.TextProperty, nameof(NewProjectDialogViewModel.CommandLineNote));
        note.Bind(IsVisibleProperty, new Binding(nameof(NewProjectDialogViewModel.CommandLineNote)) { Converter = NotNullConverter.Instance });
        content.Children.Add(note);

        return CreateSection(CommandLineExpanderName, NewProjectDialogText.CommandLineHeading, content);
    }

    #endregion

    #region Pieces

    // A disclosure with the section's heading and a rule above.
    private static Control CreateSection(string name, string title, Control content, Control? collapsedSummary = null)
    {
        StackPanel header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        header.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold });

        Disclosure disclosure = new Disclosure(header, content, title, collapsedSummary) { Name = name };
        StackPanel section = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        section.Children.Add(Divider(new Thickness(0, 0, 0, 6)));
        section.Children.Add(disclosure);
        return section;
    }

    /// <summary>
    /// A folder path that wraps between its folders. A plain wrapping text block breaks a Windows path after the drive's colon
    /// (the only break the text rules allow in it), leaving "C:" alone on the first line; here each folder is its own piece, so
    /// a line always holds whole folders, and one folder name longer than the line wraps inside itself. The full path is never cut.
    /// </summary>
    public sealed class FolderPathPanel : WrapPanel
    {
        /// <summary>The path shown.</summary>
        public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<FolderPathPanel, string?>(nameof(Text));

        static FolderPathPanel()
        {
            TextProperty.Changed.AddClassHandler<FolderPathPanel>((panel, _) => panel.Rebuild());
        }

        /// <summary>Builds the panel, empty.</summary>
        public FolderPathPanel()
        {
            Focusable = false;
        }

        /// <summary>The path shown, whole.</summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private void Rebuild()
        {
            Children.Clear();
            string text = Text ?? string.Empty;
            AutomationProperties.SetName(this, text);
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                bool lastChar = i == text.Length - 1;
                if (text[i] == System.IO.Path.DirectorySeparatorChar || text[i] == System.IO.Path.AltDirectorySeparatorChar || lastChar)
                {
                    Children.Add(CreatePiece(text.Substring(start, i - start + 1)));
                    start = i + 1;
                }
            }
        }

        private TextBlock CreatePiece(string piece)
        {
            TextBlock block = new TextBlock
            {
                Text = piece,
                FontFamily = new FontFamily(MonospaceFonts),
                FontWeight = FontWeight.Light,
                TextWrapping = TextWrapping.Wrap,
                Focusable = false,
            };
            // A folder name wider than the line wraps inside its piece instead of running out of the window.
            block.Bind(MaxWidthProperty, this.GetObservable(BoundsProperty, bounds => (object)(bounds.Width > 0 ? bounds.Width : double.PositiveInfinity)));
            return block;
        }
    }

    /// <summary>A heading with a chevron that shows or hides its content; flat, as the mock draws it.</summary>
    public sealed class Disclosure : StackPanel
    {
        private readonly FluentIcon _chevron;
        private readonly Control _content;
        private readonly Panel? _summaryHost;
        private bool _isExpanded;

        /// <summary>Builds the disclosure, collapsed.</summary>
        public Disclosure(Control header, Control content, string automationName, Control? collapsedSummary = null)
        {
            _content = content;
            _content.IsVisible = false;
            _chevron = CreateIcon(Icon.ChevronRight, 1.0);
            _chevron.Opacity = 0.7;

            // The last column takes what is left, so a long summary wraps instead of running out of the window.
            Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), ColumnSpacing = 4, Background = Brushes.Transparent, Focusable = true };
            row.Children.Add(_chevron);
            Grid.SetColumn(header, 1);
            row.Children.Add(header);
            if (collapsedSummary != null)
            {
                // The host is what hides the summary on expand; the summary keeps its own binding-driven visibility.
                _summaryHost = new Panel { Children = { collapsedSummary }, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(_summaryHost, 2);
                row.Children.Add(_summaryHost);
            }
            row.PointerPressed += (_, e) =>
            {
                IsExpanded = !IsExpanded;
                e.Handled = true;
            };
            row.KeyDown += (_, e) =>
            {
                if (e.Key is Key.Enter or Key.Space)
                {
                    IsExpanded = !IsExpanded;
                    e.Handled = true;
                }
            };
            row.Cursor = new Cursor(StandardCursorType.Hand);
            AutomationProperties.SetName(row, automationName);
            Children.Add(row);
            Children.Add(content);
        }

        /// <summary>Whether the content shows.</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                _content.IsVisible = value;
                if (_summaryHost != null)
                {
                    _summaryHost.IsVisible = !value;
                }
                _chevron.Icon = value ? Icon.ChevronDown : Icon.ChevronRight;
            }
        }
    }

    private static Style CreateTreeItemStyle()
    {
        Style style = new Style(selector => selector.OfType<TreeViewItem>());
        style.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, new Binding(nameof(NewProjectLayoutNode.IsExpanded)) { Mode = BindingMode.TwoWay }));
        style.Setters.Add(new Setter(Layoutable.MinHeightProperty, 0d));
        style.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
        return style;
    }

    private static Control CreateTreeRow(NewProjectLayoutNode node)
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 1) };
        TextBlock name = new TextBlock { Text = node.DisplayName, VerticalAlignment = VerticalAlignment.Center };
        if (node.IsMainFile)
        {
            name.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Success");
        }
        row.Children.Add(name);
        return row;
    }

    // The amber banner of the mock: a tinted fill, an amber outline with a thicker left edge, and an icon.
    private static Control CreateBanner(Control text, string brushKey = "Frb.Brushes.Icon.Manilla")
    {
        FluentIcon icon = CreateIcon(Icon.Warning, 1.4);
        icon.VerticalAlignment = VerticalAlignment.Top;
        icon.WithThemeResource(TemplatedControl.ForegroundProperty, brushKey);

        Grid content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8, Margin = new Thickness(10, 8) };
        content.Children.Add(icon);
        Grid.SetColumn(text, 1);
        content.Children.Add(text);

        Border tint = new Border { Opacity = 0.14, CornerRadius = new CornerRadius(3) };
        tint.WithThemeResource(Border.BackgroundProperty, brushKey);
        Border outline = new Border { CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(4, 1, 1, 1) };
        outline.WithThemeResource(Border.BorderBrushProperty, brushKey);

        Grid banner = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        banner.Children.Add(tint);
        banner.Children.Add(outline);
        banner.Children.Add(content);
        return banner;
    }

    private static FluentIcon CreateIcon(Icon icon, double fontSizeFactor)
    {
        FluentIcon result = new FluentIcon { Icon = icon, VerticalAlignment = VerticalAlignment.Center };
        // Sized from the base font so the icon follows the tool's zoom, like the text beside it.
        IObservable<object?> size = result.GetResourceObservable(FontSizeKey, value => (object)((value is double baseSize ? baseSize : 12d) * fontSizeFactor));
        result.Bind(FluentIcon.FontSizeProperty, size);
        result.Bind(WidthProperty, size);
        result.Bind(HeightProperty, size);
        return result;
    }

    private static Control Divider(Thickness? margin = null)
    {
        Border line = new Border { Height = 1, Margin = margin ?? new Thickness(0, 12, 0, 4) };
        line.WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Border.Secondary");
        return line;
    }

    private static Control FieldLabel(string text, string tip, bool alignRight = false)
    {
        Control label = LabelWithTip(text, tip, alignRight);
        label.Margin = new Thickness(0, 9, 0, 3);
        return label;
    }

    // A label followed by an (i) icon whose hover tip explains the field; the icon takes keyboard focus too.
    private static StackPanel LabelWithTip(string text, string tip, bool alignRight = false, double maxTextWidth = double.PositiveInfinity)
    {
        StackPanel label = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        label.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = maxTextWidth });
        label.Children.Add(CreateInfoIcon(tip, alignRight, subject: text));
        return label;
    }

    private static FluentIcon CreateInfoIcon(string? tip = null, bool alignRight = false, Binding? tipBinding = null, string subject = "")
    {
        FluentIcon info = CreateIcon(Icon.Info, 1.1);
        AutomationProperties.SetName(info, "About " + subject);
        info.Cursor = new Cursor(StandardCursorType.Help);
        info.Focusable = true;
        info.Opacity = 0.7;
        TextBlock tipText = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 300 };
        if (tipBinding != null)
        {
            tipText.Bind(TextBlock.TextProperty, tipBinding);
            info.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
            info.Opacity = 1;
        }
        else
        {
            tipText.Text = tip;
        }
        Border tipBox = new Border { Padding = new Thickness(8, 6), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), Child = tipText };
        tipBox.WithThemeResource(Border.BackgroundProperty, "Frb.Surface01");
        tipBox.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Border");
        ToolTip.SetTip(info, tipBox);
        ToolTip.SetShowDelay(info, 0);
        ToolTip.SetPlacement(info, alignRight ? PlacementMode.BottomEdgeAlignedRight : PlacementMode.BottomEdgeAlignedLeft);
        return info;
    }

    private static TextBlock HelpText(string text) =>
        new TextBlock { Text = text, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) };

    private static T Bound<T>(T control, AvaloniaProperty property, string path) where T : Control
    {
        control.Bind(property, new Binding(path));
        return control;
    }

    private static Button CommandButton(string text, string commandPath)
    {
        Button button = new Button { Content = text };
        button.Bind(Button.CommandProperty, new Binding(commandPath));
        return button;
    }

    private static HyperlinkButton Link(string text, Action onClick)
    {
        HyperlinkButton link = new HyperlinkButton { Content = text, Padding = new Thickness(4, 0) };
        link.Click += (_, _) => onClick();
        return link;
    }

    #endregion

    private void HandleCopyRequested(string text) => _ = CopyAsync(text);

    private async Task CopyAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }

        // A check in the success color confirms the copy for a moment.
        _copyIcon.Icon = Icon.Checkmark;
        _copyIcon.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Success");
        _copiedTimer.Stop();
        _copiedTimer.Interval = CopiedCheckDuration;
        _copiedTimer.Start();
    }
}
