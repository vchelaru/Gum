using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.ImportPlugin.ViewModel
{
    public class ImportFileViewModel : Gum.Mvvm.ViewModel
    {
        public string BrowseFileFilter
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    RefreshFilteredList();

                }
            }
        }

        public string SelectedListBoxItem
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<string> SelectedFiles
        {
            get;
            private set;
        } = new List<string>();

        public List<string> UnfilteredFileList
        {
            get;
            private set;
        } = new List<string>();

        public ObservableCollection<string> FilteredFileList
        {
            get;
            private set;
        } = new ObservableCollection<string>();

        public string ContentFolder { get; internal set; }

        public void RefreshFilteredList()
        {
            FilteredFileList.Clear();

            string toLower = SearchText?.ToLowerInvariant();

            foreach (var item in UnfilteredFileList)
            {
                var shouldAdd =
                    string.IsNullOrEmpty(toLower) ||
                    item.ToLowerInvariant().Contains(toLower);

                if (shouldAdd)
                {
                    FilteredFileList.Add(item);
                }

            }

            // pre-select the first (if there is one)
            if (FilteredFileList.Count > 0)
            {
                SelectedListBoxItem = FilteredFileList[0];
            }
        }
    }
}
