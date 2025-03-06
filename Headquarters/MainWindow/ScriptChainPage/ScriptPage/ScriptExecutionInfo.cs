using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Headquarters;


public class ScriptExecutionInfo
{
    #region Type Definitions
    
    // ResultStringの構成要素
    // 単純なstringかProgressRecordかを示す
    public class OutputStringUnit
    {
        public string text = "";
        public ProgressRecord? progressRecord;
        
        public bool IsText => !IsProgress;
        public bool IsProgress => progressRecord is not null;

        public override string ToString()
        {
            if ( progressRecord is not null)
            {
                var percent = progressRecord.PercentComplete < 0 ? "" : $"{progressRecord.PercentComplete}%";
                return $"{progressRecord.Activity} {percent}";
            }

            return text;
        }

        public static implicit operator OutputStringUnit(string text) => new() { text = text };
        public static implicit operator OutputStringUnit(ProgressRecord progressRecord) => new() { progressRecord = progressRecord };
    }
    
    #endregion
    
    
    public event Action? onPropertyChanged;

    private readonly string _name;
    private PSInvocationStateInfo? _info;
    private PowerShellRunner.Result? _result;
    private string _customState = "";
    
    
    private readonly List<OutputStringUnit> _outputStringUnits = [];

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

    private string CustomState
    {
        get => _customState;
        set
        {
            _customState = value;
            onPropertyChanged?.Invoke();
        }
    }
    
    public string Label => $"{_name}: {Info?.State.ToString() ?? CustomState}";

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
        lock (_outputStringUnits)
        {
            return string.Join("\n", _outputStringUnits.Select(unit => unit.ToString()));
        }
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
        
        subscriber.onDebugAdded +=  record => AddRecordToOutputString("Debug", record);
        subscriber.onInformationAdded +=  record => AddToOutputString(record.ToString());
        subscriber.onVerboseAdded += record => AddRecordToOutputString("Verbose", record);
        
        subscriber.onWarningAdded += record => AddRecordToOutputString("Warning", record);
        subscriber.onErrorAdded += (record) => AddToOutputStringWithTag("Error",
            record.ToString(),
            record.InvocationInfo?.PositionMessage ?? ""
        );
    
        subscriber.onProgressAdded += AddProgressRecord;
        
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
            lock (_outputStringUnits)
            {
                var last = _outputStringUnits.LastOrDefault();
                if (last is { IsText: true })
                {
                    last.text += $"\n{text}";
                }
                else
                {
                    _outputStringUnits.Add(text);
                }
            }
            
            onPropertyChanged?.Invoke();
        }
        
        void AddProgressRecord(ProgressRecord progressRecord)
        {
            lock (_outputStringUnits)
            {
                var index = _outputStringUnits.FindIndex(unit => unit.IsProgress
                                                                 && unit.progressRecord?.ActivityId == progressRecord.ActivityId);

                // Completedなら削除
                if (progressRecord.RecordType == ProgressRecordType.Completed)
                {
                    if(index < 0)
                    {
                        return;
                    }
                    
                    _outputStringUnits.RemoveAt(index);

                    // 前後があり、かつ両方がテキストなら結合
                    if (0 < index && index < _outputStringUnits.Count)
                    {
                        var prev = _outputStringUnits[index - 1];
                        var next = _outputStringUnits[index];
                        if (prev.IsText && next.IsText)
                        {
                            prev.text += $"\n{next.text}";
                            _outputStringUnits.RemoveAt(index);
                        }
                    }
                }
                else if (index >= 0)
                {
                    _outputStringUnits[index] = progressRecord;
                }
                else
                {
                    _outputStringUnits.Add(progressRecord);
                }
            }
            
            onPropertyChanged?.Invoke();
        }
    }
}