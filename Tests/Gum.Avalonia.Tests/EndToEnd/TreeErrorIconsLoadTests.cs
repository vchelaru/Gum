using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.TreeView;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A loaded project's tree shows before its elements are checked for errors; the "!" icons fill in
/// from UI-thread work the load posts, so the tree is usable while they do.
/// </summary>
public class TreeErrorIconsLoadTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void ProjectLoad_ShowsTheTreeFirst_ThenItsErrorIcons()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeErrorIconsLoad");
        ComponentSave broken = fixture.AddComponent("Broken");
        fixture.AddInstance(broken, "Caption", "Text");
        StateSave state = broken.DefaultState.ShouldNotBeNull();
        state.SetValue("Caption.Font", "Fonts/Missing.ttf", "string");
        state.SetValue("Caption.FontSize", 20, "int");
        ComponentSave fine = fixture.AddComponent("Fine");
        Settle();
        IErrorChecker errorChecker = Services.GetRequiredService<IErrorChecker>();
        ElementTreeViewManager tree = Services.GetRequiredService<ElementTreeViewManager>();
        List<ElementSave> checkedElements = new List<ElementSave>();
        void Record(ElementSave element, ErrorViewModel[] errors) => checkedElements.Add(element);
        errorChecker.ErrorsChecked += Record;
        // The checks made while the project was built already set the icon; clear it so only the load's
        // own check can set it again.
        tree.UpdateErrorIndicatorsForElement(broken, hasErrors: false);
        try
        {
            Services.GetRequiredService<PluginManager>().ProjectLoad(fixture.Project);

            checkedElements.ShouldBeEmpty("the tree's load notification returned before checking any element");
            GumTreeNode brokenNode = tree.GetTreeNodeFor(broken).ShouldNotBeNull();
            int iconBeforeCheck = brokenNode.ImageIndex;
            int fineIcon = tree.GetTreeNodeFor(fine).ShouldNotBeNull().ImageIndex;

            Settle();

            fixture.Project.AllElements.ShouldBeSubsetOf(checkedElements);
            brokenNode.ImageIndex.ShouldNotBe(iconBeforeCheck, "the missing font shows once the check ran");
            tree.GetTreeNodeFor(fine)!.ImageIndex.ShouldBe(fineIcon);
        }
        finally
        {
            errorChecker.ErrorsChecked -= Record;
        }
    }

    private static void Settle()
    {
        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
