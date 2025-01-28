using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Headquarters;

public class ScriptChainHeaderViewModel : ViewModelBase, IDisposable
{
    private bool _isSelected;
    private readonly ScriptChainPageViewModel _scriptChainPageViewModel;  

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsMostLeft => _scriptChainPageViewModel.HeaderViewModels.IndexOf(this) == 0;

    
    public ICommand NewRightCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand MoveLeftCommand { get; }
    public ICommand MoveRightCommand { get; }
    public ICommand CloseCommand { get; }
    
    public ScriptPageViewModel ScriptPageViewModel { get; }

    
    private ObservableCollection<ScriptChainHeaderViewModel> HeaderViewModels => _scriptChainPageViewModel.HeaderViewModels;

    public ScriptChainHeaderViewModel(ScriptPageViewModel scriptPageViewModel, ScriptChainPageViewModel scriptChainPageViewModel)
    {
        ScriptPageViewModel = scriptPageViewModel;
        _scriptChainPageViewModel = scriptChainPageViewModel;
        
        NewRightCommand = new DelegateCommand(
            _ => _scriptChainPageViewModel.InsertScriptPage(new ScriptChainData.ScriptData(), HeaderViewModels.IndexOf(this) + 1),
            _ => !_scriptChainPageViewModel.IsLocked);
        
        DuplicateCommand = new DelegateCommand(
            _ => _scriptChainPageViewModel.InsertScriptPage(ScriptPageViewModel.GenerateScriptData(), HeaderViewModels.IndexOf(this) + 1),
            _ => !_scriptChainPageViewModel.IsLocked);
        
        MoveLeftCommand = new DelegateCommand(
            _ => HeaderViewModels.Move(HeaderViewModels.IndexOf(this), HeaderViewModels.IndexOf(this) - 1),
            _ => !_scriptChainPageViewModel.IsLocked && HeaderViewModels.IndexOf(this) > 0);
        
        MoveRightCommand = new DelegateCommand(
            _ => HeaderViewModels.Move(HeaderViewModels.IndexOf(this), HeaderViewModels.IndexOf(this) + 1),
            _ => !_scriptChainPageViewModel.IsLocked && HeaderViewModels.IndexOf(this) < HeaderViewModels.Count - 1);

        CloseCommand = new DelegateCommand(
            _ =>
            {
                HeaderViewModels.Remove(this);
                Dispose();
            },
            _ => !_scriptChainPageViewModel.IsLocked && HeaderViewModels.Count > 1);

        
        HeaderViewModels.CollectionChanged += OnHeaderViewModelsChanged;
    }
    
    public void Dispose()
    {
        HeaderViewModels.CollectionChanged -= OnHeaderViewModelsChanged;
        ScriptPageViewModel.Dispose();
    }

    private void OnHeaderViewModelsChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        OnPropertyChanged(nameof(IsMostLeft));
    }
}