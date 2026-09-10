using Gum.DataTypes;
using Gum.Plugins.BaseClasses;

namespace Gum.Managers;

public interface IErrorChecker
{
    ErrorViewModel[] GetErrorsFor(ElementSave? element, GumProjectSave project);
    ErrorViewModel[] GetErrorsFor(ElementSave? element, PluginBase plugin);
}
