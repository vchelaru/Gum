using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CategoryViewModelTests
{
    [Fact]
    public void AddStateCommand_Execute_RaisesAddStateRequestedWithSelf()
    {
        CategoryViewModel categoryVm = new() { Data = new StateSaveCategory { Name = "Category" } };
        object? raisedSender = null;
        categoryVm.AddStateRequested += (sender, _) => raisedSender = sender;

        categoryVm.AddStateCommand.Execute(null);

        raisedSender.ShouldBe(categoryVm);
    }
}
