using System;
using System.Windows;
using System.Windows.Controls;
using CodeOutputPlugin.ViewModels;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using WpfDataUi.DataTypes;

namespace CodeOutputPlugin.Views;

/// <summary>
/// The WPF Code tab: the generated-code preview and the settings grid, whose rows come from the
/// shared <see cref="CodeOutputSettingsMembers"/>.
/// </summary>
public partial class CodeWindow : UserControl, ICodeOutputTabHost
{
    private readonly CodeOutputSettingsMembers _settingsMembers;

    public CodeWindow(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers)
    {
        _settingsMembers = settingsMembers;

        InitializeComponent();

        DataContext = viewModel;

        DataGrid.PropertyChange += (_, _) => CodeOutputSettingsPropertyChanged?.Invoke(this, EventArgs.Empty);
        _settingsMembers.SettingsChanged += (_, _) => CodeOutputSettingsPropertyChanged?.Invoke(this, EventArgs.Empty);
        _settingsMembers.RebuildRequested += (_, _) => FullRefreshDataGrid();

        FullRefreshDataGrid();
    }

    #region Properties

    /// <inheritdoc/>
    public object Control => this;

    public CodeOutputProjectSettings? CodeOutputProjectSettings
    {
        get => _settingsMembers.ProjectSettings;
        set
        {
            // Setter triggers FullRefreshDataGrid because NeedsSetup (which
            // toggles between the Auto/Manual prompt and the populated settings
            // grid) is recomputed during the refresh. The plugin wires this
            // property up after LoadCodeSettingsFile has already fired a
            // refresh, so without this we'd evaluate NeedsSetup against a
            // stale (null) CodeOutputProjectSettings and leave the prompt
            // showing even when CodeProjectRoot is populated (#2875).
            if (_settingsMembers.ProjectSettings == value)
            {
                return;
            }
            _settingsMembers.ProjectSettings = value;
            FullRefreshDataGrid();
        }
    }

    public CodeOutputElementSettings? CodeOutputElementSettings
    {
        get => _settingsMembers.ElementSettings;
        set
        {
            System.Diagnostics.Debug.Assert(value != null, "CodeOutputElementSettings should not be set to null when setting the property grid's instance");
            _settingsMembers.ElementSettings = value;
            DataGrid.Instance = value;

            FullRefreshDataGrid();
        }
    }

    #endregion

    #region Events

    public event EventHandler? CodeOutputSettingsPropertyChanged;
    public event EventHandler? GenerateCodeClicked;
    public event EventHandler? GenerateAllCodeClicked;

    #endregion

    private void FullRefreshDataGrid()
    {
        DataGrid.Categories.Clear();

        foreach (MemberCategory category in _settingsMembers.BuildCategories())
        {
            DataGrid.Categories.Add(category);
        }
    }

    #region Button Event Handlers

    private void HandleGenerateCodeClicked(object? sender, RoutedEventArgs e) =>
        GenerateCodeClicked?.Invoke(this, EventArgs.Empty);

    // The Generate All button is commented out in the XAML for now.
    private void HandleGenerateAllCodeClicked(object? sender, RoutedEventArgs e) =>
        GenerateAllCodeClicked?.Invoke(this, EventArgs.Empty);

    private void HandleAutoSetupClicked(object? sender, RoutedEventArgs e) => _settingsMembers.ApplyAutoSetup();

    private void HandleManualSetupClicked(object? sender, RoutedEventArgs e) => _settingsMembers.ChooseManualSetup();

    #endregion
}
