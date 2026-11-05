using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Xunit;

namespace Gum.Presentation.Tests.ExposedForms;

/// <summary>
/// A project shaped like a game's: a Forms control (carrying one of the shipped behaviors), a Row component
/// with an inner instance of it, and a screen with an instance of the Row. Behaviors come from the shipped
/// <c>FormsBehaviors/*.behx</c> files, so every declared Forms property can be run through each path.
/// </summary>
internal class ExposedFormsProject
{
    public const string ExposedName = "RowValue";

    private static readonly Lazy<IReadOnlyList<BehaviorSave>> _behaviors = new(LoadBehaviors);

    public static IReadOnlyList<BehaviorSave> Behaviors => _behaviors.Value;

    /// <summary>Every (behavior, Forms property) pair the shipped behaviors declare.</summary>
    public static TheoryData<string, string> AllPairs
    {
        get
        {
            TheoryData<string, string> data = new TheoryData<string, string>();
            foreach (BehaviorSave behavior in Behaviors)
            {
                foreach (VariableSave property in behavior.FormsProperties.Where(item => !string.IsNullOrEmpty(item.Name)))
                {
                    data.Add(behavior.Name, property.Name);
                }
            }
            return data;
        }
    }

    public GumProjectSave Project { get; } = new GumProjectSave();
    public ComponentSave Control { get; }
    public ComponentSave Row { get; }
    public InstanceSave Inner { get; }
    public ScreenSave Screen { get; }
    public InstanceSave RowInstance { get; }
    public BehaviorSave Behavior { get; }
    public VariableSave Property { get; }

    public ExposedFormsProject(string behaviorName, string propertyName)
    {
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(Project);
        Project.Behaviors.AddRange(Behaviors);
        Behavior = Behaviors.Single(item => item.Name == behaviorName);
        Property = Behavior.FormsProperties.Single(item => item.Name == propertyName);

        Control = AddComponent("Controls/Control", "Container");
        Control.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        Row = AddComponent("Row", "Container");
        Inner = AddInstance(Row, "Inner", Control.Name);

        Screen = new ScreenSave { Name = "Screen" };
        Screen.States.Add(new StateSave { Name = "Default", ParentContainer = Screen });
        Project.Screens.Add(Screen);
        RowInstance = AddInstance(Screen, "RowInstance", Row.Name);

        ObjectFinder.Self.GumProjectSave = Project;
    }

    public ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        return component;
    }

    public static InstanceSave AddInstance(ElementSave owner, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    /// <summary>What the tool's Expose command writes: the inner instance's variable in the owner's Default state.</summary>
    public VariableSave Expose(ElementSave owner, InstanceSave instance, string rootName, string exposedName, string? type = null)
    {
        StateSave state = owner.DefaultState!;
        state.SetValue(instance.Name + "." + rootName, null, instance, type ?? Property.Type);
        VariableSave variable = state.GetVariableSave(instance.Name + "." + rootName)!;
        variable.SetsValue = false;
        variable.ExposedAsName = exposedName;
        return variable;
    }

    public VariableSave ExposeProperty() => Expose(Row, Inner, Property.Name, ExposedName);

    /// <summary>A value of the property's type that differs from the declared default.</summary>
    public object SampleValue()
    {
        string type = (Property.Type ?? "string").TrimEnd('?');
        return type switch
        {
            "bool" => Property.Value is bool declared ? !declared : true,
            "double" => 12.5,
            "float" => 12.5f,
            "int" => 12,
            _ => "Sample",
        };
    }

    public static ElementWithState Level(ElementSave element, string? instanceName = null) =>
        new ElementWithState(element) { InstanceName = instanceName };

    private static IReadOnlyList<BehaviorSave> LoadBehaviors()
    {
        string directory = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsBehaviors");
        List<BehaviorSave> behaviors = new List<BehaviorSave>();
        foreach (string file in Directory.GetFiles(directory, "*.behx").OrderBy(item => item, StringComparer.Ordinal))
        {
            BehaviorSave? behavior = GumFileSerializer.DeserializeBehaviorSave(File.ReadAllText(file), GumProjectSave.NativeVersion);
            behaviors.Add(behavior ?? throw new InvalidOperationException("Could not read " + file));
        }
        return behaviors;
    }

    private static string FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates", "FormsBehaviors")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        throw new DirectoryNotFoundException("The repo root was not found above " + AppContext.BaseDirectory);
    }
}
