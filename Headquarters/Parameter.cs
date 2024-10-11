namespace Headquarters
{
    public class Parameter : ViewModelBase
    {
        private readonly IpListViewModel? _ipListViewModel;
        
        public string Name { get; }
        public string Value
        {
            get => ParameterManager.Instance.Get(Name)?.ToString() ?? "";
            set
            {
                if (IsDependIp) return;
                ParameterManager.Instance.Set(Name, value);
                OnPropertyChanged();
            }
        }

        public bool IsIndependentIp => !IsDependIp;

        public bool IsDependIp => _ipListViewModel?.DataGridViewModel.Contains(Name) ?? false;
        
        public Parameter(string name, IpListViewModel? ipListViewModel = null)
        {
            Name = name;
            _ipListViewModel = ipListViewModel;
            
            if (_ipListViewModel is null) return;

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
