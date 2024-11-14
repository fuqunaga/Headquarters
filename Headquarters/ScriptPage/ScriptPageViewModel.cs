using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        var watcher = ScriptDirectoryWatcher.GetOrCreate(folderPath);
        watcher.Scripts.CollectionChanged += OnScriptsChanged;

        
        Items = new ObservableCollection<ScriptButtonViewModel>(
            watcher.Scripts.Select(s => new ScriptButtonViewModel(s, OnSelectScript))
        );
        
        // if (!Directory.Exists(folderPath))
        // {
        //     return;
        // }
        //
        //
        // var filePaths = Directory.GetFiles(folderPath, "*.ps1")
        //     .Where(s => s.EndsWith(".ps1")) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
        //     .OrderBy(Path.GetFileName);
        //
        // var scripts = filePaths.Select(path =>
        // {
        //     var script = new Script(path);
        //     script.Load();
        //     return script;
        // });


    }

    private void OnScriptsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems?[0] is Script s)
                    Items.Insert(e.NewStartingIndex, ScriptToViewModel(s));
                return;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex >= 0)
                    Items.RemoveAt(e.OldStartingIndex);
                return;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems?[0] is Script replaceItem)
                    Items[e.NewStartingIndex] = ScriptToViewModel(replaceItem);
                return;
            
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return;

        ScriptButtonViewModel ScriptToViewModel(Script s) => new(s, OnSelectScript);
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