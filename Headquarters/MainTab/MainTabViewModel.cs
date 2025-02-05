using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Dragablz;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase, IDisposable
{
    public static Func<MainTabViewModel> Factory => () => new MainTabViewModel();
    
    private string _name = string.Empty;
    private string _header = string.Empty;
    private bool _isLocked;

    public ICommand NewTabCommand { get; }
    public ICommand DuplicateTabCommand { get; }
    public ICommand RenameTabCommand { get; }
    public ICommand ToggleLockCommand { get; private set; }
    public ICommand CloseTabCommand { get; private set; }

    public string Header
    {
        get => _header;
        private set => SetProperty(ref _header, value);
    }
    
    public bool IsLocked 
    {
        get => _isLocked;
        set { 
            SetProperty(ref _isLocked, value);
            IpListViewModel.IsLocked = value;
            ScriptChainPageViewModel.IsLocked = value;
        }
    }

    // nameはユーザーの入力して固定された名前
    // headerはnameがあればnameをなければスクリプト名などを表示する
    private string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            Header = value;
        }
    }
    
    
    public IpListViewModel IpListViewModel { get; } = new();
    public ScriptChainPageViewModel ScriptChainPageViewModel { get; }

    
    public MainTabViewModel(MainTabData data)
    {
        Name = data.Name;
        
        NewTabCommand = new DelegateCommand(_ => NewTab());
        DuplicateTabCommand = new DelegateCommand(_ => DuplicateTab());
        RenameTabCommand = new DelegateCommand((_) => RenameTab());
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        CloseTabCommand = new DelegateCommand(_ => ConfirmAndCloseTab(), _ => !IsLocked);
        
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();
        
        ScriptChainPageViewModel = new ScriptChainPageViewModel(IpListViewModel, data.ScriptChainData);
        ScriptChainPageViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ScriptChainPageViewModel.HeaderText))
            {
                UpdateHeader();
            }
        };
        
        IpListViewModel.DataGridViewModel.getScriptParameterNamesFunc = () => ScriptChainPageViewModel.CurrentScriptPageViewModel.ScriptParameterNames;
        
        UpdateHeader();
        
        // ロックのセットはScriptPageの初期化後
        IsLocked = data.IsLocked;
    }

    public MainTabViewModel() : this(new MainTabData())
    {
    }

    public void Dispose()
    {
        ScriptChainPageViewModel.Dispose();
    }
    
    private void UpdateHeader()
    {
        // NameがあるならHeaderは更新しない
        if (!string.IsNullOrEmpty(Name)) return;
        Header = ScriptChainPageViewModel.HeaderText;
    }
    
    public MainTabData CreateMainTabData()
    {
        return new MainTabData(IpListViewModel.DataGridViewModel.Items, ScriptChainPageViewModel.GenerateScriptChainData())
        {
            Name = Name,
            IsLocked = IsLocked
        };
    }
    
        
    private void NewTab()
    {
        var newItem = new MainTabViewModel();
        TabablzControl.AddItem(newItem, this, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }

    private void DuplicateTab()
    {
        var data = CreateMainTabData();
        var newItem = new MainTabViewModel(data);
        TabablzControl.AddItem(newItem, this, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }
    
    private async void RenameTab()
    {
        var viewModel =  new TextBoxDialogViewModel()
        {
            Title = "Rename Tab",
            OkButtonContent = "Rename",
            Text = Header
        };
        
        var success = await DialogService.ShowDialog(viewModel);
        if (success)
        {
            Name = viewModel.Text;
        }
        
        UpdateHeader();
    }
    
    private async void ConfirmAndCloseTab()
    {
        var viewModel = new LabelDialogViewModel()
        {
            Title = "Close Tab",
            OkButtonContent = "Close",
            Text = Header,
        };
        var success = await DialogService.ShowDialog(viewModel);
        if (!success) return;

        
        TabablzControl.CloseItem(this);
        Dispose();
    }
}