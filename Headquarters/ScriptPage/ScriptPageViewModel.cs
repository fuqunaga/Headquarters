using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Headquarters;

public class ScriptPageViewModel : ViewModelBase
{
    private int _pageIndex;
    private IpListViewModel? _ipListViewModel;
    private TabParameterSet? _tabParameterSet;
    private readonly Dictionary<Script, ScriptRunViewModel> _scriptRunViewModelDictionary = new();
    private ScriptRunViewModel _currentScriptRunViewModel = new();

    public int PageIndex
    {
        get => _pageIndex;
        set => SetProperty(ref _pageIndex, value);
    }
    
    public ObservableCollection<ScriptButtonViewModel> Items { get; }

    public ScriptRunViewModel CurrentScriptRunViewModel
    {
        get => _currentScriptRunViewModel;
        private set => SetProperty(ref _currentScriptRunViewModel, value);
    }
    
    public IReadOnlyDictionary<Script, ScriptRunViewModel> ScriptRunViewModelDictionary => _scriptRunViewModelDictionary;
        

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

    public void Initialize(IpListViewModel ipListViewModel, TabParameterSet tabParameterSet)
    {
        _ipListViewModel = ipListViewModel;
        _tabParameterSet = tabParameterSet;
    }


    private void OnSelectScript(Script script)
    {
        if (_tabParameterSet is null)
        {
            throw new NullReferenceException("TabParameterSet is not set.");
        }

        if (!_scriptRunViewModelDictionary.TryGetValue(script, out var scriptRunViewModel))
        {
            if (_ipListViewModel is null)
            {
                throw new NullReferenceException("IpListViewModel is not set.");
            }
            
            scriptRunViewModel = new ScriptRunViewModel(
                script,
                _ipListViewModel,
                _tabParameterSet.GetScriptParameterSet(script.Name)
            );

            _scriptRunViewModelDictionary[script] = scriptRunViewModel;
        }
        else
        {
            // スクリプトを編集してる場合を想定して選択するたびにViewModelをリセットする
            scriptRunViewModel.ResetScript(_tabParameterSet.GetScriptParameterSet(script.Name));
        }
        
        CurrentScriptRunViewModel = scriptRunViewModel;
        PageIndex = 1;
    }
}