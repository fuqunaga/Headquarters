namespace Headquarters
{
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

        public bool IsIndependentIp => !IsDependIp;

        public bool IsDependIp => _ipListViewModel?.DataGridViewModel.Contains(Name) ?? false;
        
        public ScriptParameterViewModel(string name, IpListViewModel ipListViewModel, ParameterSet scriptParameterSet)
        {
            Name = name;
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
