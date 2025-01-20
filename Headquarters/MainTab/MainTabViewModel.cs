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
        // CS4014: Because this call is not awaited, execution of the current method continues before the call is completed.
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs4014?redirectedfrom=MSDN
        RenameTabCommand = new DelegateCommand((_) => { var suppressWarning = RenameTab(); });
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        CloseTabCommand = new DelegateCommand(_ => CloseTab(), _ => !IsLocked);
        
        IpListViewModel.DataGridViewModel.Items = data.CreateIpListDataTable();
        
        ScriptChainPageViewModel = new ScriptChainPageViewModel(IpListViewModel, data.ScriptChainData);
        ScriptChainPageViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ScriptChainPageViewModel.FirstScriptName))
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

    public void Dispose()
    {
        ScriptChainPageViewModel.Dispose();
    }
    
    private void UpdateHeader()
    {
        // NameがあるならHeaderは更新しない
        if (!string.IsNullOrEmpty(Name)) return;
        
        var firstScriptName = ScriptChainPageViewModel.FirstScriptName;
        Header = string.IsNullOrEmpty(firstScriptName)
            ? "Select Script"
            : firstScriptName;
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
    
    private async Task RenameTab()
    {
        var viewModel =  new NameDialogViewModel()
        {
            Title = "Rename Tab",
            OkButtonContent = "Rename",
            Name = Name
        };
        
        var (success, newName) = await NameDialogService.ShowDialog(viewModel, allowEmpty: true);
        if (success)
        {
            Name = newName;
        }
        
        UpdateHeader();
    }
    
    private void CloseTab()
    {
        TabablzControl.CloseItem(this);
        Dispose();
    }
}