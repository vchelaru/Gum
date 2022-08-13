using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.FileWatchPlugin
{
    public class FileWatchViewModel : ViewModel
    {
        public string NumberOfFilesToFlush
        {
            get => Get<string>();
            set => Set(value);
        }
        public string TimeToNextFlush
        {
            get => Get<string>();
            set => Set(value);
        }
        public string NextFilesToFlush
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
