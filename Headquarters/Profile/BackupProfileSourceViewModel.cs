using System.Collections.Generic;
using System.Linq;

namespace Headquarters;

public class BackupProfileSourceViewModel : ViewModelBase, IHelpTextBlockViewModel
{
    private IReadOnlyCollection<string>? _backupProfileNames;
    
    public string HelpFirstLine => "バックアップ";
    public string HelpDetail => "";

    public IEnumerable<string> BackupNames
    {
        get
        {
            if (_backupProfileNames == null)
            {
                 _backupProfileNames = Profile.GetBackupProfileNames().ToList();
                 SelectedBackupName = BackupNames.FirstOrDefault() ?? "";
            }
                
            return _backupProfileNames;
        }
    }

    public bool HasBackup => BackupNames.Any();
    public string SelectedBackupName { get; set; } = "";
    
    public void Refresh()
    {
        _backupProfileNames = null;
        
        OnPropertyChanged(nameof(HasBackup));
        OnPropertyChanged(nameof(BackupNames));
        OnPropertyChanged(nameof(SelectedBackupName));
    }
}