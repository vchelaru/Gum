using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public  class ComboBoxTests : BaseTestClass
{
    [Fact]
    public void Visual_Assignment_ShouldSetVisualCorrectly()
    {
        ComboBox comboBox = new ();

        comboBox.Visual = new CGComboBox(tryCreateFormsObject:false);
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ComboBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Clicking_ShouldOpenComboBox_IfInBounds()
    {
        ComboBox comboBox = new ComboBox();

        comboBox.AddToRoot();

        comboBox.IsDropDownOpen.ShouldBe(false);

        Mock<ICursor> cursor = new();
        // combo boxes open on a push
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.WindowPushed).Returns(comboBox.Visual);
        cursor.Setup(x => x.VisualOver).Returns(comboBox.Visual);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(true);

        // Set it up so it's inside the window, but outside the bounds of the combo box
        cursor.Setup(x => x.X).Returns((int)(GraphicalUiElement.CanvasWidth-1));
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns((int)(GraphicalUiElement.CanvasWidth - 1));
        cursor.Setup(x => x.VisualOver).Returns((InteractiveGue?)null);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(false);

    }

    [Fact]
    public void Items_AssignTypedCollection_ShouldSyncInternalListBoxItems()
    {
        // A typed ObservableCollection<string> assigned to ComboBox.Items was a
        // bug vector: HandleCollectionNewItemCreated would try to insert a ListBoxItem
        // into the typed collection, throwing ArgumentException.
        var items = new ObservableCollection<string> { "A", "B", "C" };
        ComboBox comboBox = new();

        comboBox.Items = items;

        comboBox.ListBox.Items.Count.ShouldBe(3);
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(3);
    }

    [Fact]
    public void IsDropDownOpen_ShouldNotResetListBoxItemBindingContext()
    {
        ComboBox comboBox = new();

        comboBox.AddToRoot();

        comboBox.Visual.EffectiveManagers.ShouldNotBeNull(
            "because this is needed to effectively test removal");

        TestViewModel viewModel = new();
        viewModel.Items.Add("1");
        viewModel.Items.Add("2");
        viewModel.Items.Add("3");

        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(
            nameof(comboBox.Items),
            nameof(viewModel.Items));

        comboBox.ListBox.Items.Count.ShouldBe(3);
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(3);
        comboBox.ListBox.ListBoxItems[0].BindingContext.ShouldBe("1");

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.ListBoxItems[0].BindingContext.ShouldBe("1");

        comboBox.IsDropDownOpen = false;

        comboBox.ListBox.ListBoxItems[0].BindingContext.ShouldBe("1");

    }

    public class CGComboBox : InteractiveGue
    {
        public MonoGameGum.Forms.DefaultVisuals.DefaultListBoxRuntime? ListBoxInstance;
        public RectangleRuntime? FocusedIndicator { get; private set; }

        public CGComboBox(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new ColoredRectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new MonoGameGum.Forms.DefaultVisuals.DefaultListBoxRuntime(tryCreateFormsObject: false);
                ListBoxInstance.Name = "ListBoxInstance";


                background.Name = "Background";
                this.Children.Add(background);

                TextInstance.Text = "Selected Item";
                this.Children.Add(TextInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                var rightSideText = new TextRuntime();
                rightSideText.Name = "DropdownIndicator";

                this.Children.Add(rightSideText);

                this.Children.Add(ListBoxInstance);
                ListBoxInstance.Visible = false;

                var comboBoxCategory = new StateSaveCategory();
                comboBoxCategory.Name = "ComboBoxCategory";
                this.AddCategory(comboBoxCategory);

                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    comboBoxCategory.States.Add(state);
                    currentState = state;
                }

                AddState(FrameworkElement.DisabledStateName);

                AddState(FrameworkElement.DisabledFocusedStateName);

                AddState(FrameworkElement.EnabledStateName);

                AddState(FrameworkElement.FocusedStateName);

                AddState(FrameworkElement.HighlightedStateName);

                AddState(FrameworkElement.HighlightedFocusedStateName);

                AddState(FrameworkElement.PushedStateName);

            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => (ComboBox)FormsControlAsObject;
    }

    class TestViewModel : ViewModel
    {
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();

        public TestViewModel()
        {
        }
    }
}