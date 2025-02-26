using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Headquarters;

public class MainWindowViewModel : ViewModelBase
{
    public static MainWindowViewModel Instance { get; private set; } = null!;
    
    
    private readonly Window _targetWindow;
    private int _selectedTabIndex;
    
    public Func<IEnumerable<MainTabViewModel>>? GetOrderedTabsFunc { get; set; }
    public SettingPageViewModel SettingPageViewModel { get; } = new();
    public ObservableCollection<MainTabViewModel> TabItems { get;  } = [];
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    
    public bool IsWindowVisible => _targetWindow.IsVisible;


    public MainWindowViewModel(Window window)
    {
        Instance = this;
        _targetWindow = window;
        _targetWindow.Closed += OnClosed;
        
        LoadSettings();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        SaveSettings();
    }
    
    private void LoadSettings()
    {
        var settingData = SettingManager.Load() ?? SettingManager.SettingData.Default;
        
        GlobalParameter.SetParameterSet(settingData.GlobalParameterSet);
        SettingPageViewModel.InitializeWithGlobalParameter();

        TabItems.Clear();
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
            
        SettingManager.Save(settingData);
    }
    
    public void SaveAndHideWindow()
    {
        ScriptDirectoryWatcher.DisposeAll();
        SaveSettings();
        _targetWindow.Hide();
    }
    
    public void LoadAndShowWindow()
    {
        LoadSettings();
        SelectedTabIndex = 0;
        _targetWindow.Show();
    }
}