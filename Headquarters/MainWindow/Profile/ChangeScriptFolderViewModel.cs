using System.Windows.Input;

namespace Headquarters;

public class ChangeScriptFolderViewModel : ViewModelBase, IHelpTextBlockViewModel, ITextBoxWithOpenFileButtonViewModel
{
    public string HelpFirstLine => "スクリプトフォルダの変更";

    public string HelpDetail => """
                                スクリプトを参照するフォルダを変更します
                                主に開発用でGitのワーキングディレクトリなど指定することができます
                                """;
    
    public string Value { get; set; } = Profile.CurrentScriptsFolderPath;
    public bool ShowOpenFileButton => true;
    public ICommand OpenFileCommand { get; }
    
    public ICommand ChangeScriptFolderCommand { get; }
    
    public ChangeScriptFolderViewModel()
    {
        OpenFileCommand = new DelegateCommand(_ => { }, _ => ShowOpenFileButton);
        ChangeScriptFolderCommand = new DelegateCommand(_ => { }, null);
    }


}