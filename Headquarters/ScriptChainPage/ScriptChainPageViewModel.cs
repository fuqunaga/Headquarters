using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace Headquarters;

public class ScriptChainPageViewModel : ViewModelBase, IDisposable
{
    #region Type Definitions
    
    public enum ScriptRunMode
    {
        SingleScript,
        ScriptChain
    }
    
    public class ScriptRunModeAndDescription(ScriptRunMode runMode, string description)
    {
        public ScriptRunMode RunMode { get; } = runMode;
        public string Description { get; } = description;
    }
    
    #endregion
    
    
    #region Static
    
    public static readonly IReadOnlyList<ScriptRunModeAndDescription> RunModeAndDescriptions =
    [
        new(ScriptRunMode.ScriptChain, "すべてのスクリプトを連続して実行"),
        new(ScriptRunMode.SingleScript, "選択中のスクリプトのみ実行")
    ];
    
    #endregion
    
    
    private bool _isLocked;
    private bool _isAnyIpSelected;
    private ScriptRunMode _runMode;
    private int _maxTaskCount = 100;
    private bool _isStopOnError = true;
    private bool _isRunning;
    private readonly IpListViewModel _ipListViewModel;
    private ScriptChainHeaderViewModel _currentHeaderViewModel = null!;

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }
    
    private bool IsAnyIpSelected
    {
        get => _isAnyIpSelected;
        set => SetProperty(ref _isAnyIpSelected, value);
    }

        
    public string HeaderText => CurrentScriptPageViewModel.HeaderText;
    
    public ScriptChainHeaderViewModel CurrentHeaderViewModel
    {
        get => _currentHeaderViewModel;
        set
        {
            var oldValue = _currentHeaderViewModel;
            if (SetProperty(ref _currentHeaderViewModel, value))
            {
                OnCurrentHeaderViewModelChanged(_currentHeaderViewModel, oldValue);
            }
        }
    }

    #region Binding Properties
    public ObservableCollection<ScriptChainHeaderViewModel> HeaderViewModels { get; }
    public ScriptPageViewModel CurrentScriptPageViewModel => CurrentHeaderViewModel.ScriptPageViewModel;

    public bool CanRunScriptChain => HeaderViewModels.Count >= 2
                                     && HeaderViewModels.All(header => header.ScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.RunScript);
                                         
    
    public ScriptRunMode RunMode
    {
        get => _runMode;
        set => SetProperty(ref _runMode, value);
    }
    
    public bool IsStopOnError
    {
        get => _isStopOnError;
        set => SetProperty(ref _isStopOnError, value);
    }

    public int MaxTaskCount
    {
        get => _maxTaskCount;
        set => SetProperty(ref _maxTaskCount, value);
    }
    
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }
    
    
    public ICommand ReturnPageCommand { get; }
    public ICommand SelectScriptPageCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand StopCommand { get; }
    
    public ICommand OpenScriptFolderCommand { get; }
    public ICommand OpenScriptFileCommand { get; }

    #endregion
    

    public ScriptChainPageViewModel(IpListViewModel ipListViewModel, ScriptChainData scriptChainData)
    {
        _ipListViewModel = ipListViewModel;

        SelectScriptPageCommand = new DelegateCommand(header =>
            {
                if (header is ScriptChainHeaderViewModel scriptChainHeaderViewModel)
                {
                    CurrentHeaderViewModel = scriptChainHeaderViewModel;
                }
            },
            header => header is ScriptChainHeaderViewModel scriptChainHeaderViewModel &&
                      scriptChainHeaderViewModel != CurrentHeaderViewModel);

        ReturnPageCommand = new DelegateCommand(
            _ => CurrentScriptPageViewModel.CurrentPage = ScriptPageViewModel.Page.SelectScript,
            _ => !IsLocked && !IsRunning && CurrentScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.RunScript
        );
        
        RunCommand = new DelegateCommand(
            _ => Run(),
            _ => CanRun()
        );
        
        StopCommand = new DelegateCommand(
            _ => Stop()
        );
        
        OpenScriptFolderCommand = new DelegateCommand(
            _ => CurrentScriptPageViewModel.OpenScriptFolder()
            
        );
        
        OpenScriptFileCommand = new DelegateCommand(
            _ => CurrentScriptPageViewModel.OpenScriptFile(),
            _ => CurrentScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.RunScript
        );
        
        HeaderViewModels = [];
        foreach (var scriptData in scriptChainData.ScriptDataList)
        {
            AddScriptPage(scriptData);
        }
        if (HeaderViewModels.Count == 0)
        {
            AddScriptPage(new ScriptChainData.ScriptData());
        }
        
        CurrentHeaderViewModel = HeaderViewModels[0];

        HeaderViewModels.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanRunScriptChain));
        SubscribeIpListViewModel();
    }

    private void OnCurrentHeaderViewModelChanged(ScriptChainHeaderViewModel newHeaderViewModel, ScriptChainHeaderViewModel? oldHeaderViewModel)
    {
        if (oldHeaderViewModel != null)
        {
            oldHeaderViewModel.IsSelected = false;
            oldHeaderViewModel.ScriptPageViewModel.PropertyChanged -= OnScriptPageViewModelPropertyChanged;
        }
        
        newHeaderViewModel.IsSelected = true;
        newHeaderViewModel.ScriptPageViewModel.PropertyChanged += OnScriptPageViewModelPropertyChanged;
        
        OnPropertyChanged(nameof(CurrentScriptPageViewModel));
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(CanRunScriptChain));
        return;
        
        
        void OnScriptPageViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ScriptPageViewModel.HeaderText):
                    OnPropertyChanged(nameof(HeaderText));
                    break;
                case nameof(ScriptPageViewModel.CurrentPage):
                    OnPropertyChanged(nameof(CanRunScriptChain));
                    break;
            }
        }
    }
    
    public void Dispose()
    {
        foreach (var header in HeaderViewModels)
        {
            header.Dispose();
        }
        HeaderViewModels.Clear();
    }

    private void AddScriptPage(ScriptChainData.ScriptData scriptData) => InsertScriptPage(scriptData, HeaderViewModels.Count);

    public ScriptChainHeaderViewModel InsertScriptPage(ScriptChainData.ScriptData scriptData, int index)
    {
        var headerViewModel = new ScriptChainHeaderViewModel(
            new ScriptPageViewModel(_ipListViewModel, scriptData),
            this
        );
        HeaderViewModels.Insert(index, headerViewModel);
        
        return headerViewModel;
    }

    
    private void SubscribeIpListViewModel()
    {
        _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IpListDataGridViewModel.IsAllItemSelected))
            {
                UpdateIsAnyIpSelected();
            }
        };

        UpdateIsAnyIpSelected();
        return;
        
        void UpdateIsAnyIpSelected()
        {
            IsAnyIpSelected = _ipListViewModel.DataGridViewModel.IsAllItemSelected ?? true;
        }
    }
    
    private bool CanRun()
    {
        return IsAnyIpSelected
               && (CurrentScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.RunScript);
    }

    private async void Run()
    {
        if(IsRunning)
        {
            return;
        }
        
        try
        {
            IsRunning = true;
            await CurrentHeaderViewModel.Run(MaxTaskCount, IsStopOnError);
        }
        finally
        {
            IsRunning = false;
            
            // Returnボタンが更新されないので強制的に更新
            // https://stackoverflow.com/questions/1340302/wpf-how-to-force-a-command-to-re-evaluate-canexecute-via-its-commandbindings
            CommandManager.InvalidateRequerySuggested();
        }
    }
    
    private void Stop()
    {
        CurrentScriptPageViewModel.CurrentScriptRunViewModel.Stop();
    }
    

    public ScriptChainData GenerateScriptChainData()
    {
        return new ScriptChainData()
        {
            ScriptDataList = HeaderViewModels.Select(header => header.ScriptPageViewModel.GenerateScriptData()).ToList()
        };
    }
}