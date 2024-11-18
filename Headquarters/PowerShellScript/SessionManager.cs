﻿using System.Collections.Generic;
using System.Management.Automation;
using System.Security;
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


        public static async Task<PowerShellRunner.Result> CreateSession(string ipAddress, PowerShellRunner.InvokeParameter param)
        {
            var p = new PowerShellRunner.InvokeParameter(
                parameters: new Dictionary<string, object>()
                {
                    { "computerName", ipAddress },
                    { "credential", param.parameters[Script.ReservedParameterName.Credential] },
                },
                cancellationToken: param.cancellationToken,
                runspacePool: param.runspacePool,
                invocationStateChanged: param.invocationStateChanged
            );

            return await PowerShellRunner.InvokeAsync(CreateSessionString, p);
        }

        public static PSCredential CreateCredential(IReadOnlyDictionary<string, object> parameters)
        {
            var userName = parameters[GlobalParameter.UserNameParameterName] as string ?? GlobalParameter.UserName;
            var userPassword = parameters[GlobalParameter.UserPasswordParameterName] as string ?? GlobalParameter.UserName;
            
            var password = new SecureString();
            foreach (var c in userPassword)
            {
                password.AppendChar(c);
            }

            return string.IsNullOrEmpty(userName)
                ? PSCredential.Empty 
                : new PSCredential(userName, password);
        }
    }
}