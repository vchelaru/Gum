using Avalonia.Controls;
using AvaloniaDataUi;
using Avalonia;
using Avalonia.VisualTree;
using CodeOutputPlugin;
using Avalonia.Reactive;
using Avalonia.Threading;
using System.Linq;
using System;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Code generation page of the Settings window: a property grid over the project-wide rows of
/// its <see cref="CodeGenerationSettingsViewModel"/>, rebuilt when a setting changes which rows exist.
/// </summary>
public sealed class CodeGenerationSettingsPageView : DockPanel
{
    private const double DefaultFontSize = 12;
    private double _fontSize = DefaultFontSize;
    private CodeGenerationSettingsViewModel? _viewModel;
    private bool _isListening;

    /// <summary>Builds the view.</summary>
    public CodeGenerationSettingsPageView()
    {
        Grid = new DataUiGrid { HideLoneCategoryHeader = true, AlternatesRowBackgrounds = false };
        Children.Add(Grid);

        DataContextChanged += (_, _) => HandleDataContextChanged();

        // The label column is sized for the default font; a larger font (zoom) widens it in proportion
        // so labels stay on one line.
        this.GetResourceObservable("ControlContentThemeFontSize").Subscribe(new AnonymousObserver<object?>(size =>
        {
            _fontSize = size is double fontSize && fontSize > 0 ? fontSize : DefaultFontSize;
            ScaleLabelColumns();
        }));
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }

    // The view model lives as long as the plugin, while a view lives as long as its Settings window,
    // so the view listens only while it is on screen. A listener left behind by a closed window
    // would keep rebuilding that window's rows on every later change.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Listen();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopListening();
    }

    private void HandleDataContextChanged()
    {
        StopListening();
        _viewModel = DataContext as CodeGenerationSettingsViewModel;
        if (this.IsAttachedToVisualTree())
        {
            Listen();
            Rebuild();
        }
    }

    private void Listen()
    {
        if (_viewModel != null && !_isListening)
        {
            _viewModel.Reloaded += Rebuild;
            _isListening = true;
        }
    }

    private void StopListening()
    {
        if (_viewModel != null && _isListening)
        {
            _viewModel.Reloaded -= Rebuild;
            _isListening = false;
        }
    }

    private void Rebuild()
    {
        if (_viewModel != null)
        {
            Grid.SetCategories(_viewModel.BuildCategories());
            ScaleLabelColumns();
            Dispatcher.UIThread.Post(SpaceSectionBars);
        }
    }

    // The section bars sit closer together in the shared grid; on this page each one starts a new group.
    private void SpaceSectionBars()
    {
        bool isFirst = true;
        foreach (DataUiCategoryView view in Grid.GetVisualDescendants().OfType<DataUiCategoryView>())
        {
            view.Margin = new Thickness(0, isFirst ? 4 : 14, 0, 2);
            isFirst = false;
        }
    }

    private void ScaleLabelColumns()
    {
        double width = Math.Round(CodeOutputSettingsMembers.ProjectLabelColumnWidth * _fontSize / DefaultFontSize);
        foreach (MemberCategory category in Grid.Categories)
        {
            foreach (InstanceMember member in category.Members)
            {
                member.FirstGridLength = width;
            }
        }
    }
}
