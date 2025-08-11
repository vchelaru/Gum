using Gum.Managers;
using Gum.Plugins;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Gum.Controls;

/// <summary>
/// Interaction logic for MainPanelControl.xaml
/// </summary>
public partial class MainPanelControl : UserControl
{
    public static readonly DependencyProperty IsToolsVisibleProperty = DependencyProperty.Register(
        nameof(IsToolsVisible), typeof(bool), typeof(MainPanelControl), new PropertyMetadata(true, static (o, args) =>
        {
            MainPanelControl mainPanelControl = (MainPanelControl)o;
            if (args.NewValue is true)
            {
                mainPanelControl.ShowTools();
            }
            else
            {
                mainPanelControl.HideTools();
            }
        }));

    public bool IsToolsVisible
    {
        get { return (bool)GetValue(IsToolsVisibleProperty); }
        set { SetValue(IsToolsVisibleProperty, value); }
    }
    
    GridLength expandedLeftColumnLength;
    GridLength expandedMiddleColumnLength;
    GridLength bottomRowLength;

    GridLength splitterLength;
    
    private readonly HotkeyManager _hotkeyManager;
    
    bool isHidden;
    public MainPanelControl(MainPanelViewModel viewModel, HotkeyManager hotkeyManager)
    {
        InitializeComponent();
        DataContext = viewModel;
        SetBinding(IsToolsVisibleProperty, new Binding(nameof(viewModel.IsToolsVisible)));
        _hotkeyManager = hotkeyManager;
        this.KeyDown += HandleKeyDown;
    }

    private void HandleKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        _hotkeyManager.HandleKeyDownAppWide(e);
    }
    
    private void HideTools()
    {
        if(!isHidden)
        {
            expandedLeftColumnLength = LeftColumn.Width;
            expandedMiddleColumnLength = MiddleColumn.Width;
            bottomRowLength = BottomRightPanel.Height;

            splitterLength = LeftSplitter.Width;


            LeftColumn.Width = new GridLength(0);
            MiddleColumn.Width = new GridLength(0);
            LeftSplitter.Width = new GridLength(0);
            MiddleSplitter.Width = new GridLength(0);

            BottomRightSplitter.Height = new GridLength(0);
            BottomRightPanel.Height = new GridLength(0);

            isHidden = true;
        }
    }

    private void ShowTools()
    {
        if(isHidden)
        {
            LeftColumn.Width = expandedLeftColumnLength;
            MiddleColumn.Width = expandedMiddleColumnLength;
            LeftSplitter.Width = splitterLength;
            MiddleSplitter.Width = splitterLength;


            BottomRightSplitter.Height = splitterLength;
            BottomRightPanel.Height = bottomRowLength;

            isHidden = false;
        }
    }

    private void TabHeader_OnMiddleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PluginTab pluginTab } &&
            pluginTab.HideCommand.CanExecute(null))
        {
            pluginTab.HideCommand.Execute(null);
        }
    }
}

public class PluginTabHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CustomHeaderTemplate { get; set; }
    public DataTemplate? TitleHeaderTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item != null ? CustomHeaderTemplate! : TitleHeaderTemplate!;
    }
}