using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Headquarters;

public class MainWindowViewModel : ViewModelBase
{
    public Func<IEnumerable<MainTabViewModel>>? GetOrderedTabsFunc { get; set; }
    public SettingPageViewModel SettingPageViewModel { get; } = new();
    public ObservableCollection<MainTabViewModel> TabItems { get;  } = [];
    
    public MainWindowViewModel()
    {
        LoadSettings();
    }

    public void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
    }


    private void LoadSettings()
    {
        var settingData = SettingManager.Load(".\\setting.json")
                          ?? SettingManager.SettingData.Default;
        
        GlobalParameter.SetParameterSet(settingData.GlobalParameterSet);
        SettingPageViewModel.InitializeWithGlobalParameter();

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
            
        SettingManager.Save(".\\setting.json", settingData);
    }
}