using Gum.DataTypes;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;
using TextureCoordinateSelectionPlugin.Models;

namespace TextureCoordinateSelectionPlugin.Logic;

internal class ExposedTextureCoordinateLogic
{
    private readonly IObjectFinder _objectFinder;

    public ExposedTextureCoordinateLogic(IObjectFinder objectFinder)
    {
        _objectFinder = objectFinder;
    }

    public bool IsDirectSpriteOrNineSlice(ElementSave element)
    {
        if (element is StandardElementSave)
        {
            return element.Name == "Sprite" || element.Name == "NineSlice";
        }

        var baseElements = _objectFinder.GetBaseElements(element);
        return baseElements.Any(b =>
            b is StandardElementSave bs && (bs.Name == "Sprite" || bs.Name == "NineSlice"));
    }

    public List<ExposedTextureCoordinateSet> GetExposedSets(ElementSave element)
    {
        var state = element.DefaultState;
        var sourceSets = new Dictionary<string, ExposedTextureCoordinateSet>();

        foreach (var variable in state.Variables)
        {
            if (string.IsNullOrEmpty(variable.ExposedAsName)) continue;
            if (string.IsNullOrEmpty(variable.SourceObject)) continue;

            var rootName = variable.GetRootName();

            bool isTextureCoordinate = rootName == "TextureLeft" || rootName == "TextureTop" ||
                                       rootName == "TextureWidth" || rootName == "TextureHeight";

            if (!isTextureCoordinate) continue;

            var sourceObjectName = variable.SourceObject!;

            if (!sourceSets.ContainsKey(sourceObjectName))
            {
                var instance = element.Instances.FirstOrDefault(i => i.Name == sourceObjectName);
                if (instance == null) continue;

                var instanceElement = _objectFinder.GetElementSave(instance);
                bool isSpriteOrNineSlice = false;
                if (instanceElement is StandardElementSave ses)
                {
                    isSpriteOrNineSlice = ses.Name == "Sprite" || ses.Name == "NineSlice";
                }
                else if (instanceElement != null)
                {
                    var innerBaseElements = _objectFinder.GetBaseElements(instanceElement);
                    isSpriteOrNineSlice = innerBaseElements.Any(b =>
                        b is StandardElementSave bs && (bs.Name == "Sprite" || bs.Name == "NineSlice"));
                }

                if (!isSpriteOrNineSlice) continue;

                sourceSets[sourceObjectName] = new ExposedTextureCoordinateSet
                {
                    SourceObjectName = sourceObjectName
                };
            }

            var set = sourceSets[sourceObjectName];
            switch (rootName)
            {
                case "TextureLeft":
                    set.ExposedLeftName = variable.ExposedAsName;
                    break;
                case "TextureTop":
                    set.ExposedTopName = variable.ExposedAsName;
                    break;
                case "TextureWidth":
                    set.ExposedWidthName = variable.ExposedAsName;
                    break;
                case "TextureHeight":
                    set.ExposedHeightName = variable.ExposedAsName;
                    break;
            }
        }

        return sourceSets.Values.ToList();
    }
}
