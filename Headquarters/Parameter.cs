namespace Headquarters
{
    public class Parameter(string name)
    {
        public string Name { get; } = name;
        public string Value
        {
            get => IsDependIp ? "On IP List" : ParameterManager.Instance.Get(Name)?.ToString() ?? "";
            set
            {
                if (IsDependIp) return;
                ParameterManager.Instance.Set(Name, value);
            }
        }

        public bool IsIndependentIp => !IsDependIp;

        public bool IsDependIp => false;//IpListDataGridViewModel.Instance.Contains(Name);
    }
}
