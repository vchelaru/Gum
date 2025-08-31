//Code for GeneralScreen
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Screens;
partial class GeneralScreen : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("GeneralScreen");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new GeneralScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(GeneralScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("GeneralScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public Label LabelInstance { get; protected set; }

    public HorizontalAlignment LabelInstanceHorizontalAlignment
    {
        get => ((TextRuntime) LabelInstance.Visual).HorizontalAlignment;
        set => ((TextRuntime)LabelInstance.Visual).HorizontalAlignment = value;
    }

    public int? LabelInstanceMaxLettersToShow
    {
        get => ((TextRuntime) LabelInstance.Visual).MaxLettersToShow;
        set => ((TextRuntime)LabelInstance.Visual).MaxLettersToShow = value;
    }

    public int? LabelInstanceMaxNumberOfLines
    {
        get => ((TextRuntime) LabelInstance.Visual).MaxNumberOfLines;
        set => ((TextRuntime)LabelInstance.Visual).MaxNumberOfLines = value;
    }

    public TextOverflowHorizontalMode LabelInstanceTextOverflowHorizontalMode
    {
        get => ((TextRuntime) LabelInstance.Visual).TextOverflowHorizontalMode;
        set => ((TextRuntime)LabelInstance.Visual).TextOverflowHorizontalMode = value;
    }

    public TextOverflowVerticalMode LabelInstanceTextOverflowVerticalMode
    {
        get => LabelInstance.Visual.TextOverflowVerticalMode;
        set => LabelInstance.Visual.TextOverflowVerticalMode = value;
    }

    public VerticalAlignment LabelInstanceVerticalAlignment
    {
        get => ((TextRuntime) LabelInstance.Visual).VerticalAlignment;
        set => ((TextRuntime)LabelInstance.Visual).VerticalAlignment = value;
    }

    public GeneralScreen(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public GeneralScreen() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        LabelInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        LabelInstance.Name = "LabelInstance";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(LabelInstance);
    }
    private void ApplyDefaultVariables()
    {
        ((TextRuntime)this.LabelInstance.Visual).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ((TextRuntime)this.LabelInstance.Visual).MaxLettersToShow = 55;
        ((TextRuntime)this.LabelInstance.Visual).MaxNumberOfLines = 3;
        this.LabelInstance.Text = @"Hello1234";
        ((TextRuntime)this.LabelInstance.Visual).TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.EllipsisLetter;
        this.LabelInstance.Visual.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
        ((TextRuntime)this.LabelInstance.Visual).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;

    }
    partial void CustomInitialize();
}
