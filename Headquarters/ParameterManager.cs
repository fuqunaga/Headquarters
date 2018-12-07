using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

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


        public string userName
        {
            get => Get(SpecialParamName.UserName);
            set => Set(SpecialParamName.UserPassword, value);
        }

        public string userPassword
        {
            get => Get(SpecialParamName.UserPassword);
            set => Set(SpecialParamName.UserPassword, value);
        }
        

        protected Dictionary<string, string> parameters = new Dictionary<string, string>();

        public void Load(string filepath)
        {
            if (File.Exists(filepath))
            {
                var str = File.ReadAllText(filepath);

                parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
            }
        }


        public void Save(string filepath)
        {
            var str = JsonConvert.SerializeObject(parameters);
            File.WriteAllText(filepath, str);
        }


        public string Get(string name)
        {
            parameters.TryGetValue(name, out string ret);
            return ret;
        }

        public void Set(string name, string value)
        {
            parameters[name] = value;
        }
    }
}
