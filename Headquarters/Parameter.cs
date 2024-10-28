namespace Headquarters
{
    public class Parameter(string name)
    {
        public string Name { get; set; } = name;
        public string Value
        {
            get { return IsDependIP ? "On IP List" : ParameterManager.Instance.Get(Name)?.ToString(); }
            set
            {
                if ( !IsDependIP )
                    ParameterManager.Instance.Set(Name, value);
            }
        }

        public bool IsIndependentIP => !IsDependIP;

        public bool IsDependIP => false;//IpListDataGridViewModel.Instance.Contains(Name);

        public string Get(IPParams ipParam)
        {
            return ipParam.Get(Name) ?? Value;
        }
    }
}
