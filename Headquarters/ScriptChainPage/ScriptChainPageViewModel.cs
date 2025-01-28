using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Headquarters;

public class ScriptChainPageViewModel : ViewModelBase, IDisposable
{
    private bool _isLocked;
    private readonly IpListViewModel _ipListViewModel;
    private ScriptChainHeaderViewModel _currentHeaderViewModel;

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
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
    
    public ICommand SelectScriptPageCommand { get; }
    
    public ScriptPageViewModel CurrentScriptPageViewModel => CurrentHeaderViewModel.ScriptPageViewModel;
    
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


    private ScriptChainHeaderViewModel AddScriptPage(ScriptChainData.ScriptData scriptData )
        => InsertScriptPage(scriptData, HeaderViewModels.Count);
    
    public ScriptChainHeaderViewModel InsertScriptPage(ScriptChainData.ScriptData scriptData, int index)
    {
        var headerViewModel = new ScriptChainHeaderViewModel(
            new ScriptPageViewModel(_ipListViewModel, scriptData),
            this
        );
        HeaderViewModels.Insert(index, headerViewModel);
        
        return headerViewModel;
    }
    
    public void Dispose()
    {
        foreach (var header in HeaderViewModels)
        {
            header.Dispose();
        }
    }

    public ScriptChainData GenerateScriptChainData()
    {
        return new ScriptChainData()
        {
            ScriptDataList = HeaderViewModels.Select(header => header.ScriptPageViewModel.GenerateScriptData()).ToList()
        };
    }
}