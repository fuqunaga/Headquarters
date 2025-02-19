using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using NetTools;

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
        new(ScriptRunMode.ScriptChain, "スクリプトを連続して実行"),
        new(ScriptRunMode.SingleScript, "選択中のスクリプトのみ実行")
    ];
    
    #endregion
    
    
    private bool _isLocked;
    private bool _isAnyIpSelected;
    private ScriptRunMode _runMode;
    private string _runButtonToolTip = "";
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
    
    public string RunButtonToolTip
    {
        get => _runButtonToolTip;
        private set => SetProperty(ref _runButtonToolTip, value);
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
            _ =>  Run(),
            _ => CheckCanRunAndSetRunButtonToolTip()
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
        ApplyScriptChainData(scriptChainData);
        

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
        
        // 連続実行中にSelectScriptPageCommandが更新されないので強制的に更新
        CommandManager.InvalidateRequerySuggested();
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
    
    private bool CheckCanRunAndSetRunButtonToolTip()
    {
        if (!IsAnyIpSelected)
        {
            RunButtonToolTip = "IPアドレスが選択されていません";
            return false;
        }
        
        if (CurrentScriptPageViewModel.CurrentPage != ScriptPageViewModel.Page.RunScript)
        {
            RunButtonToolTip = "スクリプトが選択されていません";
            return false;
        }

        return true;
    }

    private async void Run()
    {
        if(IsRunning)
        {
            return;
        }

        if (GlobalParameter.ShowConfirmationDialogOnExecute)
        {
            var ok = await ShowConfirmationDialog();
            if (!ok)
            {
                return;
            }
        }
        
        try
        {
            IsRunning = true;
            if (RunMode == ScriptRunMode.ScriptChain && CanRunScriptChain)
            {
                // 連続実行中はIPListのチェックも含めた編集を禁止
                _ipListViewModel.DataGridViewModel.IsEnabled = false;
                
                await RunScriptChain();
            }
            else
            {
                await RunSingleScript();
            }
        }
        finally
        {
            IsRunning = false;
            _ipListViewModel.DataGridViewModel.IsEnabled = true;
            
            // Returnボタンが更新されないので強制的に更新
            // https://stackoverflow.com/questions/1340302/wpf-how-to-force-a-command-to-re-evaluate-canexecute-via-its-commandbindings
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task<bool> ShowConfirmationDialog()
    {
        var checkedIpList = _ipListViewModel.DataGridViewModel.SelectedParams.SelectMany(ipParams =>
                IPAddressRange.TryParse(ipParams.IpString, out var range)
                    ? range.AsEnumerable().Select(ip => ip.ToString())
                    : [ipParams.IpString])
            .ToList();
        
        var viewModel = new ListDialogViewModel()
        {
            Title = "Confirm",
            Message = $"{checkedIpList.Count} 件のIPに対してスクリプトを実行します",
            OkButtonContent = "Run",
            Items = checkedIpList
        };
        
        return await DialogService.ShowDialog(viewModel);
    }

    private async Task RunScriptChain()
    {
        var cancelled = false;
        var stopWatch = Stopwatch.StartNew();
        var lastHeader = HeaderViewModels.First();

        for (var i = 0; i < HeaderViewModels.Count; i++)
        {
            var runViewModel = HeaderViewModels[i].ScriptPageViewModel.CurrentScriptRunViewModel;
            runViewModel.ClearOutput();
            runViewModel.AddOutputInformation($"スクリプト連続実行 ({(i + 1)}/{HeaderViewModels.Count})");
        }

        var headerIndex = 0;
        for (; headerIndex < HeaderViewModels.Count; headerIndex++)
        {
            var headerViewModel = HeaderViewModels[headerIndex];
            lastHeader = headerViewModel;

            // Stopが押されてたら中断
            if (!IsRunning)
            {
                cancelled = true;
                break;
            }

            CurrentHeaderViewModel = lastHeader;
            var result = await lastHeader.Run(MaxTaskCount, IsStopOnError);

            // 異常終了なら中断
            if (result != ScriptRunResult.Success)
            {
                cancelled = true;
                break;
            }
        }

        // 実行中に手動でCurrentHeaderViewModelが変更される場合もあるので
        // CurrentHeaderViewModelではなくlastHeaderにメッセージを追加
        var targetRunViewModel = lastHeader.ScriptPageViewModel.CurrentScriptRunViewModel;

        var durationMessage = $"実行時間 {stopWatch.Elapsed:hh\\:mm\\:ss\\.ff}";
        if (cancelled)
        {
            // キャンセル時は連続実行を待機している全てのスクリプトにメッセージを追加
            for (var i = headerIndex; i < HeaderViewModels.Count; i++)
            {
                HeaderViewModels[i].ScriptPageViewModel.CurrentScriptRunViewModel
                    .AddOutputInformationFailureColor($"スクリプト連続実行がキャンセルされました - {durationMessage}");
            }
        }
        else
        {
            targetRunViewModel.AddOutputInformationSuccessColor($"スクリプト連続実行が完了しました - {durationMessage}");
        }
    }
    
    private async Task RunSingleScript()
    {
        CurrentHeaderViewModel.ScriptPageViewModel.CurrentScriptRunViewModel.ClearOutput();
        await CurrentHeaderViewModel.Run(MaxTaskCount, IsStopOnError);
    }
    
    
    private void Stop()
    {
        CurrentScriptPageViewModel.CurrentScriptRunViewModel.Stop();
        IsRunning = false;
    }

    
    private void ApplyScriptChainData(ScriptChainData scriptChainData)
    {
        RunMode = scriptChainData.ScriptRunMode;
        MaxTaskCount =  Math.Max(1, scriptChainData.MaxTaskCount);
        IsStopOnError = scriptChainData.IsStopOnError;
        
        foreach (var scriptData in scriptChainData.ScriptDataList)
        {
            AddScriptPage(scriptData);
        }
        if (HeaderViewModels.Count == 0)
        {
            AddScriptPage(new ScriptChainData.ScriptData());
        }
        
        var selectedIndex = (scriptChainData.SelectedScriptIndex < HeaderViewModels.Count)
            ? scriptChainData.SelectedScriptIndex
            : 0;       
        
        CurrentHeaderViewModel = HeaderViewModels[selectedIndex];
    }

    public ScriptChainData GenerateScriptChainData()
    {
        return new ScriptChainData()
        {
            SelectedScriptIndex = HeaderViewModels.IndexOf(CurrentHeaderViewModel),
            ScriptRunMode = RunMode,
            MaxTaskCount = MaxTaskCount,
            IsStopOnError = IsStopOnError,
            
            ScriptDataList = HeaderViewModels.Select(header => header.ScriptPageViewModel.GenerateScriptData()).ToList()
        };
    }
}