using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Security;

namespace Headquarters
{
    class SessionManager
    {
        #region Singleton
        public static SessionManager Instance { get; } = new SessionManager();

        private SessionManager()
        {
        }

        #endregion

        PowerShellScript createSession = new PowerShellScript("CreateSession",
@"
param($ComputerName,$cred)
New-PSSession -ComputerName $ComputerName -Credential $cred
");


        public PowerShellScript.Result CreateSession(string ipAddress, string userName, string passwordStr, PowerShellScript.InvokeParameter param)
        {
            var p = new PowerShellScript.InvokeParameter(param);
            p.parameters.Add("ComputerName", ipAddress);
            p.parameters.Add("cred", CreateCredential(userName, passwordStr));

            return createSession.Invoke(p);
        }

        PSCredential CreateCredential(string userName, string passwordStr)
        {
            SecureString password = new SecureString();
            passwordStr?.ToList().ForEach(c => password.AppendChar(c));

            return string.IsNullOrEmpty(userName) ? PSCredential.Empty : new PSCredential(userName, password);
        }
    }
}