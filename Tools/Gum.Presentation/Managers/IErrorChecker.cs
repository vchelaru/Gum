using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using System;

namespace Gum.Managers;

public interface IErrorChecker
{
    /// <summary>
    /// Raised after <see cref="GetErrorsFor(ElementSave?, GumProjectSave)"/> checks an element, with
    /// every error it found. Lets a view show the latest result without running its own check.
    /// </summary>
    event Action<ElementSave, ErrorViewModel[]>? ErrorsChecked;

    ErrorViewModel[] GetErrorsFor(ElementSave? element, GumProjectSave project);
    ErrorViewModel[] GetErrorsFor(ElementSave? element, PluginBase plugin);
}
