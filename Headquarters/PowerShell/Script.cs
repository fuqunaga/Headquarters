using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading.Tasks;

namespace Headquarters
{
    public class Script(string filepath)
    {
        #region static
        
        public static Script Empty => new("");
        
        public static class ReservedParameterName
        {
            public const string Session = "session";
        }
        
        #endregion


        private string _scriptString = "";
        private bool _isLoadingNeeded;
        private readonly List<string> _parameterNames = [];
        private bool _isSessionRequired;
        
        
        public string Name { get; } = Path.GetFileNameWithoutExtension(filepath);

        public List<string> ParameterNames
        {
            get
            {
                if (_isLoadingNeeded)
                {
                    Load();
                }

                return _parameterNames;
            }
        }

        private bool IsSessionRequired
        {
            get
            {
                if (_isLoadingNeeded)
                {
                    Load();
                }

                return _isSessionRequired;
            }
        }


        public void Load()
        {
            _isSessionRequired = false;
            _parameterNames.Clear();
            _isLoadingNeeded = false;
            
            if (!File.Exists(filepath)) return;
            
            _scriptString = File.ReadAllText(filepath);
            var allParameters = SearchParameters(_scriptString);
            var sessionParam = allParameters.FirstOrDefault(str => str.Equals(ReservedParameterName.Session, StringComparison.CurrentCultureIgnoreCase));
            
            _isSessionRequired = sessionParam != null;
            _parameterNames.AddRange(allParameters.Where(str => str != sessionParam));
        }

        private static List<string> SearchParameters(string scriptString)
        {
            var ast = Parser.ParseInput(scriptString, out _, out var errors);
            return errors.Length != 0 
                ? [] 
                : ast.ParamBlock.Parameters.Select(p => p.Name.ToString().TrimStart('$')).ToList();
        }

        public async Task<PowerShellRunner.Result> Run(string ipAddress, PowerShellRunner.InvokeParameter param)
        {
            if (IsSessionRequired)
            {
                param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserName, out var userNameObject);
                param.parameters.TryGetValue(ParameterManager.SpecialParamName.UserPassword, out var userPasswordObject);

                var userName = userNameObject as string ?? "";
                var userPassword = userPasswordObject as string ?? "";

                var sessionResult = await SessionManager.CreateSession(ipAddress, userName, userPassword, param);
                var session = sessionResult.objs?.FirstOrDefault()?.BaseObject;
                if (session == null)
                {
                    return sessionResult;
                }
                
                param.parameters = new Dictionary<string, object>(param.parameters)
                {
                    { ReservedParameterName.Session, session }
                };
            }

            
            return await PowerShellRunner.InvokeAsync(_scriptString, param);
        }
    }
}
