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

    /// <summary>
    /// The rows plugins contribute through <see cref="PluginBase.GetAllErrors"/>, from every plugin
    /// or only <paramref name="plugin"/>. They are project-wide (the orphaned code files), so they
    /// are listed whatever is selected and are not part of an element's check.
    /// </summary>
    ErrorViewModel[] GetPluginErrors(PluginBase? plugin = null);

    /// <summary>
    /// The errors that belong to the project rather than an element (see
    /// <see cref="Gum.ProjectServices.IHeadlessErrorChecker.GetProjectErrors"/>). Walks the whole
    /// project; does not raise <see cref="ErrorsChecked"/>.
    /// </summary>
    ErrorViewModel[] GetProjectErrors(GumProjectSave project);
}
