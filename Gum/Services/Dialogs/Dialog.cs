﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Xceed.Wpf.AvalonDock.Controls;

namespace Gum.Services.Dialogs;

public class Dialog : ContentControl
{
    public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.RegisterAttached(
        "DialogTitle", typeof(string), typeof(Dialog), new PropertyMetadata(default(string?)));

    public static void SetDialogTitle(DependencyObject element, string? value)
    {
        element.SetValue(DialogTitleProperty, value);
    }

    public static string? GetDialogTitle(DependencyObject element)
    {
        return (string?) element.GetValue(DialogTitleProperty);
    }


    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.RegisterAttached("Actions", typeof(object), typeof(Dialog), new PropertyMetadata(null));

    public static object? GetActions(DependencyObject obj)
    {
        return obj.GetValue(ActionsProperty);
    }

    public static void SetActions(DependencyObject obj, object value)
    {
        obj.SetValue(ActionsProperty, value);
    }
    
    public Dialog()
    {
        ContentTemplateSelector = new DialogTemplateSelector();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            UserControl? userControl = this.VisitDescendentsBfs().OfType<UserControl>().FirstOrDefault();
            if (userControl != null)
            {
                Bind(DialogTitleProperty);
                Bind(ActionsProperty);
            }

            void Bind(DependencyProperty source)
            {
                SetBinding(source, new Binding()
                {
                    Path = new PropertyPath("(0)", source),
                    Source = userControl,
                });
            }
        }, DispatcherPriority.Loaded);
    }
    
    private class DialogTemplateSelector : DataTemplateSelector
    {
        private static IDialogViewResolver DialogViewResolver { get; } = Locator.GetRequiredService<IDialogViewResolver>();
        private static Dictionary<Type, DataTemplate> ResolvedTemplates { get; } = [];

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item?.GetType() is not { } itemType)
            {
                return base.SelectTemplate(item, container);
            }
            
            if (ResolvedTemplates.TryGetValue(itemType, out var template))
            {
                return template;
            }
            
            if (DialogViewResolver.GetDialogViewType(itemType) is { } viewType)
            {
                DataTemplate newTemplate = new(itemType)
                {
                    VisualTree = new(viewType)
                };
                ResolvedTemplates.Add(itemType, newTemplate);
                return newTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}

file static class VisualTreeHelpers
{
    public static IEnumerable<DependencyObject> VisitDescendentsBfs(this DependencyObject root, bool includeRoot = false)
    {
        if (includeRoot)
            yield return root;

        Queue<DependencyObject> queue = new();
        queue.Enqueue(root);
        do
        {
            DependencyObject current = queue.Dequeue();
            foreach (DependencyObject child in current.GetChildObjects(true))
            {
                yield return current;
                queue.Enqueue(child);
            }
        } while (queue.Count > 0);
    }
    
    private static IEnumerable<DependencyObject> GetChildObjects(this DependencyObject? parent, bool forceUsingTheVisualTreeHelper = false)
    {
        if (parent is not null)
        {
            if (!forceUsingTheVisualTreeHelper && parent is ContentElement or FrameworkElement)
            {
                // use the logical tree for content / framework elements
                foreach (var obj in LogicalTreeHelper.GetChildren(parent))
                {
                    if (obj is DependencyObject dependencyObject)
                    {
                        yield return dependencyObject;
                    }
                }
            }
            else if (parent is Visual or Visual3D)
            {
                // use the visual tree per default
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    yield return VisualTreeHelper.GetChild(parent, i);
                }
            }
        }
    }
}