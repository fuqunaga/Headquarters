using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Headquarters
{
    class Script
    {
        public static class IgnoreParameterName
        {
            public const string Session = "session";
        }

        public string name => Path.GetFileNameWithoutExtension(filepath);
        public List<string> paramNames { get; protected set; }

        string filepath;
        PowerShellScript psScript;

        public Script(string filepath)
        {
            this.filepath = filepath;
            Load();
        }

        public void Load()
        {
            var script = File.ReadAllText(filepath);

            psScript = new PowerShellScript(name, script);

            paramNames = SearchParameters(script);
        }

        List<string> SearchParameters(string script)
        {
            var match = Regex.Match(script, @"(?<=param\().*?(?=\))");
            return match.Value
                .Replace("$", "")
                .Replace(" ", "")
                .Split(',')
                .Where(str => string.Compare(str, IgnoreParameterName.Session, true) != 0)
                .ToList();
        }

        public PowerShellScript.Result Run(string ipAddress)
        {
            PowerShellScript.Result result;

            var dic = new Dictionary<string, object>();
            using (var rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open();

                var sessionResult = SessionManager.Instance.CreateSession(rs, ipAddress);
                var session = sessionResult.objs.FirstOrDefault()?.BaseObject;
                if (session == null)
                {
                    result = sessionResult;
                }
                else
                {
                    dic.Add("session", session);

                    result = psScript.Invoke(rs, dic);
                }
            }

            return result;
        }
    }
}
