﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Dragablz;

namespace Headquarters;

public class MainTabViewModel : ViewModelBase, IDisposable
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
        
        NewTabCommand = new DelegateCommand(_ => NewTab());
        DuplicateTabCommand = new DelegateCommand(_ => DuplicateTab());
        // CS4014: Because this call is not awaited, execution of the current method continues before the call is completed.
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs4014?redirectedfrom=MSDN
        RenameTabCommand = new DelegateCommand((_) => { var suppressWarning = RenameTab(); });
        ToggleLockCommand = new DelegateCommand(_ => IsLocked = !IsLocked);
        CloseTabCommand = new DelegateCommand(_ => CloseTab(), _ => !IsLocked);
        
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

    public void Dispose()
    {
        ScriptPageViewModel.Dispose();
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