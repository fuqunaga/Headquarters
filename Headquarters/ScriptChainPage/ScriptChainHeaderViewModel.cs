using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Headquarters;

public class ScriptChainHeaderViewModel : ViewModelBase, IDisposable
{
    private bool _isSelected;
    private bool _isRunning;
    private readonly ScriptChainPageViewModel _scriptChainPageViewModel;  

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
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
            _ =>
            {
                var headerViewModel = _scriptChainPageViewModel.InsertScriptPage(new ScriptChainData.ScriptData(), HeaderViewModels.IndexOf(this) + 1);
                _scriptChainPageViewModel.CurrentHeaderViewModel = headerViewModel;
            },
            _ => IsScriptChainEditable());
        
        DuplicateCommand = new DelegateCommand(
            _ =>
            {
                var headerViewModel = _scriptChainPageViewModel.InsertScriptPage(ScriptPageViewModel.GenerateScriptData(), HeaderViewModels.IndexOf(this) + 1);
                _scriptChainPageViewModel.CurrentHeaderViewModel = headerViewModel;
            },
            _ => IsScriptChainEditable());
        
        MoveLeftCommand = new DelegateCommand(
            _ => HeaderViewModels.Move(HeaderViewModels.IndexOf(this), HeaderViewModels.IndexOf(this) - 1),
            _ => !_scriptChainPageViewModel.IsLocked && HeaderViewModels.IndexOf(this) > 0);
        
        MoveRightCommand = new DelegateCommand(
            _ => HeaderViewModels.Move(HeaderViewModels.IndexOf(this), HeaderViewModels.IndexOf(this) + 1),
            _ => IsScriptChainEditable() && HeaderViewModels.IndexOf(this) < HeaderViewModels.Count - 1);

        CloseCommand = new DelegateCommand(
            _ => ConfirmAndCloseScriptPage(),
            _ => IsScriptChainEditable() && HeaderViewModels.Count > 1);

        
        HeaderViewModels.CollectionChanged += OnHeaderViewModelsChanged;

        return;


        bool IsScriptChainEditable() => _scriptChainPageViewModel.IsScriptChainEditable;
    }
    
    public void Dispose()
    {
        HeaderViewModels.CollectionChanged -= OnHeaderViewModelsChanged;
        ScriptPageViewModel.Dispose();
    }
    
    private async void ConfirmAndCloseScriptPage()
    {
        var viewModel = new LabelDialogViewModel()
        {
            Title = "Close",
            OkButtonContent = "Close",
            Text = ScriptPageViewModel.HeaderText,
        };
        var success = await DialogService.ShowDialog(viewModel);
        if (!success) return;
        
        var index = HeaderViewModels.IndexOf(this);
        HeaderViewModels.Remove(this);
        
        var selectIndex = Math.Min(index, HeaderViewModels.Count - 1);
        _scriptChainPageViewModel.CurrentHeaderViewModel = HeaderViewModels[selectIndex];
        
        Dispose();
    }

    private void OnHeaderViewModelsChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        OnPropertyChanged(nameof(IsMostLeft));
    }
    
    
    public async Task<ScriptRunResult> Run(int maxTaskCount, bool isStopOnError)
    {
        if (IsRunning
            || ScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.SelectScript)
        {
            return ScriptRunResult.None;
        }

        try
        {
            IsRunning = true;
            return await ScriptPageViewModel.CurrentScriptRunViewModel.Run(maxTaskCount, isStopOnError);
        }
        finally
        {
            IsRunning = false;
        }
    }
}