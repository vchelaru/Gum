using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Logic.FileWatch;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Wireframe;

namespace Gum.Services;

public class Builder
{
    [Obsolete("Use Locator")]
    public static T Get<T>() => Locator.GetRequiredService<T>();
}
