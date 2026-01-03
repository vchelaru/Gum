# Tutorial - Task Screen

## Introduction

ViewModels can be used to provide bound properties for your view as well as logic for managing your data. This page creates a task screen which displays a list of tasks in a ListBox. The user can add and remove tasks using the UI. The logic for this behavior is in the bound view model.

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

Using the Get and Set methods on properties is recommended, even if the property itself is unlikely to change. By using Get and Set on a property of type ObservableCollection, other properties can still use a DependsOn attribute which references the Tasks property. For more information on using DependsOn, see the [Binding Deep Dive](binding-deep-dive.md) page.
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
public class TaskScreen : FrameworkElement { ListBox TaskListBox; Button AddTaskButton; Button RemoveTaskButton; TextBox TaskNameTextBox;

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
var screen = new TaskScreen();
screen.AddToRoot();

var viewModel = new TaskScreenViewModel();
screen.BindingContext = viewModel;
```

Now we have a fully functional screen where tasks can be added and removed.

<figure><img src="../../.gitbook/assets/14_11 06 20.gif" alt=""><figcaption><p>TaskScreen using bound properties to add and remove tasks</p></figcaption></figure>
