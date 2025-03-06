using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        ValidateProfileFolder();
        LoadSettings();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        SaveSettings();
    }

    // プロファイルフォルダがアクセス出来ない場合作り直す
    // シンボリックリンクでターゲットが削除された場合を想定
    private static void ValidateProfileFolder()
    {
        const string profilePath = Profile.DefaultPath;

        if (SymbolicLinkService.IsMissingTargetSymbolicLink(profilePath))
        {
            Directory.Delete(profilePath, true);

            MessageBox.Show($"""
                            {profilePath} にアクセスできなかったため削除しました
                            シンボリックリンク元のフォルダが変更された可能性があります
                            """, "Profileフォルダエラー");
        }
        
        const string scriptPath = Profile.ScriptsFolderPath;
        if (SymbolicLinkService.IsMissingTargetSymbolicLink(scriptPath))
        {
            Directory.Delete(scriptPath, true);
            
            MessageBox.Show($"""
                            {scriptPath} にアクセスできなかったため削除しました
                            シンボリックリンク元のフォルダが変更された可能性があります
                            """, "Scriptsフォルダエラー");
        }
        
        // Scriptsフォルダまで作成しておく
        // 自動的にProfileフォルダも作成される
        if(!Directory.Exists(scriptPath))
        {
            Directory.CreateDirectory(scriptPath);
        }
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