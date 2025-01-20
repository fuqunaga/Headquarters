using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ScriptParameterSetTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace Headquarters;

public class ScriptPageViewModel : ViewModelBase, IDisposable
{
    public enum Page
    {
        SelectScript,
        RunScript
    }
    
    private readonly ScriptDirectoryWatcher _watcher;
    private Page _currentPage;
    private readonly IpListViewModel _ipListViewModel;
    private readonly Dictionary<Script, ScriptRunViewModel> _scriptRunViewModelDictionary = new();
    private ScriptRunViewModel _currentScriptRunViewModel = new();

    public Page CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                if ( value == Page.SelectScript )
                {
                    _ipListViewModel?.DataGridViewModel.ClearScriptParameterNames();
                }
            }
        }
    }

    public ObservableCollection<ScriptButtonViewModel> Items { get; }

    public ScriptRunViewModel CurrentScriptRunViewModel
    {
        get => _currentScriptRunViewModel;
        private set => SetProperty(ref _currentScriptRunViewModel, value);
    }

    public string CurrentScriptName => CurrentPage == Page.SelectScript
        ? ""
        : CurrentScriptRunViewModel.ScriptName;

    
    public ScriptParameterSetTable ScriptParameterSetTable { get; private set; }
   
    
    public ScriptPageViewModel(IpListViewModel ipListViewModel, ScriptChainData.ScriptData scriptData, string folderPath=@".\Scripts")
    {
        _watcher = ScriptDirectoryWatcher.GetOrCreate(folderPath);
        _watcher.Scripts.CollectionChanged += OnScriptsChanged;
        
        Items = new ObservableCollection<ScriptButtonViewModel>(
            _watcher.Scripts.Select(s => new ScriptButtonViewModel(s, OnSelectScript))
        );
        
        _ipListViewModel = ipListViewModel;
        ScriptParameterSetTable = scriptData.ScriptToParameterSet;

        var scriptName = scriptData.ScriptName;
        if (string.IsNullOrEmpty(scriptName))
        {
            return;
        }

        var initialScript = Items.FirstOrDefault(scriptButtonViewModel => scriptButtonViewModel.Name == scriptName)?.Script;
        initialScript ??= _watcher.GetOrCreateNonExistentScript(scriptName);
        OnSelectScript(initialScript);
    }
    
    
    public void Dispose()
    {
        _watcher.Scripts.CollectionChanged -= OnScriptsChanged;
        
        foreach (var item in Items)
        {
            item.Dispose();
        }
        
        foreach(var scriptRunViewModel in  _scriptRunViewModelDictionary.Values)
        {
            scriptRunViewModel.Dispose();
        }
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

    private void OnSelectScript(Script script)
    {
        if (ScriptParameterSetTable is null)
        {
            throw new NullReferenceException("ScriptParameterSetTable is not set.");
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
                CreateParameterSet(script.Name)
            );

            _scriptRunViewModelDictionary[script] = scriptRunViewModel;
        }
        
        CurrentScriptRunViewModel = scriptRunViewModel;
        CurrentPage = Page.RunScript;
        return;

        ParameterSet CreateParameterSet(string scriptName)
        {
            if (!ScriptParameterSetTable.TryGetValue(scriptName, out var scriptParameterDictionary))
            {
                scriptParameterDictionary = new Dictionary<string, string>();
                ScriptParameterSetTable[scriptName] = scriptParameterDictionary;
            }

            return new ParameterSet(scriptParameterDictionary);
        }
    }
    
    public ScriptChainData.ScriptData GenerateScriptData()
    {
        return new ScriptChainData.ScriptData
        {
            ScriptName = CurrentScriptName,
            ScriptToParameterSet = ScriptParameterSetTable
        };
    }
}