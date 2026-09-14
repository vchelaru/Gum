using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.Reflection;
using Moq;
using Moq.AutoMock;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// The Variables tab behavior that <see cref="PropertyGridManager"/> owns for both heads: the
/// filter box, Ctrl+E focus, and the behavior-variable selection.
/// </summary>
public class PropertyGridManagerTests
{
    private readonly AutoMocker _mocker;
    private readonly FakeTabView _view;
    private readonly Mock<IPluginTab> _tab;

    public PropertyGridManagerTests()
    {
        _mocker = new AutoMocker();
        _mocker.Use(new LocalizationService());
        _mocker.Use(new TypeManager());
        _view = new FakeTabView();
        _tab = new Mock<IPluginTab>();
        _mocker.GetMock<IVariableGridHead>()
            .Setup(head => head.CreateVariablesTabView(It.IsAny<MainControlViewModel>()))
            .Returns(_view);
        _mocker.GetMock<ITabManager>()
            .Setup(tabs => tabs.AddControl(_view, "Variables", TabLocation.CenterBottom))
            .Returns(_tab.Object);
    }

    private sealed class FakeGrid : IDataUiGrid
    {
        private readonly DataUiGridModel _model = new DataUiGridModel();

        public DataUiGridModel Model => _model;
        public object? Instance { get => _model.Instance; set => _model.Instance = value; }
        public bool IsEnabled { get; set; } = true;
        public BulkObservableCollection<MemberCategory> Categories => _model.Categories;

        public event Action<string, PropertyChangedArgs>? PropertyChange
        {
            add => _model.PropertyChange += value;
            remove => _model.PropertyChange -= value;
        }

        public void SetCategories(IList<MemberCategory> newCategories) => _model.SetCategories(newCategories);
        public void SetMultipleCategoryLists(List<List<MemberCategory>> lists) => _model.SetMultipleCategoryLists(lists);
        public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch) => _model.ApplyMemberFilter(isMatch);
        public InstanceMember? GetInstanceMember(string memberName) => _model.GetInstanceMember(memberName);
        public void InsertSpacesInCamelCaseMemberNames() => _model.InsertSpacesInCamelCaseMemberNames();
        public void Refresh() => _model.SimulateValueChangedOnAllMembers();
    }

    private sealed class FakeTabView : IVariablesTabView
    {
        public object Control => this;
        public FakeGrid Variables { get; } = new FakeGrid();
        public IDataUiGrid VariablesGrid => Variables;
        public IDataUiGrid BehaviorGrid { get; } = new FakeGrid();
        public int FocusCount { get; private set; }
        public event EventHandler? AddVariableClicked;
        public event EventHandler? SelectedBehaviorVariableChanged;

        public void FocusVariableFilter() => FocusCount++;

        public void RaiseAddVariableClicked() => AddVariableClicked?.Invoke(this, EventArgs.Empty);
        public void RaiseSelectedBehaviorVariableChanged() => SelectedBehaviorVariableChanged?.Invoke(this, EventArgs.Empty);
    }

    private static MemberCategory CategoryWith(string name, params string[] memberNames)
    {
        MemberCategory category = new MemberCategory(name);
        foreach (string memberName in memberNames)
        {
            category.Members.Add(new InstanceMember(memberName, new object()));
        }
        return category;
    }

    [Fact]
    public void FocusVariableFilter_ShowsAndSelectsTheTab_ThenFocusesTheView()
    {
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        sut.InitializeEarly();

        sut.FocusVariableFilter();

        _tab.Verify(tab => tab.Show(), Times.Once);
        _tab.VerifySet(tab => tab.IsSelected = true, Times.Once);
        _view.FocusCount.ShouldBe(1);
    }

    [Fact]
    public void SelectedBehaviorVariable_CanBeSetBeforeTheViewExists()
    {
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        VariableSave variable = new VariableSave { Name = "IsEnabled", Type = "bool" };

        sut.SelectedBehaviorVariable = variable;

        sut.VariableViewModel.SelectedBehaviorVariable.ShouldBe(variable);
    }

    [Fact]
    public void VariableFilterText_NarrowsTheVariablesGrid_AndClearingRestoresIt()
    {
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        sut.InitializeEarly();
        MemberCategory position = CategoryWith("FilterPosition", "X", "Y", "XUnits");
        _view.Variables.SetCategories(new List<MemberCategory> { position });

        sut.VariableViewModel.VariableFilterText = "x";

        position.Members.Select(m => m.Name).ShouldBe(new[] { "X", "XUnits" });

        sut.VariableViewModel.VariableFilterText = "";

        position.Members.Select(m => m.Name).ShouldBe(new[] { "X", "Y", "XUnits" });
    }
}
