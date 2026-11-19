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

    /// <summary>
    /// <see cref="GetProjectErrors"/> in halves: read the project in steps on the UI thread, check the disk
    /// elsewhere, then turn the rows into view models with <see cref="ToViewModels"/> back on the UI thread.
    /// </summary>
    Gum.ProjectServices.ProjectErrorCheck BeginProjectErrors(GumProjectSave project);

    /// <summary>The rows a <see cref="Gum.ProjectServices.ProjectErrorCheck"/> found, as <see cref="GetProjectErrors"/> returns them.</summary>
    ErrorViewModel[] ToViewModels(System.Collections.Generic.IReadOnlyList<Gum.ProjectServices.ErrorResult> rows);
}
