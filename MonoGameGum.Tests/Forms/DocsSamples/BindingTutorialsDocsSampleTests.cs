// Sample code is copied from the docs as written, and the docs samples do not use nullable annotations.
#nullable disable warnings
using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the code-only samples on docs/code/binding-viewmodels/tutorial-settings-screen.md and tutorial-task-screen.md.
/// </summary>
public class BindingTutorialsDocsSampleTests : BaseTestClass
{
    public class SettingsViewModel : ViewModel
    {
        public float MusicVolume
        {
            get => Get<float>();
            set => Set(value);
        }

        public float SfxVolume
        {
            get => Get<float>();
            set => Set(value);
        }

        [DependsOn(nameof(SfxVolume))]
        public string SfxVolumeDisplay => $"SFX: {SfxVolume:N0}";

        [DependsOn(nameof(MusicVolume))]
        public string MusicVolumeDisplay => $"Music: {MusicVolume:N0}";
    }

    public class SettingsScreen : FrameworkElement
    {
        public Slider SfxSlider;
        public Label SfxValueLabel;
        public Slider MusicSlider;
        public Label MusicValueLabel;

        public SettingsScreen() : base(new ContainerRuntime())
        {
            CreateLayout();

            CreateBinding();
        }

        private void CreateLayout()
        {
            this.Dock(Gum.Wireframe.Dock.Fill);

            var panel = new StackPanel();
            this.AddChild(panel);
            panel.Anchor(Gum.Wireframe.Anchor.Center);

            var label = new Label();
            panel.AddChild(label);
            label.Text = "SFX Volume:";

            SfxSlider = new Slider();
            panel.AddChild(SfxSlider);
            SfxSlider.Width = 200;

            SfxValueLabel = new Label();
            panel.AddChild(SfxValueLabel);

            var musicLabel = new Label();
            panel.AddChild(musicLabel);
            musicLabel.Text = "Music Volume:";
            musicLabel.Y = 12;

            MusicSlider = new Slider();
            panel.AddChild(MusicSlider);
            MusicSlider.Width = 200;

            MusicValueLabel = new Label();
            panel.AddChild(MusicValueLabel);
        }

        private void CreateBinding()
        {
            SfxSlider.SetBinding(
                nameof(SfxSlider.Value),
                nameof(SettingsViewModel.SfxVolume));

            SfxValueLabel.SetBinding(
                nameof(SfxValueLabel.Text),
                nameof(SettingsViewModel.SfxVolumeDisplay));

            MusicSlider.SetBinding(
                nameof(MusicSlider.Value),
                nameof(SettingsViewModel.MusicVolume));

            MusicValueLabel.SetBinding(
                nameof(MusicValueLabel.Text),
                nameof(SettingsViewModel.MusicVolumeDisplay));
        }
    }

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

    public class TaskItemViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    public class TaskScreen : FrameworkElement
    {
        public ListBox TaskListBox;
        public Button AddTaskButton;
        public Button RemoveTaskButton;
        public TextBox TaskNameTextBox;

        TaskScreenViewModel ViewModel => (TaskScreenViewModel)BindingContext!;

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

    [Fact]
    public void SettingsScreen_ViewModelValues_ReachSlidersAndLabels()
    {
        var screen = new SettingsScreen();
        screen.AddToRoot();

        var viewModel = new SettingsViewModel();
        screen.BindingContext = viewModel;
        viewModel.SfxVolume = 50;
        viewModel.MusicVolume = 75;

        screen.SfxSlider.Value.ShouldBe(50);
        screen.MusicSlider.Value.ShouldBe(75);
        screen.SfxValueLabel.Text.ShouldBe($"SFX: {50f:N0}");

        screen.SfxSlider.Value = 20;
        viewModel.SfxVolume.ShouldBe(20);
        screen.SfxValueLabel.Text.ShouldBe($"SFX: {20f:N0}");
    }

    [Fact]
    public void TaskScreen_AddAndRemove_UpdateListBox()
    {
        var screen = new TaskScreen();
        screen.AddToRoot();

        var viewModel = new TaskScreenViewModel();
        screen.BindingContext = viewModel;

        screen.TaskNameTextBox.Text = "Write docs";
        screen.AddTaskButton.PerformClick();

        screen.TaskListBox.Items.Count.ShouldBe(1);
        screen.TaskListBox.ListBoxItems[0].ToString().ShouldBe("Write docs");
        screen.TaskNameTextBox.Text.ShouldBe(string.Empty);

        screen.TaskListBox.SelectedIndex = 0;
        viewModel.SelectedTask.ShouldNotBeNull();
        screen.RemoveTaskButton.PerformClick();
        screen.TaskListBox.Items.Count.ShouldBe(0);

        // The hint says replacing the whole collection still updates the ListBox.
        viewModel.Tasks = new() { new TaskItemViewModel { Name = "A" }, new TaskItemViewModel { Name = "B" } };
        screen.TaskListBox.Items.Count.ShouldBe(2);
    }
}
