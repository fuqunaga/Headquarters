using System;
using System.Windows.Input;

namespace Headquarters;

public class ScriptButtonViewModel : ViewModelBase, IDisposable
{
    private string _synopsis = "";
    private bool _hasError;
    
    public string Name  => Script.Name;
    
    public string Synopsis
    {
        get => _synopsis;
        private set => SetProperty(ref _synopsis, value);
    }
    
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }
    
    public ICommand SelectCommand { get; }

    
    public Script Script { get; }
    

    public ScriptButtonViewModel(Script script, Action<Script>? onSelected)
    {
        Script = script;
        Script.onLoad += OnUpdateScript;
        SelectCommand = new DelegateCommand(_ => onSelected?.Invoke(Script));
        
        OnUpdateScript();
    }
    
    public void Dispose()
    {
        Script.onLoad -= OnUpdateScript;
    }

    private void OnUpdateScript()
    {
        Synopsis = Script.Synopsis;
        HasError = Script.HasParseError;
    }
}