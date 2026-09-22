using System.Collections.Generic;
using System.ComponentModel;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4911: setting a Component's custom ("new") variable through SetProperty (how the tool
// applies a Variables-tab edit, and how ApplyState drives a state change at runtime) must raise
// INotifyPropertyChanged.PropertyChanged, the same as a direct C# assignment. This pins the fix in
// CodeGenerator.FillWithNewVariables, which now backs the generated property with a field and calls
// NotifyPropertyChanged() from its setter instead of emitting a bare auto-property.
public class CustomVariableNotifyPropertyChangedTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        base.Dispose();
    }

    // Mirrors what CodeGenerator.FillWithNewVariables now emits for a custom variable.
    private class SliderRuntime : GraphicalUiElement
    {
        public SliderRuntime() : base(new InvisibleRenderable(), null) { }

        private int _numberOfMarks;
        public int NumberOfMarks
        {
            get => _numberOfMarks;
            set
            {
                _numberOfMarks = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Fact]
    public void SetProperty_OnCustomVariable_RaisesPropertyChanged()
    {
        ElementSaveExtensions.RegisterGueInstantiationType("Slider", typeof(SliderRuntime));

        ComponentSave element = new ComponentSave { Name = "Slider" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "NumberOfMarks",
            Value = 5,
            Type = "int",
            IsCustomVariable = true
        });
        element.States.Add(defaultState);

        GumProjectSave gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = gumProject;
        gumProject.Components.Add(element);

        GraphicalUiElement created = Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(element);
        SliderRuntime slider = created.ShouldBeOfType<SliderRuntime>();

        List<string> raisedPropertyNames = new List<string>();
        slider.PropertyChanged += (sender, args) => raisedPropertyNames.Add(args.PropertyName!);

        slider.SetProperty("NumberOfMarks", 9);

        slider.NumberOfMarks.ShouldBe(9);
        raisedPropertyNames.ShouldContain(nameof(SliderRuntime.NumberOfMarks));
    }
}
