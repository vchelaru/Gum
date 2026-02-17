using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public class CategorySortAndColorLogic
{
    record CategoryColor
    {
        public string Name;
        public string Color;

    }


    const string alphaHex = "20";

    List<CategoryColor> OrderedCategories = new List<CategoryColor>
    {
        new CategoryColor { Name = "General", Color = $"#{alphaHex}4300FF" },
        new CategoryColor { Name = "Position", Color = $"#{alphaHex}0000FF" },
        new CategoryColor { Name = "Dimensions", Color = $"#{alphaHex}0090FF" },
        new CategoryColor { Name = "Text", Color = $"#{alphaHex}00F6FF" },
        new CategoryColor { Name = "Font", Color = $"#{alphaHex}00FF1D" },
        new CategoryColor { Name = "Source", Color = $"#{alphaHex}BBFF00" },
        new CategoryColor { Name = "Animation", Color = $"#{alphaHex}FF9D00" },
        new CategoryColor { Name = "Flip and Rotation", Color = $"#{alphaHex}FF0C00" },
        new CategoryColor { Name = "States and Visibility", Color = $"#{alphaHex}FF00A5" },
        new CategoryColor { Name = "Parent", Color = $"#{alphaHex}CB00FF" },
        new CategoryColor { Name = "Children", Color = $"#{alphaHex}3F00FF" },
        new CategoryColor { Name = "Rendering", Color = $"#{alphaHex}004CFF" },
        new CategoryColor { Name = "Dropshadow", Color = $"#{alphaHex}00B6FF" },
        new CategoryColor { Name = "Stroke and Fill", Color = $"#{alphaHex}00FF83" },
        new CategoryColor { Name = "Behavior", Color = $"#{alphaHex}2EFF00" },
    };

    public List<MemberCategory> SortAndColorCategories(List<MemberCategory> categories)
    {
        int GetDesiredIndex(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "General")
            {
                return -1;
            }
            else
            {
                var foundItem = OrderedCategories.FirstOrDefault(item => item.Name == category);
                if (foundItem != default)
                {
                    return OrderedCategories.IndexOf(foundItem);
                }
            }

            var itemByCategory = categories.FirstOrDefault(item => item.Name == category);

            var index = categories.IndexOf(itemByCategory);

            return OrderedCategories.Count + index;
        }

        categories = categories.OrderBy(item => GetDesiredIndex(item.Name))
            .ToList();

        foreach (var category in categories)
        {
            var foundColor = OrderedCategories.FirstOrDefault(item => item.Name == category.Name);
            if (foundColor != default)
            {
                category.HeaderColor =
                    (System.Windows.Media.Brush)(new System.Windows.Media.BrushConverter().ConvertFrom(foundColor.Color)!);
            }
        }

        return categories;
    }
}
