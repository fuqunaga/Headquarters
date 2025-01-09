using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Headquarters;

/// <summary>
/// スクリプトに渡すクラス
/// Headquartersから提供するパラメータやメソッドを持つ
/// </summary>
public class TaskContext
{
    private PSCredential? _credential;
    
    public string IpAddress { get; }
    public string UserName { get; }
    public string UserPassword { get; }
    
    // １回の実行中にTask間で共有する空のDictionary
    // スクリプトが自由に追加削除できる
    public ConcurrentDictionary<string, object> SharedDictionary { get; }
    
    public PSCredential Credential => _credential ??= CreateCredential();
    
    public TaskContext(string ipAddress, string userName, string userPassword, ConcurrentDictionary<string, object> sharedDictionary)
    {
        IpAddress = ipAddress;
        UserName = userName;
        UserPassword = userPassword;
        SharedDictionary = sharedDictionary;
    }
    
    private PSCredential CreateCredential()
    {
        if (string.IsNullOrEmpty(UserPassword))
        {
            return PSCredential.Empty;
        }
        
        var password = new System.Security.SecureString();
        foreach (var c in UserPassword)
        {
            password.AppendChar(c);
        }
        
        return new PSCredential(UserName, password);
    }
}