using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Gum.Menus;

namespace Gum.Managers
{
    /// <summary>
    /// Renders the shared <see cref="MenuModel"/> built by <see cref="StandardMenuModelBuilder"/>
    /// into the WPF main menu and keeps the two in sync: every model item maps to exactly one
    /// <see cref="MenuItem"/> for its lifetime, so plugins that hold the returned item keep a live
    /// handle across layout passes; header, enabled, and check state follow the model's property
    /// changes; and an item's children are rebuilt when the model collection changes.
    /// </summary>
    public class MenuStripManager
    {
        private readonly StandardMenuModelBuilder _builder;
        private readonly Dictionary<MenuItemModel, MenuItem> _itemsByModel = new Dictionary<MenuItemModel, MenuItem>();

        private Menu? _menu;
        private NotifyCollectionChangedEventHandler? _topLevelHandler;

        public MenuStripManager(StandardMenuModelBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>The model this menu renders. Plugins may add and remove items through it.</summary>
        public MenuModel Model => _builder.Model;

        /// <summary>Builds the standard menus into the model and renders them into <paramref name="menu"/>.</summary>
        public void PopulateMenu(Menu menu)
        {
            if (_topLevelHandler != null)
            {
                Model.TopLevelItems.CollectionChanged -= _topLevelHandler;
            }
            _itemsByModel.Clear();
            _menu = menu;

            _builder.Build();

            Populate(_menu.Items, Model.TopLevelItems);
            _topLevelHandler = (_, _) => Populate(_menu.Items, Model.TopLevelItems);
            Model.TopLevelItems.CollectionChanged += _topLevelHandler;
        }

        /// <summary>Syncs the selection-dependent headers, enabled flags, and check marks.</summary>
        public void RefreshUI() => _builder.RefreshUI();

        /// <summary>The WPF item rendered for <paramref name="model"/>. The model must be in the menu.</summary>
        public MenuItem GetMenuItem(MenuItemModel model)
        {
            if (!_itemsByModel.TryGetValue(model, out MenuItem? menuItem))
            {
                throw new InvalidOperationException($"'{model.Header}' is not in the rendered menu.");
            }
            return menuItem;
        }

        private void Populate(ItemCollection target, ObservableCollection<MenuItemModel> items)
        {
            target.Clear();
            foreach (MenuItemModel item in items)
            {
                target.Add(item.IsSeparator ? new Separator() : GetOrCreate(item));
            }
        }

        private MenuItem GetOrCreate(MenuItemModel model)
        {
            if (_itemsByModel.TryGetValue(model, out MenuItem? existing))
            {
                return existing;
            }

            MenuItem menuItem = new MenuItem
            {
                Header = model.Header,
                IsEnabled = model.IsEnabled,
                // The model toggles its own check state in Invoke; WPF must not toggle it as well.
                IsCheckable = false,
                IsChecked = model.IsChecked,
                InputGestureText = model.InputGestureText,
                ToolTip = model.ToolTip,
            };

            menuItem.Click += (_, e) =>
            {
                // Click bubbles from children through their parent items; only the picked item
                // invokes, and a submenu header's click just opens it.
                if (ReferenceEquals(e.Source, menuItem) && model.Items.Count == 0)
                {
                    model.Invoke();
                }
            };

            model.PropertyChanged += (_, e) => Apply(menuItem, model, e);
            Populate(menuItem.Items, model.Items);
            model.Items.CollectionChanged += (_, _) => Populate(menuItem.Items, model.Items);

            _itemsByModel[model] = menuItem;
            return menuItem;
        }

        private static void Apply(MenuItem menuItem, MenuItemModel model, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MenuItemModel.Header):
                    menuItem.Header = model.Header;
                    break;
                case nameof(MenuItemModel.IsEnabled):
                    menuItem.IsEnabled = model.IsEnabled;
                    break;
                case nameof(MenuItemModel.IsChecked):
                    menuItem.IsChecked = model.IsChecked;
                    break;
                case nameof(MenuItemModel.InputGestureText):
                    menuItem.InputGestureText = model.InputGestureText;
                    break;
                case nameof(MenuItemModel.ToolTip):
                    menuItem.ToolTip = model.ToolTip;
                    break;
            }
        }
    }
}
