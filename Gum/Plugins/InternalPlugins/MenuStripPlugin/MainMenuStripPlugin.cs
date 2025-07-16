using ExCSS;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;

namespace Gum.Plugins.InternalPlugins.MenuStripPlugin;

[Export(typeof(PluginBase))]
public class MainMenuStripPlugin : InternalPlugin
{
    static MenuStripManager _menuStripManager;

    public static void InitializeMenuStrip()
    {
        _menuStripManager = new MenuStripManager();
        _menuStripManager.Initialize();

    }

    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.BehaviorVariableSelected += HandleBehaviorVariableSelected;
        this.AfterUndo += HandleAfterUndo;
        this.UiZoomValueChanged += HandleUiZoomValueChanged;
    }

    private void HandleUiZoomValueChanged()
    {
        _menuStripManager.HandleUiZoomValueChanged();
    }

    private void HandleAfterUndo()
    {
        _menuStripManager.RefreshUI();
    }

    private void HandleBehaviorVariableSelected(VariableSave save)
    {
        _menuStripManager.RefreshUI();
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        _menuStripManager.RefreshUI();
    }

    private void HandleBehaviorSelected(BehaviorSave save)
    {
        _menuStripManager.RefreshUI();
    }

    private void HandleElementSelected(ElementSave save)
    {
        _menuStripManager.RefreshUI();
    }
}
