//Code for Elements/CautionLines (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Elements;
partial class CautionLines : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/CautionLines");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new CautionLines(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(CautionLines)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/CautionLines", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime LinesSprite { get; protected set; }

    public int LineAlpha
    {
        get => LinesSprite.Alpha;
        set => LinesSprite.Alpha = value;
    }


    public CautionLines(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public CautionLines() : base(new ContainerRuntime())
    {

        this.Visual.ClipsChildren = true;
        this.Visual.Height = 16f;
         
        this.Visual.Width = 128f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        LinesSprite = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        LinesSprite.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (LinesSprite.ElementSave != null) LinesSprite.AddStatesAndCategoriesRecursivelyToGue(LinesSprite.ElementSave);
        if (LinesSprite.ElementSave != null) LinesSprite.SetInitialState();
        LinesSprite.Name = "LinesSprite";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(LinesSprite);
    }
    private void ApplyDefaultVariables()
    {
        this.LinesSprite.SourceFileName = @"UISpriteSheet.png";
        this.LinesSprite.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.LinesSprite.TextureHeight = 32;
        this.LinesSprite.TextureLeft = 0;
        this.LinesSprite.TextureTop = 992;
        this.LinesSprite.TextureWidth = 1024;
        this.LinesSprite.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.LinesSprite.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.LinesSprite.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
