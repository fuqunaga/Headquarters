using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Headquarters
{
    public static class SessionManager
    {
        private const string CreateSessionString =
            """
            param($computerName,$credential)
            New-PSSession -ComputerName $computerName -Credential $credential
            """;


        public static async Task<PowerShellRunner.Result> CreateSession(string ipAddress, PSCredential credential, PowerShellRunner.InvokeParameter param)
        {
            var p = new PowerShellRunner.InvokeParameter(
                parameters: new Dictionary<string, object>
                {
                    { "computerName", ipAddress },
                    { "credential", credential }
                },
                cancellationToken: param.CancellationToken,
                eventSubscriber: param.EventSubscriber
            )
            {
                Runspace = param.Runspace
            };

            return await PowerShellRunner.InvokeAsync(CreateSessionString, p);
        }
    }
}