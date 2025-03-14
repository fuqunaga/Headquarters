using System.Windows.Input;

namespace Headquarters;

public class ChangeScriptFolderViewModel : ViewModelBase, IHelpTextBlockViewModel, ITextBoxWithOpenFileButtonViewModel
{
    private string _value = "";
    
    public string HelpFirstLine => "ローカルフォルダ";

    public string HelpDetail => """
                                ローカルフォルダを参照します
                                主に開発用でGitの作業ディレクトリなど指定することができます
                                """;
    
    public string Value { get => _value; set => SetProperty(ref _value, value); }
    public bool ShowOpenFileButton => true;
    public ICommand OpenFileCommand { get; }
    
    
    public ChangeScriptFolderViewModel()
    {
        OpenFileCommand = new DelegateCommand(_ => OnOpenFile());
    }

    private void OnOpenFile()
    {
        if (OpenFileOrFolderDialog.ShowDialog(Value) is { } path)
        {
            Value = path;
        }
    }
}