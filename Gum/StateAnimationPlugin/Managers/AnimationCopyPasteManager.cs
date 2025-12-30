using Gum.Mvvm;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    class CopiedData
    {
        public AnimationViewModel? CopiedAnimation;
    }

    internal static class AnimationCopyPasteManager
    {
        public static CopiedData CopiedData { get; private set; } = new CopiedData();

        internal static void Copy(AnimationViewModel viewModel)
        {
            CopiedData.CopiedAnimation = viewModel.Clone();

        }

        internal static void Paste(ElementAnimationsViewModel mainViewModel)
        {
            if(CopiedData.CopiedAnimation != null)
            {
                var whyCantPaste = mainViewModel.GetWhyAddingAnimationIsInvalid();

                if(!string.IsNullOrEmpty(whyCantPaste))
                {
                    Locator.GetRequiredService<IDialogService>().ShowMessage(whyCantPaste);
                }
                else
                {
                    var toPaste = CopiedData.CopiedAnimation.Clone();

                    toPaste.Name = CopiedData.CopiedAnimation.Name;
                    while (mainViewModel.Animations.Any(item => item.Name == toPaste.Name))
                    {
                        toPaste.Name = StringFunctions.IncrementNumberAtEnd(toPaste.Name);
                    }
                    mainViewModel.Animations.Add(toPaste);
                    mainViewModel.SelectedAnimation = toPaste;
                }
            }
        }
    }
}
