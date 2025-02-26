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
        lock (_progressRecords)
        {
            if (_progressRecords.Count == 0)
            {
                return "";
            }

            var removeIds = _progressRecords
                .Where(kv => kv.Value.RecordType == ProgressRecordType.Completed)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var id in removeIds)
            {
                _progressRecords.Remove(id);
            }

            var progressStrings = _progressRecords.Values.Select(record =>
            {
                var percent = record.PercentComplete < 0 ? "" : $"{record.PercentComplete}%";
                return $"{record.Activity} {percent}";
            });
            return StringJoinWithoutNullOrEmpty("\n", progressStrings);
        }
    }
    
    private static string StringJoinWithoutNullOrEmpty(string separator, params string[] strings)
        => StringJoinWithoutNullOrEmpty(separator, strings.AsEnumerable());
    
    private static string StringJoinWithoutNullOrEmpty(string separator, IEnumerable<string> strings)
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
        
        subscriber.onDebugAdded +=  record => AddRecordToOutputString("Debug", record);;
        subscriber.onInformationAdded +=  record => AddToOutputStringWithTag("Info", record.ToString(), "");
        subscriber.onVerboseAdded += record => AddRecordToOutputString("Verbose", record);;
        
        subscriber.onWarningAdded += record => AddRecordToOutputString("Warning", record);
        subscriber.onErrorAdded += (record) => AddToOutputStringWithTag("Error",
            record.ToString(),
            record.InvocationInfo?.PositionMessage ?? ""
        );
    
        
        subscriber.onProgressAdded += (progressRecord) =>
        {
            lock (_progressRecords)
            {
                _progressRecords[progressRecord.ActivityId] = progressRecord;
            }

            onPropertyChanged?.Invoke();
        };
        
        return subscriber;
        
        
        void AddRecordToOutputString<TRecord>(string tag, TRecord record)
            where TRecord : InformationalRecord
        {
            var message = record.Message;
            var positionMessage = record.InvocationInfo?.PositionMessage ?? "";
            AddToOutputStringWithTag(tag, message, positionMessage);
        }

        void AddToOutputStringWithTag(string tag, string message, string positionMessage)
        {
            positionMessage = string.IsNullOrEmpty(positionMessage) ? "" : $"\n{positionMessage}";
            AddToOutputString($"[{tag}] {message.TrimEnd('\r', '\n')}{positionMessage}");
        }
        
        void AddToOutputString(string text)
        {
            lock (subscriber)
            {
                if (string.IsNullOrEmpty(_outputString))
                {
                    _outputString = text;
                }
                else
                {
                    _outputString += $"\n{text}";
                }

                onPropertyChanged?.Invoke();
            }
        }
    }
}