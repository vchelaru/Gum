//Code for LabelContainer (Container)
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

namespace CodeGen_MonoGameForms_FullCodegen.Components;
partial class LabelContainer : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("LabelContainer");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new LabelContainer(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(LabelContainer)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("LabelContainer", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum Category1
    {
        SetTextState,
    }

    private Category1? _category1State;
    public Category1? Category1State
    {
        get => _category1State;
        set
        {
            _category1State = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case Category1.SetTextState:
                        this.LabelInstance.Text = @"Set by state";
                        break;
                }
            }
        }
    }
    public Label LabelInstance { get; protected set; }

    public string Text
    {
        get => LabelInstance.Text;
        set => LabelInstance.Text = value;
    }

    public LabelContainer(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public LabelContainer() : base(new ContainerRuntime())
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
        this.LabelInstance.Text = @"Hi there";

    }
    partial void CustomInitialize();
}
