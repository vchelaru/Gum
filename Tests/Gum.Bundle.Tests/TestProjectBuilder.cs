using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Bundle.Tests;

/// <summary>
/// Helpers for constructing in-memory <see cref="GumProjectSave"/> instances and matching
/// on-disk fixtures for walker / packer tests.
/// </summary>
internal static class TestProjectBuilder
{
    public const string DefaultProjectName = "TestProject";

    /// <summary>
    /// Creates a temp directory with the given files written into it. Returns the directory path.
    /// </summary>
    public static string CreateTempProjectDirectory(IEnumerable<(string relativePath, byte[] content)> files)
    {
        string dir = Path.Combine(Path.GetTempPath(), "GumBundleWalkerTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        foreach ((string relativePath, byte[] content) in files)
        {
            string fullPath = Path.Combine(dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string? parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllBytes(fullPath, content);
        }
        return dir;
    }

    public static GumProjectSave BuildProject(
        string projectName = DefaultProjectName,
        IEnumerable<ScreenSave>? screens = null,
        IEnumerable<ComponentSave>? components = null,
        IEnumerable<StandardElementSave>? standards = null,
        IEnumerable<BehaviorSave>? behaviors = null)
    {
        GumProjectSave project = new GumProjectSave();
        project.FullFileName = projectName + "." + GumProjectSave.ProjectExtension;

        if (screens != null)
        {
            foreach (ScreenSave screen in screens)
            {
                project.Screens.Add(screen);
                project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
            }
        }
        if (components != null)
        {
            foreach (ComponentSave component in components)
            {
                project.Components.Add(component);
                project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
            }
        }
        if (standards != null)
        {
            foreach (StandardElementSave standard in standards)
            {
                project.StandardElements.Add(standard);
                project.StandardElementReferences.Add(new ElementReference { Name = standard.Name, ElementType = ElementType.Standard });
            }
        }
        if (behaviors != null)
        {
            foreach (BehaviorSave behavior in behaviors)
            {
                project.Behaviors.Add(behavior);
                project.BehaviorReferences.Add(new BehaviorReference { Name = behavior.Name });
            }
        }

        return project;
    }

    public static ScreenSave BuildScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        EnsureDefaultState(screen);
        return screen;
    }

    public static ComponentSave BuildComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        EnsureDefaultState(component);
        return component;
    }

    public static StandardElementSave BuildStandard(string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        EnsureDefaultState(standard);
        return standard;
    }

    public static BehaviorSave BuildBehavior(string name)
    {
        return new BehaviorSave { Name = name };
    }

    public static InstanceSave AddSpriteInstance(ElementSave element, string instanceName, string sourceFileRelativePath)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Sprite", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave
        {
            Name = instanceName + ".SourceFile",
            Type = "string",
            Value = sourceFileRelativePath,
            IsFile = true,
            SetsValue = true,
        });
        return instance;
    }

    public static InstanceSave AddTextInstanceWithFontCache(
        ElementSave element,
        string instanceName,
        string fontName,
        int fontSize,
        int outlineThickness = 0,
        bool useFontSmoothing = true,
        bool isItalic = false,
        bool isBold = false)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Text", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseCustomFont", Type = "bool", Value = false, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".Font", Type = "string", Value = fontName, IsFont = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".FontSize", Type = "int", Value = fontSize, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".OutlineThickness", Type = "int", Value = outlineThickness, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseFontSmoothing", Type = "bool", Value = useFontSmoothing, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".IsItalic", Type = "bool", Value = isItalic, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".IsBold", Type = "bool", Value = isBold, SetsValue = true });
        return instance;
    }

    public static InstanceSave AddTextInstanceWithCustomFont(
        ElementSave element,
        string instanceName,
        string customFontFilePath)
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Text", ParentContainer = element };
        element.Instances.Add(instance);

        StateSave state = element.DefaultState;
        state.Variables.Add(new VariableSave { Name = instanceName + ".UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = instanceName + ".CustomFontFile", Type = "string", Value = customFontFilePath, IsFile = true, SetsValue = true });
        return instance;
    }

    private static void EnsureDefaultState(ElementSave element)
    {
        if (element.States.Count == 0)
        {
            StateSave state = new StateSave { Name = "Default", ParentContainer = element };
            element.States.Add(state);
        }
        else
        {
            element.States[0].ParentContainer = element;
        }
    }

    public static byte[] EmptyXmlBytes => System.Text.Encoding.UTF8.GetBytes("<root />");
}
