using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Headquarters;

public class ScriptPageViewModel : ViewModelBase, IDisposable
{
    public enum Page
    {
        SelectScript,
        RunScript
    }
    
    private ScriptDirectoryWatcher _watcher;
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
        _watcher = ScriptDirectoryWatcher.GetOrCreate(folderPath);
        _watcher.Scripts.CollectionChanged += OnScriptsChanged;
        
        Items = new ObservableCollection<ScriptButtonViewModel>(
            _watcher.Scripts.Select(s => new ScriptButtonViewModel(s, OnSelectScript))
        );
    }
    
    public void Dispose()
    {
        _watcher.Scripts.CollectionChanged -= OnScriptsChanged;
    }
    
    private void OnScriptsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems?[0] is Script s)
                    Items.Insert(e.NewStartingIndex, ScriptToViewModel(s));
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex >= 0)
                {
                    Items[e.OldStartingIndex].Dispose();
                    Items.RemoveAt(e.OldStartingIndex);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems?[0] is Script replaceItem)
                    Items[e.NewStartingIndex] = ScriptToViewModel(replaceItem);
                break;
            
            case NotifyCollectionChangedAction.Move:
                throw new NotImplementedException();

            case NotifyCollectionChangedAction.Reset:
                {
                    foreach (var item in Items)
                    {
                        item.Dispose();
                    }
                    Items.Clear();
                }
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