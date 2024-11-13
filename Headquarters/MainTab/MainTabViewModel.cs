using System;
using System.Windows.Input;
using Dragablz;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase
{
    public static Func<MainTabViewModel> Factory => () => new MainTabViewModel();

    private readonly TabParameterSet _tabParameterSet;
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
            ScriptPageViewModel.CurrentScriptRunViewModel.IsLocked = value;
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
    public ScriptPageViewModel ScriptPageViewModel { get; } = new();

    
    public MainTabViewModel(MainTabData data)
    {
        Name = data.Name;
        
        
        NewTabCommand = new DelegateCommand(_ => NewTab(this));
        DuplicateTabCommand = new DelegateCommand(_ => DuplicateTab(this));
        RenameTabCommand = new DelegateCommand(_ => RenameTab());
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        CloseTabCommand = new DelegateCommand(_ => TabablzControl.CloseItem(this), _ => !IsLocked);
        
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();

        _tabParameterSet = data.CreateTabParameterSet();
        ScriptPageViewModel.Initialize(IpListViewModel, _tabParameterSet, data.ScriptName);
        ScriptPageViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ScriptPageViewModel.CurrentPage))
            {
                UpdateHeader();
            }
        };
        
        UpdateHeader();
        
        // ロックのセットはScriptPageの初期化後
        IsLocked = data.IsLocked;
    }

    public MainTabViewModel() : this(new MainTabData())
    {
    }

    private void UpdateHeader()
    {
        // NameがあるならHeaderは更新しない
        if (!string.IsNullOrEmpty(Name)) return;
        
        Header = ScriptPageViewModel.CurrentPage == ScriptPageViewModel.Page.SelectScript
            ? "Select Script"
            : ScriptPageViewModel.CurrentScriptRunViewModel.ScriptName;
    }
    
    public MainTabData CreateMainTabData()
    {
        return new MainTabData(IpListViewModel.DataGridViewModel.Items, _tabParameterSet)
        {
            Name = Name,
            IsLocked = IsLocked,
            ScriptName = ScriptPageViewModel.CurrentScriptRunViewModel.ScriptName
        };
    }
    
        
    private static void NewTab(MainTabViewModel sender)
    {
        var newItem = new MainTabViewModel();
        TabablzControl.AddItem(newItem, sender, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }

    private static void DuplicateTab(MainTabViewModel sender)
    {
        var data = sender.CreateMainTabData();
        var newItem = new MainTabViewModel(data);
        TabablzControl.AddItem(newItem, sender, AddLocationHint.After);
        TabablzControl.SelectItem(newItem);
    }
    
    private async void RenameTab()
    {
        var viewModel =  new NameDialogViewModel()
        {
            Title = "Rename Tab",
            Name = Name
        };
        
        var (success, newName) = await NameDialogService.ShowDialog(viewModel, allowEmpty: true);
        if (success)
        {
            Name = newName;
        }
        
        UpdateHeader();
    }
}