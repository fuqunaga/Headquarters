using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace Headquarters;

public class ProfileWindowViewModel : ViewModelBase
{
    private const string ProfilesDataFilePath = $"{PathSetting.DataPath}\\Profiles.json";
    
    private string _targetUrl = "";
    private string _outputText = "";
    
    
    public ObservableCollection<ProfileSourceViewModel> ProfileSources { get; } = [];
    
    public string TargetUrl
    {
        get => _targetUrl;
        set => SetProperty(ref _targetUrl, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }
    
    public ICommand ChangeProfileCommand { get; }

    public ProfileWindowViewModel()
    {
        LoadDataFile();
        AddManualUrlProfileSource();
        ChangeProfileCommand = new DelegateCommand(obj =>
        {
            if (obj is string url)
            {
                ChangeProfile(url);
            }
        });
    }

 
    private void LoadDataFile()
    {
        if (!File.Exists(ProfilesDataFilePath))
        {
            return;
        }

        var str = File.ReadAllText(ProfilesDataFilePath);
        var profilesData = JsonConvert.DeserializeObject<ProfilesData>(str);
        if (profilesData == null)
        {
            return;
        }

        foreach (var profileSourceData in profilesData.ProfileSources)
        {
            ProfileSources.Add(new ProfileSourceViewModel(){
                HelpFirstLine = profileSourceData.Name,
                HelpDetail = profileSourceData.Description ?? "",
                Url = profileSourceData.Url,
                IsEnabled = false
            });
        }
    }
    
    private void AddManualUrlProfileSource()
    {
        ProfileSources.Add(new ProfileSourceViewModel()
        {
            HelpFirstLine = "Git URL",
            HelpDetail = "URLを手動で入力",
            Url = "",
            IsEnabled = true
        });
    }


    private async void ChangeProfile(string targetUrl)
    {
        var labelDialogViewModel = new LabelDialogViewModel()
        {
            Title = "Change Profile",
            Text = "Profileを変更しますか？\n\n現在のパラメーターはすべて上書きされます\nあとでバックアップから復元できます",
            OkButtonContent = "Change",
        };
        
        var ok = await DialogService.ShowDialog(labelDialogViewModel, "ProfileWindowDialog");
        if (!ok)
        {
            return;
        }
        
        OutputText = "";
        var success = await Profile.Change(targetUrl, AddMessage);
        AddMessage(success ? "Profileを変更しました" : "Profileの変更に失敗しました");

        Console.WriteLine(Application.Current.ShutdownMode);
    }

    private void AddMessage(string message)
    {
        var lineBreak = string.IsNullOrEmpty(OutputText) ? "" : "\n";
        OutputText += $"{lineBreak}{message}";
    }
}