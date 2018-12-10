using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Headquarters
{
    class ScriptsViewModel
    {
        public ObservableCollection<ScriptViewModel> Items { get; set; } = new ObservableCollection<ScriptViewModel>();
        public ScriptViewModel Current { get; set; }


        public ScriptsViewModel(string dirpath)
        {
            var filepaths = Directory.GetFiles(dirpath, "*.ps1")
                .Where(s => s.EndsWith(".ps1")) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
                .ToList();

            var scripts = filepaths.Select(path => new Script(path));

            Items = new ObservableCollection<ScriptViewModel>(scripts.Select(s => new ScriptViewModel(s)));

            Current = Items.First();
        }
    }
}
