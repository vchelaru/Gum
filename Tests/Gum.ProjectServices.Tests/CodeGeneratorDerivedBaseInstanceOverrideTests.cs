using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A derived component's override of a variable on an instance its base owns (#7093) is assigned in the
/// derived class's ApplyDefaultVariables, after the base ran, through the base's public instance property.
/// </summary>
public class CodeGeneratorDerivedBaseInstanceOverrideTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);

        return new CodeGenerator(codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
    }

    private ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        return component;
    }

    /// <summary>Builds Grand (owns Holder), Middle : Grand and Leaf : Middle, the latter two overriding Holder.</summary>
    private string Generate(string elementName, OutputLibrary library, ObjectInstantiationType instantiation, Action<ComponentSave, ComponentSave, ComponentSave>? configure = null)
    {
        Project.Components.Clear();
        StateSave containerDefaults = StandardElementsManager.Self.DefaultStates!["Container"];
        StandardElementSave container = Project.StandardElements.Single(item => item.Name == "Container");
        container.DefaultState.Variables.Clear();
        foreach (VariableSave variable in containerDefaults.Variables)
        {
            container.DefaultState.Variables.Add(variable.Clone());
        }
        ComponentSave grand = AddComponent("Grand", "Container");
        grand.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = grand });

        ComponentSave middle = AddComponent("Middle", "Grand");
        middle.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = middle, DefinedByBase = true });
        middle.DefaultState.SetValue("Holder.Width", 111f, middle.Instances[0], "float");

        ComponentSave leaf = AddComponent("Leaf", "Middle");
        leaf.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = leaf, DefinedByBase = true });
        leaf.DefaultState.SetValue("Holder.Height", 222f, leaf.Instances[0], "float");
        leaf.DefaultState.SetValue("Holder.IsRenderTarget", true, leaf.Instances[0], "bool");
        leaf.DefaultState.Variables.Add(new VariableSave { Name = "Holder.Outline_Thickness", Type = "float", Value = 3f, SetsValue = true, ShaderParameter = "Thickness" });

        leaf.DefaultState.SetValue("Holder.WidthUnits", Gum.DataTypes.DimensionUnitType.Absolute, leaf.Instances[0], "DimensionUnitType");
        leaf.DefaultState.SetValue("Holder.Parent", "Missing", leaf.Instances[0], "string");
        ComponentSave inner = AddComponent("Inner", "Container");
        inner.DefaultState.Variables.Add(new VariableSave { Name = "Kid.Width", Type = "float", Value = 1f, SetsValue = true, ExposedAsName = "InnerWidth" });
        inner.Instances.Add(new InstanceSave { Name = "Kid", BaseType = "Container", ParentContainer = inner });
        inner.DefaultState.Variables.Add(new VariableSave { Name = "Container.Visible", Type = "bool", Value = true, SetsValue = true });
        InstanceSave innerInGrand = new InstanceSave { Name = "InnerInstance", BaseType = "Inner", ParentContainer = grand };
        grand.Instances.Add(innerInGrand);
        leaf.Instances.Add(new InstanceSave { Name = "InnerInstance", BaseType = "Inner", ParentContainer = leaf, DefinedByBase = true });
        leaf.DefaultState.SetValue("InnerInstance.InnerWidth", 77f, leaf.Instances[1], "float");
        leaf.DefaultState.SetValue("InnerInstance.Visible", false, leaf.Instances[1], "bool");
        configure?.Invoke(grand, middle, leaf);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            ElementSave element = Project.Components.Single(item => item.Name == elementName);
            return CreateCodeGenerator().GetGeneratedCodeForElement(
                element,
                elementSettings: null!,
                new CodeOutputProjectSettings
                {
                    OutputLibrary = library,
                    RootNamespace = "MyGame",
                    ObjectInstantiationType = instantiation,
                });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GumForms_FullyInCode_DerivedOverrideOfParentOwnedInstance_IsAssignedAfterBase()
    {
        string code = Generate("Middle", OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("base.ApplyDefaultVariables();");
        code.IndexOf("this.Holder.Width = 111f;", StringComparison.Ordinal).ShouldBeGreaterThan(code.IndexOf("base.ApplyDefaultVariables();", StringComparison.Ordinal));
    }

    [Fact]
    public void GumForms_FullyInCode_DerivedOverridesOfGrandparentOwnedInstances_AreAssignedForEveryVariableKind()
    {
        string code = Generate("Leaf", OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("this.Holder.Height = 222f;");
        code.ShouldContain("this.Holder.IsRenderTarget = true;");
        code.ShouldContain("this.Holder.SetProperty(\"Outline_Thickness\", 3f);");
        code.ShouldContain("this.Holder.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;");
        code.ShouldContain("this.InnerInstance.InnerWidth = 77f;");
        code.ShouldContain("this.InnerInstance.Visual.Visible = false;");
        // Each level assigns only its own overrides; the base chain assigns the rest.
        code.ShouldNotContain("Holder.Width = 111f");
    }

    [Fact]
    public void MonoGame_FullyInCode_DerivedOverrideOfGrandparentOwnedInstance_IsAssigned()
    {
        string code = Generate("Leaf", OutputLibrary.MonoGame, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("this.Holder.Height = 222f;");
        code.ShouldContain("this.Holder.IsRenderTarget = true;");
    }

    [Fact]
    public void MonoGame_FindByName_AssignsNoVariables_BecauseTheLoadedProjectSuppliesThem()
    {
        string code = Generate("Leaf", OutputLibrary.MonoGame, ObjectInstantiationType.FindByName);

        code.ShouldContain("Not assigning variables");
        code.ShouldNotContain("Holder.Height");
    }

    private static string Var(string name, string type, string value, string xsd) =>
        $"<Variable><Type>{type}</Type><Name>{name}</Name><Value xsi:type=\"xsd:{xsd}\">{value}</Value><SetsValue>true</SetsValue></Variable>";

    [Fact]
    public void GumForms_FullyInCode_ProjectLoadedFromDisk_AssignsDerivedOverrideOfBaseInstance()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumDerivedOverride_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            new ProjectCreator().Create(gumxPath);
            string header = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ComponentSave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
            string dir = Path.Combine(tempDir, "Components");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Base.gucx"), header + "<Name>Base</Name><BaseType>Container</BaseType><State><Name>Default</Name></State><Instance><Name>Holder</Name><BaseType>Container</BaseType></Instance></ComponentSave>");
            File.WriteAllText(Path.Combine(dir, "Derived.gucx"), header + "<Name>Derived</Name><BaseType>Base</BaseType><State><Name>Default</Name>"
                + Var("Holder.Width", "float", "111", "float") + Var("Holder.IsRenderTarget", "bool", "true", "boolean") + "</State></ComponentSave>");
            string gumx = File.ReadAllText(gumxPath).Replace("</GumProjectSave>", "<ComponentReference Name=\"Base\" /><ComponentReference Name=\"Derived\" /></GumProjectSave>");
            File.WriteAllText(gumxPath, gumx);

            ProjectLoadResult result = new ProjectLoader().Load(gumxPath);
            result.Success.ShouldBeTrue();
            ObjectFinder.Self.GumProjectSave = result.Project;
            ComponentSave derived = result.Project!.Components.Single(item => item.Name == "Derived");

            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                derived,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms, RootNamespace = "MyGame", ObjectInstantiationType = ObjectInstantiationType.FullyInCode });

            code.ShouldContain("this.Holder.Width = 111f;");
            code.ShouldContain("this.Holder.IsRenderTarget = true;");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
