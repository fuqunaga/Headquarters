using System;
using System.Management.Automation;

namespace Headquarters;

/// <summary>
/// PowerShellのStreamsを購読するクラス
/// </summary>
public class PowerShellEventSubscriber
{
    public event Action<PSInvocationStateChangedEventArgs>? onInvocationStateChanged;
    
    public event Action<PSObject>? onOutputAdded; 
    public event Action<ErrorRecord>? onErrorAdded;
    public event Action<ProgressRecord>? onProgressAdded; 
    
    public void Subscribe(PowerShell powerShell, PSDataCollection<PSObject> output)
    {
        powerShell.InvocationStateChanged += (_, args) => onInvocationStateChanged?.Invoke(args);
        
        // PSDataCollectionを直接操作するとダメそう（スレッド周り？）なのでReadAll()する
        output.DataAdded += (_, args) => onOutputAdded?.Invoke(output.ReadAll()[args.Index]);

        
        var streams = powerShell.Streams;
        // PSDataCollectionを操作するとダメそう（スレッド周り？）なのでReadAll()する
         streams.Error.DataAdded += (_, args) =>　onErrorAdded?.Invoke(streams.Error.ReadAll()[args.Index]);
         streams.Progress.DataAdded += (_, args) => onProgressAdded?.Invoke(streams.Progress.ReadAll()[args.Index]);
    }
}