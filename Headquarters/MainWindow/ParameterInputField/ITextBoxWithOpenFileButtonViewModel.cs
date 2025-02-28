using System.Windows.Input;

namespace Headquarters;

public interface ITextBoxWithOpenFileButtonViewModel
{
    public string Value { get; set; }
    public bool ShowOpenFileButton { get; }
    public ICommand OpenFileCommand { get; }
}