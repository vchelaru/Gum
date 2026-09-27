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

    /// <summary>
    /// The errors that belong to the project rather than an element (see
    /// <see cref="Gum.ProjectServices.IHeadlessErrorChecker.GetProjectErrors"/>). Walks the whole
    /// project; does not raise <see cref="ErrorsChecked"/>.
    /// </summary>
    ErrorViewModel[] GetProjectErrors(GumProjectSave project);
}
