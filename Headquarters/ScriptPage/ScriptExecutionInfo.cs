using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Headquarters;


public class ScriptExecutionInfo
{
    public event Action? onPropertyChanged;

    private readonly string _name;
    private PSInvocationStateInfo? _info;
    private PowerShellRunner.Result? _result;
    private string _customState = "";
    private string _outputString = "";
    private readonly Dictionary<int, ProgressRecord> _progressRecords = [];

    public PowerShellEventSubscriber EventSubscriber { get; }
    
    public PSInvocationStateInfo? Info
    {
        get => _info;
        private set
        {
            _info = value;
            onPropertyChanged?.Invoke();
        }
    }
    
    public PowerShellRunner.Result? Result
    {
        get => _result;
        set
        {
            _result = value;
            onPropertyChanged?.Invoke();
        }
    }

    public string CustomState
    {
        get => _customState;
        set
        {
            _customState = value;
            onPropertyChanged?.Invoke();
        }
    }
    
    public string Label => $"{_name}: {Info?.State.ToString() ?? _customState}";

    public ScriptExecutionInfo(string name)
    {
        _name = name;
        EventSubscriber = CreateEventSubscriber();
    }
    
    public void SetCancelledIfNoResult()
    {
        if (Result != null)
        {
            return;
        }
        
        Result = new PowerShellRunner.Result { canceled = true };
        CustomState = "Cancelled - Not Started";
    }
    
    public string GetResultString()
    {
        return StringJoinWithoutNullOrEmpty("\n", _outputString, GetProgressString());
    }
    
    private string GetProgressString()
    {
        if(_progressRecords.Count == 0)
        {
            return "";
        }
        
        var removeIds =_progressRecords
            .Where(kv => kv.Value.RecordType == ProgressRecordType.Completed)
            .Select(kv => kv.Key)
            .ToList();
        
        foreach (var id in removeIds)
        {
            _progressRecords.Remove(id);
        }
        
        var progressStrings = _progressRecords.Values.Select(record => $"{record.Activity} {record.PercentComplete}%");
        return StringJoinWithoutNullOrEmpty("\n", progressStrings.ToArray());
    }
    
    private static string StringJoinWithoutNullOrEmpty(string separator, params string[] strings)
    {
        return string.Join(separator, strings.Where(str => !string.IsNullOrEmpty(str)));
    }
    
    private PowerShellEventSubscriber CreateEventSubscriber()
    {
        var subscriber = new PowerShellEventSubscriber();

        subscriber.onInvocationStateChanged += (arg) => Info = arg.InvocationStateInfo;
        
        subscriber.onOutputAdded += (psObj) =>
        {
            if (psObj?.BaseObject is not string str)
            {
                return;
            }
            AddToOutputString(str);
        };
        
        subscriber.onDebugAdded += AddToOutputString;
        subscriber.onInformationAdded += AddToOutputString;
        subscriber.onVerboseAdded += AddToOutputString;
        
        subscriber.onWarningAdded += (warningRecord) =>
        {
            var message = $"[Warning] {warningRecord.ToString().TrimEnd('\r', '\n')}\n{warningRecord.InvocationInfo?.PositionMessage}";
            AddToOutputString(message);
        };
        subscriber.onErrorAdded += (errorRecord) =>
        {
            var message = $"[Error] {errorRecord.ToString().TrimEnd('\r', '\n')}\n{errorRecord.InvocationInfo?.PositionMessage}";
            AddToOutputString(message);
        };

        
        subscriber.onProgressAdded += (progressRecord) =>
        {
            _progressRecords[progressRecord.ActivityId] = progressRecord;
            onPropertyChanged?.Invoke();
        };
        
        return subscriber;
        
        
        void AddToOutputString<T>(T addedObj)
        {
            var text = addedObj?.ToString() ?? "";
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (subscriber)
            {
                if (string.IsNullOrEmpty(_outputString))
                {
                    _outputString = text;
                }
                else
                {
                    _outputString += "\n" + text;
                }

                onPropertyChanged?.Invoke();
            }
        }
    }
}