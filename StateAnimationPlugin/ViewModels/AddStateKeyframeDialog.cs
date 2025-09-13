using Gum.DataTypes;
using Gum.Services.Dialogs;
using System.Collections.ObjectModel;
using System.Linq;

namespace StateAnimationPlugin.ViewModels;

internal class AddStateKeyframeDialog : DialogViewModel
{
    public AnimatedKeyframeViewModel? Result { get; private set; }

    public ElementSave? ElementSave
    {
        get => Get<ElementSave?>();
        set
        {
            if (Set(value))
            {
                States.Clear();

                if (value is not null)
                {
                    States.AddRange([
                        ..value.States.Select(x => x.Name),
                        ..value.Categories.SelectMany(category => category.States.Select(state => $"{category.Name}/{state.Name}"))
                        ]);
                }
            }
        }
    }

    public ObservableCollection<string> States { get; } = [];

    public string? SelectedState
    {
        get => Get<string?>();
        set
        {
            if (Set(value))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public override bool CanExecuteAffirmative() => !string.IsNullOrWhiteSpace(SelectedState);
    
    protected override void OnAffirmative()
    {
        Result = new AnimatedKeyframeViewModel()
        {
            StateName = SelectedState!,
            HasValidState = true,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
            Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out
        };

        base.OnAffirmative();
    }
}
