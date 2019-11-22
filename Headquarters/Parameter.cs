namespace Headquarters
{
    public class Parameter
    {
        public string Name { get; set; }
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

        public bool IsDependIP => IPListViewModel.Instance.Contains(Name);



        public Parameter(string name)
        {
            Name = name;
        }

        public string Get(IPParams ipParam)
        {
            return ipParam.Get(Name) ?? Value;
        }
    }
}
