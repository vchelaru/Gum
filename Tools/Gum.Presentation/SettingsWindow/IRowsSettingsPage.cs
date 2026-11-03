using System;
using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The content of a Settings page built as rows (see <see cref="SettingsRows"/>): the head's rows page
/// view shows <see cref="BuildCategories"/> and rebuilds when <see cref="Reloaded"/> is raised.
/// </summary>
public interface IRowsSettingsPage
{
    /// <summary>Raised when the view should rebuild its rows from <see cref="BuildCategories"/>.</summary>
    event Action? Reloaded;

    /// <summary>The sections of the page, each a category of rows.</summary>
    List<MemberCategory> BuildCategories();
}
