using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Headquarters;

public class MainWindowViewModel : ViewModelBase
{
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
        var settingData = SettingManager.Instance.Load(".\\setting.json")
                          ?? new SettingManager.SettingData()
                          {
                              MainTabDataList =
                              [
                                  new MainTabData
                                  {
                                      TabHeader = "Tab0",
                                      IpList =
                                      [
                                          new Dictionary<string, string>()
                                          {
                                              { "Value1", "1" }
                                          }
                                      ]
                                  }
                              ]
                          };

        foreach (var data in settingData.MainTabDataList)
        {
            TabItems.Add(new MainTabViewModel(data));
        }
    }
    
    private void SaveSettings()
    {
        var settingData = new SettingManager.SettingData()
        {
            MainTabDataList = TabItems.Select(vm => vm.CreateMainTabData()).ToList()
        };
            
        SettingManager.Instance.Save(".\\setting.json", settingData);
    }
}