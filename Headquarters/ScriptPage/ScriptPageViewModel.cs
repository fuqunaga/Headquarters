using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Headquarters;

public class ScriptPageViewModel : ViewModelBase
{
    private int _pageIndex;

    public int PageIndex
    {
        get => _pageIndex;
        set => SetProperty(ref _pageIndex, value);
    }
    
    public ObservableCollection<ScriptButtonViewModel> Items { get; }
        
    public ScriptViewModel ScriptViewModel { get; } = new();
        

    public ScriptPageViewModel() : this(@".\Scripts")
    {
    }

    public ScriptPageViewModel(string folderPath)
    {
        var filePaths = Directory.GetFiles(folderPath, "*.ps1")
            .Where(s => s.EndsWith(".ps1")) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
            .OrderBy(Path.GetFileName);

        var scripts = filePaths.Select(path => new Script(path));

        Items = new ObservableCollection<ScriptButtonViewModel>(
            scripts.Select(s => new ScriptButtonViewModel(s, OnSelectScript))
        );
    }

    private void OnSelectScript(Script script)
    {
        ScriptViewModel.SetScript(script);
        PageIndex = 1;
    }
    
}