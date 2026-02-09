using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins;

public interface IPluginManager
{
    // todo - interface this...
    void BeforeSavingElementSave(ElementSave savedElement);

    void AfterSavingElementSave(ElementSave savedElement);

    void BeforeSavingProjectSave(GumProjectSave savedProject);

    void ProjectLoad(GumProjectSave newlyLoadedProject);

    void ProjectPropertySet(string propertyName);

    void ProjectSave(GumProjectSave savedProject);

    GraphicalUiElement CreateGraphicalUiElement(ElementSave elementSave);

    void ProjectLocationSet(FilePath filePath);

    void Export(ElementSave elementToExport);

    void ModifyDefaultStandardState(string type, StateSave stateSave);

    bool TryHandleDelete();

    internal void ShowDeleteDialog(DeleteOptionsWindow window, Array objectsToDelete);







    void InstanceReordered(InstanceSave instance);

    List<Attribute> GetAttributesFor(VariableSave variableSave);

    bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember);




    void FocusSearch();

    bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf);
}
