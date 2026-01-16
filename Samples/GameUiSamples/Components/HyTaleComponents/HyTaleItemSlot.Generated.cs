//Code for HyTaleComponents/HyTaleItemSlot (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class HyTaleItemSlot : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/HyTaleItemSlot");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/HyTaleItemSlot - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleItemSlot(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleItemSlot)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/HyTaleItemSlot", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum HasItem
    {
        True,
        False,
    }
    public enum HasDamage
    {
        True,
        False,
    }
    public enum HasMoreThanOne
    {
        True,
        False,
    }
    public enum IsHotBarItem
    {
        True,
        False,
    }

    HasItem? _hasItemState;
    public HasItem? HasItemState
    {
        get => _hasItemState;
        set
        {
            _hasItemState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("HasItem"))
                {
                    var category = Visual.Categories["HasItem"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "HasItem");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    HasDamage? _hasDamageState;
    public HasDamage? HasDamageState
    {
        get => _hasDamageState;
        set
        {
            _hasDamageState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("HasDamage"))
                {
                    var category = Visual.Categories["HasDamage"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "HasDamage");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    HasMoreThanOne? _hasMoreThanOneState;
    public HasMoreThanOne? HasMoreThanOneState
    {
        get => _hasMoreThanOneState;
        set
        {
            _hasMoreThanOneState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("HasMoreThanOne"))
                {
                    var category = Visual.Categories["HasMoreThanOne"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "HasMoreThanOne");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    IsHotBarItem? _isHotBarItemState;
    public IsHotBarItem? IsHotBarItemState
    {
        get => _isHotBarItemState;
        set
        {
            _isHotBarItemState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("IsHotBarItem"))
                {
                    var category = Visual.Categories["IsHotBarItem"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "IsHotBarItem");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime HyTaleItemSlotBackground { get; protected set; }
    public HyTaleItemIcon HyTaleItemIconInstance { get; protected set; }
    public HyTaleSlotNumber HotBarSlotNumberInstance { get; protected set; }
    public HyTaleDurabilityBar HyTaleDurabilityBarInstance { get; protected set; }
    public TextRuntime QuantityValueText { get; protected set; }
    public NineSliceRuntime HighlightNineSlice { get; protected set; }

    public string ItemName
    {
        get;
        set;
    }
    public bool IsHighlighted
    {
        get => HighlightNineSlice.Visible;
        set => HighlightNineSlice.Visible = value;
    }

    public string HotBarSlotNumber
    {
        get => HotBarSlotNumberInstance.Text;
        set => HotBarSlotNumberInstance.Text = value;
    }

    public float DurabilityRatio
    {
        get => HyTaleDurabilityBarInstance.DurabilityRatioValue;
        set => HyTaleDurabilityBarInstance.DurabilityRatioValue = value;
    }

    public int ItemStartX
    {
        get => HyTaleItemIconInstance.ItemStartX;
        set => HyTaleItemIconInstance.ItemStartX = value;
    }

    public int ItemStartY
    {
        get => HyTaleItemIconInstance.ItemStartY;
        set => HyTaleItemIconInstance.ItemStartY = value;
    }

    public string Quantity
    {
        get => QuantityValueText.Text;
        set => QuantityValueText.Text = value;
    }

    public HyTaleItemSlot(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleItemSlot()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        HyTaleItemSlotBackground = this.Visual?.GetGraphicalUiElementByName("HyTaleItemSlotBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        HyTaleItemIconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemIcon>(this.Visual,"HyTaleItemIconInstance");
        HotBarSlotNumberInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleSlotNumber>(this.Visual,"HotBarSlotNumberInstance");
        HyTaleDurabilityBarInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleDurabilityBar>(this.Visual,"HyTaleDurabilityBarInstance");
        QuantityValueText = this.Visual?.GetGraphicalUiElementByName("QuantityValueText") as global::MonoGameGum.GueDeriving.TextRuntime;
        HighlightNineSlice = this.Visual?.GetGraphicalUiElementByName("HighlightNineSlice") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
