using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The delete confirmation under this head: plugins add neutral options, the view renders them, the
/// user's choices flow back, and confirming hands the same options back to the plugins.
/// </summary>
public class DeleteOptionsDialogTests
{
    [Fact]
    public void Service_LetsPluginsAddOptions_ThenConfirmsWithTheSameDialog()
    {
        RecordingDeletePluginNotifier notifier = new RecordingDeletePluginNotifier();
        AvaloniaDeleteDialogService service = new AvaloniaDeleteDialogService(new AffirmingDialogService(), notifier);
        object[] objects = { new ComponentSave { Name = "Button" } };

        IDeleteDialogResult result = service.ShowDeleteDialog("Delete?", "Delete Button?", objects);
        service.NotifyConfirmed(result, objects);

        result.Result.ShouldBe(true);
        notifier.Shown.ShouldNotBeNull();
        notifier.Shown!.Title.ShouldBe("Delete?");
        notifier.Shown.Message.ShouldBe("Delete Button?");
        notifier.Confirmed.ShouldBeSameAs(notifier.Shown);
    }

    [AvaloniaFact]
    public void View_RendersPluginOptions_AndWritesTheUsersChoicesBack()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel { Title = "Delete?", Message = "Delete Button?" };
        DeleteOptionCheckboxViewModel deleteXml = new DeleteOptionCheckboxViewModel { Label = "Delete XML file", IsChecked = true };
        DeleteOptionCheckboxViewModel onlyParent = new DeleteOptionCheckboxViewModel { Label = "Delete only parent(s)", IsChecked = true };
        DeleteOptionCheckboxViewModel withChildren = new DeleteOptionCheckboxViewModel { Label = "Delete parent and children" };
        viewModel.CheckBoxes.Add(deleteXml);
        viewModel.Choices.Add(new DeleteOptionChoiceViewModel("Delete children?", new[] { onlyParent, withChildren }));
        DialogViewRegistry registry = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>();

        Control view = registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        view.ShouldBeOfType<DeleteOptionsDialogView>();
        CheckBox checkBox = view.GetLogicalDescendants().OfType<CheckBox>().Single();
        RadioButton[] radios = view.GetLogicalDescendants().OfType<RadioButton>().ToArray();

        checkBox.IsChecked = false;
        radios[1].IsChecked = true;

        window.Title.ShouldBe("Delete?");
        radios.Length.ShouldBe(2);
        deleteXml.IsChecked.ShouldBeFalse();
        withChildren.IsChecked.ShouldBeTrue();
        onlyParent.IsChecked.ShouldBeFalse();
        window.Close();
    }

    private sealed class AffirmingDialogService : IDialogService
    {
        public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null) => MessageDialogResult.Affirmative;

        public bool Show<T>(T dialogViewModel) where T : DialogViewModel => true;

        public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel => throw new NotSupportedException();

        public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null) => null;

        public List<string>? OpenFile(OpenFileDialogOptions? options = null) => null;

        public string? SaveFile(SaveFileDialogOptions? options = null) => null;
    }

    private sealed class RecordingDeletePluginNotifier : IDeletePluginNotifier
    {
        public DeleteOptionsDialogViewModel? Shown { get; private set; }

        public DeleteOptionsDialogViewModel? Confirmed { get; private set; }

        public void ShowDeleteOptions(DeleteOptionsDialogViewModel dialog, Array objectsToDelete) => Shown = dialog;

        public void ConfirmDeleteOptions(DeleteOptionsDialogViewModel dialog, Array deletedObjects) => Confirmed = dialog;

        public bool TryHandleDelete() => false;

        public void ElementDelete(ElementSave element) { }

        public void StateDelete(StateSave stateSave) { }

        public void CategoryDelete(StateSaveCategory category) { }

        public void BehaviorDeleted(BehaviorSave behavior) { }

        public void InstanceDelete(ElementSave elementSave, InstanceSave instance) { }

        public void InstancesDelete(ElementSave elementSave, InstanceSave[] instances) { }

        public void BehaviorInstanceDelete(BehaviorSave behavior, BehaviorInstanceSave instance) { }
    }
}
