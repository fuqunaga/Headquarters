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
    
    public event Action<DebugRecord>? onDebugAdded;
    public event Action<ErrorRecord>? onErrorAdded;
    public event Action<InformationRecord>? onInformationAdded;
    public event Action<ProgressRecord>? onProgressAdded; 
    public event Action<VerboseRecord>? onVerboseAdded;
    public event Action<WarningRecord>? onWarningAdded;
    
    public void Subscribe(PowerShell powerShell, PSDataCollection<PSObject> output)
    {
        powerShell.InvocationStateChanged += (_, args) => onInvocationStateChanged?.Invoke(args);
        
        // PSDataCollectionを直接操作するとダメそう（スレッド周り？）なのでReadAll()する
        output.DataAdded += (_, args) => onOutputAdded?.Invoke(output.ReadAll()[args.Index]);

        var streams = powerShell.Streams;
        streams.Debug.DataAdded += (_, args) => onDebugAdded?.Invoke(streams.Debug.ReadAll()[args.Index]);
        streams.Error.DataAdded += (_, args) =>　onErrorAdded?.Invoke(streams.Error.ReadAll()[args.Index]);
        streams.Information.DataAdded += (_, args) => onInformationAdded?.Invoke(streams.Information.ReadAll()[args.Index]);
        streams.Progress.DataAdded += (_, args) => onProgressAdded?.Invoke(streams.Progress.ReadAll()[args.Index]);
        streams.Verbose.DataAdded += (_, args) => onVerboseAdded?.Invoke(streams.Verbose.ReadAll()[args.Index]);
        streams.Warning.DataAdded += (_, args) => onWarningAdded?.Invoke(streams.Warning.ReadAll()[args.Index]);
    }
}