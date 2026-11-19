using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Shell;
using Gum.Input;
using Gum.Menus;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class MenuSyncRandomizedTests
{
    [AvaloniaFact]
    public void RandomCollectionChanges_MatchAFreshBuild_InBothBuilders()
    {
        Random random = new Random(8271);
        MenuStepper zoom = new MenuStepper("Zoom", "Zoom In", "Zoom Out", "Actual Size", () => { }, () => { }, () => { });
        List<MenuItemModel> parents = Enumerable.Range(0, 8).Select(i => new MenuItemModel("P" + i)).ToList();
        List<MenuItemModel> leaves = Enumerable.Range(0, 10).Select(i => new MenuItemModel("L" + i) { IsCheckable = i % 3 == 0 }).ToList();
        leaves.Add(new MenuItemModel("Zoom") { Stepper = zoom });
        MenuModel model = new MenuModel();
        Menu menu = AvaloniaMenuBuilder.Build(model);
        NativeMenu native = AvaloniaNativeMenuBuilder.Build(model);

        for (int step = 0; step < 300; step++)
        {
            bool top = random.Next(3) == 0;
            ObservableCollection<MenuItemModel> target = top ? model.TopLevelItems : parents[random.Next(parents.Count)].Items;
            MenuItemModel Pick() => top ? parents[random.Next(parents.Count)]
                : random.Next(6) == 0 ? MenuItemModel.Separator() : random.Next(5) == 0 ? leaves[^1] : leaves[random.Next(leaves.Count)];
            int op = random.Next(10);
            int n = target.Count;
            if (op <= 3 || n == 0) target.Insert(random.Next(n + 1), Pick());
            else if (op == 4) target.RemoveAt(random.Next(n));
            else if (op == 5) target.Move(random.Next(n), random.Next(n));
            else if (op == 6) target[random.Next(n)] = Pick();
            else if (op == 7 && random.Next(4) == 0) target.Clear();
            else
            {
                MenuItemModel m = random.Next(2) == 0 ? parents[random.Next(parents.Count)] : leaves[random.Next(leaves.Count - 1)];
                switch (random.Next(3))
                {
                    case 0: m.Header = m.Header.Split('*')[0] + "*" + step; break;
                    case 1: m.IsEnabled = !m.IsEnabled; break;
                    default: m.IsChecked = !m.IsChecked; break;
                }
            }

            if (step % 10 == 9 || step == 299)
            {
                Describe(menu.Items).ShouldBe(Describe(AvaloniaMenuBuilder.Build(model).Items), $"in-window, step {step}");
                Describe(native).ShouldBe(Describe(AvaloniaNativeMenuBuilder.Build(model)), $"native, step {step}");
            }
        }
    }

    private static string Describe(ItemCollection items)
    {
        StringBuilder sb = new StringBuilder("[");
        foreach (object? item in items)
        {
            if (item is Separator) { sb.Append("-,"); continue; }
            MenuItem mi = (MenuItem)item!;
            string header = mi.Header is MenuStepperRow row ? "Stepper:" + row.ValueButton.Content : (string)mi.Header!;
            sb.Append($"{header}|{mi.IsEnabled}|{mi.ToggleType}|{mi.IsChecked}|{mi.InputGesture}{Describe(mi.Items)},");
        }
        return sb.Append(']').ToString();
    }

    private static string Describe(NativeMenu? menu)
    {
        if (menu == null || menu.Items.Count == 0) return "[]";
        StringBuilder sb = new StringBuilder("[");
        foreach (NativeMenuItemBase item in menu.Items)
        {
            if (item is NativeMenuItemSeparator) { sb.Append("-,"); continue; }
            NativeMenuItem mi = (NativeMenuItem)item;
            sb.Append($"{mi.Header}|{mi.IsEnabled}|{mi.ToggleType}|{mi.IsChecked}|{mi.Gesture}{Describe(mi.Menu)},");
        }
        return sb.Append(']').ToString();
    }
}
