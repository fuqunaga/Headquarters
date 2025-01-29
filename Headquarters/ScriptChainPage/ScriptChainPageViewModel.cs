﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace Headquarters;

public class ScriptChainPageViewModel : ViewModelBase, IDisposable
{
    private bool _isLocked;
    private bool _isAnyIpSelected;
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

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }
    
    public ICommand SelectScriptPageCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand StopCommand { get; }

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

        RunCommand = new DelegateCommand(
            _ => Run(),
            _ => CanRun()
        );
        
        StopCommand = new DelegateCommand(
            _ => Stop()
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
        return;
        
        
        void OnScriptPageViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptPageViewModel.HeaderText))
            {
                OnPropertyChanged(nameof(HeaderText));
            }
        }
    }
    
    public void Dispose()
    {
        foreach (var header in HeaderViewModels)
        {
            header.Dispose();
        }
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
        
        IsRunning = true;
        await CurrentScriptPageViewModel.CurrentScriptRunViewModel.Run();
        IsRunning = false;
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