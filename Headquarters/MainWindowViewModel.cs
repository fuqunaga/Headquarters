using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Headquarters;

public class MainWindowViewModel : ViewModelBase
{
    private bool _isSettingPageOpen;
    
    
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
    }

    public void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
    }


    private void LoadSettings()
    {
        var settingData = SettingManager.Load(".\\setting.json")
                          ?? SettingManager.SettingData.Default;

        var globalParameterSet = new ParameterSet(settingData.GlobalParameterSet);
        SettingPageViewModel.SetParameterSet(globalParameterSet);

        foreach (var data in settingData.MainTabDataList)
        {
            TabItems.Add(new MainTabViewModel(data));
        }
    }
    
    private void SaveSettings()
    {
        var settingData = new SettingManager.SettingData()
        {
            GlobalParameterSet = SettingPageViewModel.GetParameterSet()?.Parameters ?? new Dictionary<string, string>(),
            MainTabDataList = TabItems.Select(vm => vm.CreateMainTabData()).ToList()
        };
            
        SettingManager.Instance.Save(".\\setting.json", settingData);
    }
}