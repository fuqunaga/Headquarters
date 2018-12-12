namespace Headquarters
{
    class Parameter
    {
        public string Name { get; set; }
        public string Value
        {
            get { return IsDependIP ? "Depends IP List" : ParameterManager.Instance.Get(Name); }
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
            this.Name = name;
        }

        public string Get(IPParams ipParam)
        {
            return ipParam.Get(Name) ?? Value;
        }
    }
}
