using System.IO;

namespace Headquarters
{
    /// <summary>
    /// スクリプトのパラメータを表すViewModel
    /// ParameterSetとIPListを参照し適切な値を取得・設定する
    /// </summary>
    public class ScriptParameterInputFieldViewModel : ViewModelBase
    {
        public enum ButtonType
        {
            None,
            OpenFile,
            OpenDirectory,
            OpenFileOrDirectory
        }
        
        private readonly IpListViewModel _ipListViewModel;
        private readonly ParameterSet _scriptParameterSet;
        
        public string Name { get; }
        public string Value
        {
            get => _scriptParameterSet.Get(Name);
            set
            {
                if (IsDependIp) return;
                if ( _scriptParameterSet.Set(Name, value) )
                {
                    OnPropertyChanged();
                }
            }
        }
        
        public string HelpFirstLine { get; private set; }
        public string HelpDetail { get; private set; }
        public bool IsDependIp => _ipListViewModel.DataGridViewModel.Contains(Name);
        public ButtonType RightButtonType { get; private set; }
        
        public ScriptParameterInputFieldViewModel(ScriptParameter scriptParameter, string help, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
        {
            using var reader = new StringReader(help);
            
            Name = scriptParameter.Name;
            HelpFirstLine = reader.ReadLine() ?? "";
            HelpDetail = reader.ReadToEnd() ?? "";
            RightButtonType = GetButtonType(scriptParameter);
            
            _ipListViewModel = ipListViewModel;
            _scriptParameterSet = scriptParameterSet;
            
            _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IpListDataGridViewModel.Items))
                {
                    OnPropertyChanged(nameof(IsDependIp));
                }
            };
        }
        
        private static ButtonType GetButtonType(ScriptParameter scriptParameter)
        {
            foreach (var attribute in scriptParameter.Attributes)
            {
                if (attribute == typeof(FileInfo)) return ButtonType.OpenFile;
                if (attribute == typeof(DirectoryInfo)) return ButtonType.OpenDirectory;
                if (attribute == typeof(FileSystemInfo)) return ButtonType.OpenFileOrDirectory;
            }
            
            return ButtonType.None;
        }
    }
}
