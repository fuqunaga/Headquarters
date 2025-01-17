using System;
using System.Collections.ObjectModel;

namespace Headquarters;

public class ScriptChainPageViewModel : ViewModelBase, IDisposable
{
    private IpListViewModel _ipListViewModel;
    private bool _isLocked;
    private string _firstScriptName = string.Empty;
    private ScriptChainData _scriptChainData = new();

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    // 最初のスクリプト名
    // 命名されていないタブの名前として使う
    public string FirstScriptName { 
        get => _firstScriptName;
        set => SetProperty(ref _firstScriptName, value);
    }
    
    public ScriptPageViewModel CurrentScriptPageViewModel { get; } = new();
    
    public ObservableCollection<ScriptPageViewModel> ScriptPageViewModels { get; } = [new()];

    public void Initialize(IpListViewModel ipListViewModel, ScriptChainData scriptChainData)
    {
        _ipListViewModel = ipListViewModel;
        _scriptChainData = scriptChainData;
        
        ResetScriptPageViewModels();
    }
    
    private void ResetScriptPageViewModels()
    {
        ScriptPageViewModels.Clear();
        foreach (var scriptData in _scriptChainData.ScriptDataList)
        {
            AddScriptPageViewModel(scriptData);
        }
    }

    private void AddScriptPageViewModel(ScriptChainData.ScriptData scriptData )
    {
        var scriptPageViewModel = new ScriptPageViewModel();
        scriptPageViewModel.Initialize(_ipListViewModel, scriptData.SelectedScriptName, scriptData.ScriptToParameterSet);
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
        throw new NotImplementedException();
    }
}