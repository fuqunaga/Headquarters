using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Dragablz;

namespace Headquarters;

public class MainWindowViewModel : ViewModelBase
{
    private bool _isSettingPageOpen;
    
    public Func<IEnumerable<MainTabViewModel>>? GetOrderedTabsFunc { get; set; }
    
    public SettingPageViewModel SettingPageViewModel { get; } = new();
    public ObservableCollection<MainTabViewModel> TabItems { get;  } = [];
    public ICommand OpenSettingPageCommand { get; }

    public bool IsSettingPageOpen
    {
        get => _isSettingPageOpen;
        set => SetProperty(ref _isSettingPageOpen, value);
    }

    
    public MainWindowViewModel()
    {
        OpenSettingPageCommand = new DelegateCommand(_ => IsSettingPageOpen = true);
        SettingPageViewModel.closeRequested += () => IsSettingPageOpen = false;
        LoadSettings();
        
        MainTabViewModel.newTabEvent += NewTab;
    }

    public void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
    }


    private void LoadSettings()
    {
        var settingData = SettingManager.Load(".\\setting.json")
                          ?? SettingManager.SettingData.Default;
        
        GlobalParameter.ParameterSet = new ParameterSet(settingData.GlobalParameterSet);
        SettingPageViewModel.AddGlobalParameterViewModel();

        foreach (var data in settingData.MainTabDataList)
        {
            TabItems.Add(new MainTabViewModel(data));
        }
    }
    
    private void SaveSettings()
    {
        var settingData = new SettingManager.SettingData()
        {
            GlobalParameterSet = GlobalParameter.ParameterSet?.Parameters ?? new Dictionary<string, string>(),
            MainTabDataList = GetOrderedTabsFunc?.Invoke().Select(vm => vm.CreateMainTabData()).ToList() ?? []
        };
            
        SettingManager.Instance.Save(".\\setting.json", settingData);
    }
    
    private static void NewTab(MainTabViewModel sender)
    {
        TabablzControl.AddItem(new MainTabViewModel(), sender, AddLocationHint.After);
    }
}