using System.Linq;

namespace Headquarters
{
    /// <summary>
    /// スクリプトのパラメータを表すViewModel
    /// ParameterSetとIPListを参照し適切な値を取得・設定する
    /// </summary>
    public class ScriptParameterViewModel : ViewModelBase
    {
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
        
        public bool HasHelp => !string.IsNullOrEmpty(HelpFirstLine) || !string.IsNullOrEmpty(HelpDetail);
        
        public string HelpFirstLine { get; private set; }
        public string HelpDetail { get; private set; }

        public bool IsIndependentIp => !IsDependIp;

        public bool IsDependIp => _ipListViewModel?.DataGridViewModel.Contains(Name) ?? false;
        
        public ScriptParameterViewModel(string name, string help, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
        {
            Name = name;
            HelpFirstLine = help.Split('\n').FirstOrDefault() ?? string.Empty;
            HelpDetail = help.Split('\n').Skip(1).FirstOrDefault() ?? string.Empty;
            _ipListViewModel = ipListViewModel;
            _scriptParameterSet = scriptParameterSet;
            
            _ipListViewModel.DataGridViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IpListDataGridViewModel.Items))
                {
                    OnPropertyChanged(nameof(IsIndependentIp));
                }
            };
        }
    }
}
