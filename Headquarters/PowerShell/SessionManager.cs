using System.Collections.Generic;
using System.Management.Automation;
using System.Security;
using System.Threading.Tasks;

namespace Headquarters
{
    public static class SessionManager
    {
        private const string CreateSessionString =
            """
            param($ComputerName,$cred)
            New-PSSession -ComputerName $ComputerName -Credential $cred
            """;


        public static async Task<PowerShellRunner.Result> CreateSession(string ipAddress, string? userName, string passwordStr, PowerShellRunner.InvokeParameter param)
        {
            var p = new PowerShellRunner.InvokeParameter()
            {
                parameters = new Dictionary<string, object>()
                {
                    { "ComputerName", ipAddress },
                    { "cred", CreateCredential(userName, passwordStr) },
                },
                cancellationToken = param.cancellationToken,
                invocationStateChanged = param.invocationStateChanged,
            };

            return await PowerShellRunner.InvokeAsync(CreateSessionString, p);
        }

        private static PSCredential CreateCredential(string? userName, string passwordStr)
        {
            var password = new SecureString();
            foreach (var c in passwordStr)
            {
                password.AppendChar(c);
            }

            return string.IsNullOrEmpty(userName)
                ? PSCredential.Empty 
                : new PSCredential(userName, password);
        }
    }
}