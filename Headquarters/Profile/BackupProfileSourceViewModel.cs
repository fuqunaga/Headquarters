using System.Collections.Generic;
using System.Linq;

namespace Headquarters;

public class BackupProfileSourceViewModel : ViewModelBase, IHelpTextBlockViewModel
{
    private IReadOnlyCollection<string>? _backupProfileNames;
    
    public string HelpFirstLine => "バックアップ";
    public string HelpDetail => "";
    
    public IEnumerable<string> BackupNames => _backupProfileNames ??= Profile.GetBackupProfileNames().ToList();
    
    public bool HasBackup => BackupNames.Any();
    public string SelectedBackupName { get; set; } = "";
    
    public void Refresh()
    {
        _backupProfileNames = null;
        SelectedBackupName = BackupNames.FirstOrDefault() ?? "";
        
        OnPropertyChanged(nameof(HasBackup));
        OnPropertyChanged(nameof(BackupNames));
        OnPropertyChanged(nameof(SelectedBackupName));
    }
}