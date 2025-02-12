using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;

namespace Headquarters;

public class ProfileWindowViewModel : ViewModelBase
{
    private const string ProfilesDataFilePath = $"{PathSetting.DataPath}\\profiles.json";
    private string _outputText = "";
    
    
    public ObservableCollection<ProfileSourceViewModel> ProfileSources { get; } = [];
    public BackupProfileSourceViewModel BackupProfileSource { get; } = new();
    

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }
    
    public ICommand ChangeProfileCommand { get; }
    public ICommand RestoreBackupCommand { get; }

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

        RestoreBackupCommand = new DelegateCommand(_ => RestoreBackup());
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
                IsReadOnly = true
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
            IsReadOnly = false
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
        
        await ChangeProfile(labelDialogViewModel, () => Profile.ChangeProfileByUrl(targetUrl, AddMessage));
    }

    
    private async void RestoreBackup()
    {
        var labelDialogViewModel = new LabelDialogViewModel()
        {
            Title = "Restore Backup",
            Text = "Profileのバックアップを復元しますか？\n\n現在のパラメーターはすべて上書きされます\nあとでバックアップから復元できます",
            OkButtonContent = "Restore",
        };
        
        await ChangeProfile(labelDialogViewModel, () =>
        {
            var success = Profile.RestoreBackup(BackupProfileSource.SelectedBackupName, AddMessage);
            return Task.FromResult(success);
        });
    }

    private async Task ChangeProfile(LabelDialogViewModel dialogViewModel, Func<Task<bool>> profileAction)
    {
        var ok = await DialogService.ShowDialog(dialogViewModel, "ProfileWindowDialog");
        if (!ok)
        {
            return;
        }
    
        OutputText = "";
        var success = await profileAction();
        AddMessage(success ? "Profileを変更しました" : "Profileの変更に失敗しました");
        
        BackupProfileSource.Refresh();
    }

    
    private void AddMessage(string message)
    {
        var lineBreak = string.IsNullOrEmpty(OutputText) ? "" : "\n";
        OutputText += $"{lineBreak}{message}";
    }
}