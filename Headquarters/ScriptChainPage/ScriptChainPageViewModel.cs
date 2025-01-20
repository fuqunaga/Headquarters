using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Headquarters;

public class ScriptChainPageViewModel : ViewModelBase, IDisposable
{
    private bool _isLocked;
    private readonly IpListViewModel _ipListViewModel;

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    // 最初のスクリプト名
    // 命名されていないタブの名前として使う
    public string FirstScriptName => CurrentScriptPageViewModel.CurrentScriptName;
    
    public ScriptPageViewModel CurrentScriptPageViewModel => ScriptPageViewModels[0];
    
    public ObservableCollection<ScriptPageViewModel> ScriptPageViewModels { get; }

    public ScriptChainPageViewModel(IpListViewModel ipListViewModel, ScriptChainData scriptChainData)
    {
        _ipListViewModel = ipListViewModel;

        ScriptPageViewModels = [];
        foreach (var scriptData in scriptChainData.ScriptDataList)
        {
            AddScriptPageViewModel(scriptData);
        }
        if (ScriptPageViewModels.Count == 0)
        {
            AddScriptPageViewModel(new ScriptChainData.ScriptData());
        }
    }

    private void AddScriptPageViewModel(ScriptChainData.ScriptData scriptData )
    {
        var scriptPageViewModel = new ScriptPageViewModel(_ipListViewModel, scriptData);
        ScriptPageViewModels.Add(scriptPageViewModel);
    }
    
    public void Dispose()
    {
        foreach (var scriptPageViewModel in ScriptPageViewModels)
        {
            scriptPageViewModel.Dispose();
        }
    }

    public ScriptChainData GenerateScriptChainData()
    {
        return new ScriptChainData()
        {
            ScriptDataList = ScriptPageViewModels.Select(scriptPageViewModel => scriptPageViewModel.GenerateScriptData()).ToList()
        };
    }
}