using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Headquarters
{
    class ScriptsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<ScriptViewModel> Items { get; set; } = new ObservableCollection<ScriptViewModel>();

        protected ScriptViewModel current_;
        public ScriptViewModel Current
        {
            get => current_;
            set
            {
                if (current_ != value)
                {
                    current_ = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
                }
            }
        }


        public ScriptsViewModel(params string[] dirpaths)
        {
            var filepaths = dirpaths.Where(dirpath => Directory.Exists(dirpath)).SelectMany(dirpath =>
            {
                return Directory.GetFiles(dirpath, "*.ps1")
                .Where(s => s.EndsWith(".ps1")); // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
            })
            .OrderBy(path => Path.GetFileName(path));

            var scripts = filepaths.Select(path => new Script(path));

            Items = new ObservableCollection<ScriptViewModel>(scripts.Select(s => new ScriptViewModel(s)));

            Current = Items.FirstOrDefault();
        }

        public void SetCurrent(string name)
        {
            var item = Items.Where(svm => svm.Header == name).FirstOrDefault();
            if (item != null)
            {
                item.Load();
                Current = item;
            }
        }

    }
}
