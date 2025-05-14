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

namespace Gum.Services;

public class Builder
{
    public static IHost App { get; private set; }

    public static T Get<T>() => App.Services.GetRequiredService<T>();

    public void Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(typeof(ElementCommands), ElementCommands.Self);
        builder.Services.AddSingleton(typeof(ISelectedState), SelectedState.Self);
        builder.Services.AddSingleton(typeof(Commands.GuiCommands), GumCommands.Self.GuiCommands);
        builder.Services.AddSingleton(typeof(UndoManager), UndoManager.Self);
        builder.Services.AddSingleton(typeof(FileCommands), GumCommands.Self.FileCommands);
        builder.Services.AddSingleton(typeof(GuiCommands), GumCommands.Self.GuiCommands);
        builder.Services.AddSingleton(typeof(NameVerifier), NameVerifier.Self);
        builder.Services.AddSingleton(typeof(SetVariableLogic), SetVariableLogic.Self);
        builder.Services.AddSingleton(typeof(HotkeyManager), HotkeyManager.Self);
        builder.Services.AddSingleton(typeof(RenameLogic));
        builder.Services.AddSingleton(typeof(LocalizationManager));
        builder.Services.AddSingleton(typeof(FontManager));
        builder.Services.AddSingleton<IEditVariableService, EditVariableService>();
        builder.Services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        builder.Services.AddSingleton<IDeleteVariableService, DeleteVariableService>();


        builder.Services.AddTransient<AddVariableViewModel>();

        App = builder.Build();

        // This is needed until we unroll all the singletons...
        SetVariableLogic.Self.Initialize();
    }
}
