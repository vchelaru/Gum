using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public interface IStandardElementsManagerGumTool
{
    void Initialize();
    void FixCustomTypeConverters(GumProjectSave project);
    void FixCustomTypeConverters(ElementSave elementSave);
    void RefreshStateVariablesThroughPlugins();
    void SetPreferredDisplayers(StateSave state);
}
