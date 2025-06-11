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
using GumCommon;

namespace Gum.Services;

public class Builder
{
    public void Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(typeof(CircularReferenceManager));
        builder.Services.AddSingleton(typeof(ElementCommands), ElementCommands.Self);
        builder.Services.AddSingleton(typeof(Commands.GuiCommands), GumCommands.Self.GuiCommands);
        builder.Services.AddSingleton(typeof(FileCommands), GumCommands.Self.FileCommands);
        builder.Services.AddSingleton(typeof(UndoManager), UndoManager.Self);
        builder.Services.AddSingleton(typeof(FileCommands), GumCommands.Self.FileCommands);
        builder.Services.AddSingleton(typeof(GuiCommands), GumCommands.Self.GuiCommands);
        builder.Services.AddSingleton(typeof(NameVerifier), NameVerifier.Self);
        builder.Services.AddSingleton(typeof(SetVariableLogic), SetVariableLogic.Self);
        builder.Services.AddSingleton(typeof(HotkeyManager), HotkeyManager.Self);
        builder.Services.AddSingleton(typeof(RenameLogic));
        builder.Services.AddSingleton(typeof(LocalizationManager));
        builder.Services.AddSingleton(typeof(FontManager));
        builder.Services.AddSingleton(typeof(DragDropManager));
        builder.Services.AddSingleton<ISelectedState, SelectedState>();
        builder.Services.AddSingleton<IObjectFinder>(ObjectFinder.Self);

        builder.Services.AddSingleton<IEditVariableService, EditVariableService>();
        builder.Services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        builder.Services.AddSingleton<IDeleteVariableService, DeleteVariableService>();


        builder.Services.AddTransient<AddVariableViewModel>();

        IHost host = builder.Build();
        Locator.Register(host.Services);
        // This is needed until we unroll all the singletons...
        Initialize(host.Services);
        
    }

    private static void Initialize(IServiceProvider services)
    {
        CircularReferenceManager circularReferenceManager = services.GetRequiredService<CircularReferenceManager>();
        FileCommands fileCommands = services.GetRequiredService<FileCommands>();
        
        SetVariableLogic.Self.Initialize(circularReferenceManager, fileCommands);
    }
}
