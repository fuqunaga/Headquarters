using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading;

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


        public PowerShellScript.Result CreateSession(RunspacePool rsp, string ipAddress, string userName, string passwordStr, CancellationToken cancelToken)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("ComputerName", ipAddress);
            dic.Add("cred", CreateCredential(userName, passwordStr));

            return createSession.Invoke(rsp, dic, cancelToken);
        }

        PSCredential CreateCredential(string userName, string passwordStr)
        {
            SecureString password = new SecureString();
            passwordStr?.ToList().ForEach(c => password.AppendChar(c));

            return string.IsNullOrEmpty(userName) ? PSCredential.Empty : new PSCredential(userName, password);
        }
    }
}