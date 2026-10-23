//Code for interface (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components;
partial class @interface : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("interface") ?? throw new System.InvalidOperationException("Could not find an element named interface - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new @interface(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(@interface)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("interface", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum @class
    {
        @checked,
    }

    private @class? _classState;
    public @class? @classState
    {
        get => _classState;
        set
        {
            _classState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case @class.@checked:
                        break;
                }
            }
        }
    }

    private float _object;
    public float @object
    {
        get => _object;
        set
        {
            _object = value;
            OnPropertyChanged();
        }
    }
    public @interface(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public @interface() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
    }
    protected virtual void InitializeInstancesFromVisual()
    {
    }
    protected virtual void AssignParents()
    {
    }
    protected virtual void ApplyDefaultVariables()
    {
        this.@object = 1f;
    }
    private bool _isGeneratedVisualBuilt;
    protected override void ReactToVisualChanged()
    {
        if (_isGeneratedVisualBuilt) base.ReactToVisualChanged();
    }
    private void ReactToGeneratedVisual()
    {
        _isGeneratedVisualBuilt = true;
        ReactToVisualChanged();
    }
    partial void CustomInitialize();
}
