using System;
using Gum.Commands;
using Gum.Controls;
using Gum.Managers;

namespace Gum;

public class GumCommands
{
    public static GumCommands Self { get; private set; }
    
    public GuiCommands GuiCommands { get; }

    public WireframeCommands WireframeCommands { get; }

    public FileCommands FileCommands { get; }

    public EditCommands Edit { get; }

    public ToolCommands.ProjectCommands ProjectCommands { get; }

    public GumCommands(GuiCommands guiCommands, 
        FileCommands fileCommands, 
        EditCommands editCommands, 
        WireframeCommands wireframeCommands,
        ToolCommands.ProjectCommands projectCommands)
    {
        Self = Self switch
        {
            { } => throw new InvalidOperationException(),
            _ => this
        };
        GuiCommands = guiCommands;
        FileCommands = fileCommands;
        Edit = editCommands;
        WireframeCommands = wireframeCommands;
        ProjectCommands = projectCommands;
    }

    /// <summary>
    /// Performs the argument action multiple times, swalling exceptions every time except the last. 
    /// This can be used to perform operations which may fail from time to time, like writing to disk.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="numberOfTimesToTry">The number of times to try.</param>
    public void TryMultipleTimes(Action action, int numberOfTimesToTry = 5)
    {
        const int msSleep = 200;
        int failureCount = 0;

        while (failureCount < numberOfTimesToTry)
        {
            try
            {
                action();
                break;
            }


            catch (Exception e)
            {
                failureCount++;
                System.Threading.Thread.Sleep(msSleep);
                if (failureCount >= numberOfTimesToTry)
                {
                    throw e;
                }
            }
        }
    }

    public void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
    {
        GuiCommands.Initialize(mainWindow, mainPanelControl);
        FileCommands.Initialize(mainWindow);
    }

}
