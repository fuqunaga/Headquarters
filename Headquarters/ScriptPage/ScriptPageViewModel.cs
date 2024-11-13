using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Headquarters;

public class ScriptPageViewModel : ViewModelBase
{
    public enum Page
    {
        SelectScript,
        RunScript
    }
    
    private Page _currentPage;
    private IpListViewModel? _ipListViewModel;
    private TabParameterSet? _tabParameterSet;
    private readonly Dictionary<Script, ScriptRunViewModel> _scriptRunViewModelDictionary = new();
    private ScriptRunViewModel _currentScriptRunViewModel = new();

    public Page CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public ObservableCollection<ScriptButtonViewModel> Items { get; } = [];

    public ScriptRunViewModel CurrentScriptRunViewModel
    {
        get => _currentScriptRunViewModel;
        private set => SetProperty(ref _currentScriptRunViewModel, value);
    }


    public ScriptPageViewModel() : this(@".\Scripts")
    {
    }

    public ScriptPageViewModel(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }
        
        var filePaths = Directory.GetFiles(folderPath, "*.ps1")
            .Where(s => s.EndsWith(".ps1")) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
            .OrderBy(Path.GetFileName);

        var scripts = filePaths.Select(path =>
        {
            var script = new Script(path);
            script.Load();
            return script;
        });

        Items = new ObservableCollection<ScriptButtonViewModel>(
            scripts.Select(s => new ScriptButtonViewModel(s, OnSelectScript))
        );
    }

    public void Initialize(IpListViewModel ipListViewModel, TabParameterSet tabParameterSet, string scriptName)
    {
        _ipListViewModel = ipListViewModel;
        _tabParameterSet = tabParameterSet;

        var initialScript = Items.FirstOrDefault(scriptButtonViewModel => scriptButtonViewModel.Name == scriptName)?.Script;
        if (initialScript is not null)
        {
            OnSelectScript(initialScript);
        }
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
        CurrentPage = Page.RunScript;
    }
}