# Tutorial - Task Screen

## Introduction

ViewModels can be used to provide bound properties for your view as well as logic for managing your data. This page creates a task screen which displays a list of tasks in a ListBox. The user can add and remove tasks using the UI. The logic for this behavior is in the bound view model.

[Try the completed example on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACq1XX2_bNhB_96dg-yRhAYFiwB7mOkNiJ56BuiniNN1bQUtnm7MkGiQlNwv83XekaImSpdgFqheTvLvf3ZH3z7ni2ZrMRSamLIVpng4HuT3CJb0XMlUnB3QsMi1F0kGZwIrliX7mKmeJos-_-yzzomigf-MSVhK1Hg_nPJJCiZWm_2SM3hvSXsjtGTKdSrbb8Kgyx3OGTnOYgOQFnh_JixelIUUnkgQizUWm6MPyX1zORQzJcDDY5cuERyRKmFLkiantDPmfOewtA_mTVOvB64Dg5wSUlgb_M6q2xyXRfGvQZHRNpqA_lkzXQTisqKqkLkAHBUtycKTD4NBhyyKSANkF1jwsFciCLROoPf144s21BVX99l4Ic5lDvn0nGH-hgFEAsSH1W3Qq-PPKj08Fe4P2616s7WDrtYKwpcZePhmRDPZBJ0gheEz-ZlmcwE1sL-YEg69I8K40ks7U5zxJHuS3Ddew2LEIAs_HsBRtildmUFQQoCEdIf9qgxrt9MDIwbsQ8_m0kbthepfu9EvNd3jbxUdIRQG9XvrxQd7hraGvb7tUAjYEw1Nr-vMM06uqM3cJpJBpl2WfuNK34ofldesS-DbXWmTEvVa5a1BqJ33iE_yo8MwVuj2Wo6M37cyvVxiKQQdHeMuzGB_BlGtEG_ZEZxCil0umwD6-YWY8A_mYZ5qnELigqW93LIFp-MReRK79pMBscApvsviuwKtS7ZjGQoyi5Ys3YVo69IZjMxHRNmg0CntE7zk-u_PGfAWTZMcycxEmkchCs2j7xRz45llIfJXxhidxYPk9qt1TkzGmLozIH23STRZthGyZUx7SMfoK0jfJiwpnlNv5Fjngo0meTBvKCwgH53Zn4Dw5H7IRmw6w3LyB1xDy2Brn1GhDxPd4ai1_76ltB_6lmttyHmebVOkvCUcT-mKwM2Q7SrR7FlrzB41ik-E9i5X_gtQUUBVedbFVCUptkQp7AudntB0LXDnInFPbKId92idc7RL2Mod0CfIL0xvzXLXmRoOgtrv0B-0lrvjs5vecE42-1hfcdIy1bkt-G5Hg-xX5HppSWUO0-uobYXUBjN-7-gY4M5V-wGprfl0fOQ6vEyh4BHOWsTVIsq5GWsuTpwscwpBull9ndjKpzo5Dd7O-W1VY2l8rLJdunQoDUx3DIZmpucgV4PzOceJDCS1zGNa5I7QNHIKeSsljl0azjGvOEv4fnGSPtZd6DEbRFWn-T3gGqXCsxL8L7bKuyi7sap7XsrxZzJ6YavEkHoWwLamBUdR9sgXjzWUneM3eiaIVjAdvuqbvXLvddV_X112MFSgwL_SEHZas3aL77hx3xVRbarV3k89YMJFsf4H-RqRgDgCTAf4LMB1PyGyViD3I23r8rW22-G1L7eGJnYfB_wsU-nKCDgAA)

## Defining TaskScreenViewModel

The `TaskScreenViewModel` class contains all of the properties for our `TaskScreen` binding. Not only does it include properties for binding but it also provides methods for performing actions on these properties, such as adding or removing tasks.

```csharp
public class TaskScreenViewModel : ViewModel
{
    public ObservableCollection<TaskItemViewModel> Tasks
    {
        get => Get<ObservableCollection<TaskItemViewModel>>();
        set => Set(value);
    }

    public TaskItemViewModel? SelectedTask
    {
        get => Get<TaskItemViewModel?>();
        set => Set(value);
    }

    public string NewTaskName
    {
        get => Get<string>();
        set => Set(value);
    }

    public TaskScreenViewModel()
    {
        Tasks = new ();
    }

    public void HandleAddTask()
    {
        if (!string.IsNullOrWhiteSpace(NewTaskName))
        {
            Tasks.Add(new TaskItemViewModel { Name = NewTaskName });
            NewTaskName = string.Empty;
        }
    }

    public void HandleRemoveTask()
    {
        if (SelectedTask != null)
        {
            Tasks.Remove(SelectedTask);
        }
    }
}
```

This view model includes an `ObservableCollection` property called `Tasks`. By making this inherit from `ObservableCollection`, a bound `ListBox` automatically updates its items whenever items are added or removed.

The `ViewModel` also includes methods for adding or removing tasks. These methods perform their own validation, using the `ViewModel`'s properties to determine if actions can be performed.

{% hint style="info" %}
The Tasks property gets assigned in the `TaskScreenViewModel`'s constructor and doesn't change. This means that the property could have a `private` or `init` setter. This example creates a public setter to show that the entire collection could also be replaced and the binding ListBox would still update its items correctly.

Using the Get and Set methods on properties is recommended, even if the property itself is unlikely to change. By using Get and Set on a property of type ObservableCollection, other properties can still use a DependsOn attribute which references the Tasks property. For more information on using DependsOn, see the [View Model Property Dependency](view-model-property-dependency.md) page.
{% endhint %}

Each task is itself a view model, so we need to define the `TaskItemViewModel`. This example is simple so the `TaskItemViewModel` only includes a single property, but a more complex project may include a class with more properties.

```csharp
public class TaskItemViewModel : ViewModel
{
    public string Name
    {
        get => Get<string>();
        set => Set(value);
    }
}
```

## Defining TaskScreen

This section shows how to set up binding in a `TaskScreen` class. The binding is the same whether you use the Gum UI tool or a code-only approach.

{% tabs %}
{% tab title="Code-Only" %}
```csharp
public class TaskScreen : FrameworkElement
{
    ListBox TaskListBox;
    Button AddTaskButton;
    Button RemoveTaskButton;
    TextBox TaskNameTextBox;

    TaskScreenViewModel ViewModel => (TaskScreenViewModel)BindingContext;

    public TaskScreen() : base(new ContainerRuntime())
    {
        CreateLayout();

        SetBindingAndEvents();
    }

    private void CreateLayout()
    {
        this.Dock(Gum.Wireframe.Dock.Fill);

        var panel = new StackPanel();
        this.AddChild(panel);
        panel.Spacing = 6;
        panel.Anchor(Gum.Wireframe.Anchor.Center);

        TaskListBox = new ListBox();
        panel.AddChild(TaskListBox);

        TaskNameTextBox = new TextBox();
        panel.AddChild(TaskNameTextBox);

        AddTaskButton = new Button();
        panel.AddChild(AddTaskButton);
        AddTaskButton.Text = "Add Task";

        RemoveTaskButton = new Button();
        panel.AddChild(RemoveTaskButton);
        RemoveTaskButton.Text = "Remove Task";
    }

    private void SetBindingAndEvents()
    {
        TaskListBox.SetBinding(
            nameof(TaskListBox.Items), 
            nameof(ViewModel.Tasks));

        TaskListBox.SetBinding(
            nameof(TaskListBox.SelectedObject), 
            nameof(ViewModel.SelectedTask));

        TaskListBox.DisplayMemberPath = nameof(TaskItemViewModel.Name);

        TaskNameTextBox.SetBinding(
            nameof(TaskNameTextBox.Text), 
            nameof(ViewModel.NewTaskName));

        AddTaskButton.Click += (_,_) => ViewModel.HandleAddTask();
        RemoveTaskButton.Click += (_,_) => ViewModel.HandleRemoveTask();
    }
}
```
{% endtab %}

{% tab title="Gum UI tool (with code generation)" %}

```csharp
class TaskScreen
{
    TaskScreenViewModel ViewModel => (TaskScreenViewModel)BindingContext;
    
    private void CustomInitialize()
    {
        TaskListBox.SetBinding(
            nameof(TaskListBox.Items), 
            nameof(ViewModel.Tasks));
    
        TaskListBox.SetBinding(
            nameof(TaskListBox.SelectedObject), 
            nameof(ViewModel.SelectedTask));
    
        TaskListBox.DisplayMemberPath = nameof(TaskItemViewModel.Name);
    
        TaskNameTextBox.SetBinding(
            nameof(TaskNameTextBox.Text), 
            nameof(ViewModel.NewTaskName));
    
        AddTaskButton.Click += (_,_) => ViewModel.HandleAddTask();
        RemoveTaskButton.Click += (_,_) => ViewModel.HandleRemoveTask();
    }
}
```
{% endtab %}
{% endtabs %}

The `TaskListBox` and `TaskNameTextBox` bind to properties on the view model using the regular `SetBinding` call.

The `TaskListBox` sets its `DisplayMemberPath` to `TaskItemViewModel.Name`, resulting in the name of each item being displayed in the `ListBoxItem`. If this is not assigned, then each `ListBoxItem` displays the qualified class name of the `ViewModel`.

Finally, the two buttons have their `Click` events handled by calling the `ViewModel`'s `HandleAddTask` and `HandleRemoveTask` methods.

{% hint style="info" %}
Currently Gum does not support command binding. Future versions of Gum may introduce this feature, but in the meantime events must be manually subscribed and handled as shown in the code above.
{% endhint %}

## Using the TaskScreen

Once the `TaskScreen` is defined, we can instantiate this in our game. The following code shows how to create a `TaskScreen` instance and assign its `BindingContext` to a `TaskScreenViewModel` instance.

```csharp
// Initialize
var screen = new TaskScreen();
screen.AddToRoot();

var viewModel = new TaskScreenViewModel();
screen.BindingContext = viewModel;
```

Now we have a fully functional screen where tasks can be added and removed.

<figure><img src="../../.gitbook/assets/14_11 06 20.gif" alt=""><figcaption><p>TaskScreen using bound properties to add and remove tasks</p></figcaption></figure>
