using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Headquarters
{
    public class ParameterManager
    {
        public static class SpecialParamName
        {
            public static string UserName = "UserName";
            public static string UserPassword = "UserPassword";
        }


        #region Singleton

        public static ParameterManager Instance { get; } = new ParameterManager();

        private ParameterManager()
        {
        }

        #endregion

        public static Parameter UserName => new Parameter(SpecialParamName.UserName);
        public static Parameter UserPassword => new Parameter(SpecialParamName.UserPassword);


        protected Dictionary<string, object> parameters = new Dictionary<string, object>();
        protected string filepath;

        public void Load(string filepath)
        {
            this.filepath = filepath;

            if (File.Exists(filepath))
            {
                var str = File.ReadAllText(filepath);

                parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(str);
            }
        }


        public void Save()
        {
            if (parameters.Any())
            {
                var str = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                File.WriteAllText(filepath, str);
            }
        }


        public object Get(string name)
        {
            parameters.TryGetValue(name, out var ret);
            return ret;
        }

        public void Set(string name, object value)
        {
            parameters[name] = value;
        }
    }
}
