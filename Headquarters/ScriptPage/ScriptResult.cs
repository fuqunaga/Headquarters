using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Headquarters;


public class ScriptResult(string name)
{
    public event Action? onPropertyChanged;
    
    private PSInvocationStateInfo? _info;
    private PowerShellRunner.Result? _result;

    
    public PSInvocationStateInfo? Info
    {
        get => _info;
        set
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
    
    public string Label => $"{name}: {Info?.State}";

    public string GetResultString()
    {
        if (Result == null) return "";
        
        var objString = ListToString(Result.objs);
        var errString = ListToString(Result.errors);

        return StringJoinWithoutNullOrEmpty("\n", objString, errString);
        
        string ListToString<T>(IList<T>? collection)
        {
            return collection == null || collection.Count == 0
                ? ""
                : $"{string.Join("\n ", collection.Select(elem => $" {elem?.ToString()}"))}\n";
        }
    }
    
    private static string StringJoinWithoutNullOrEmpty(string separator, params string[] strings)
    {
        return string.Join(separator, strings.Where(str => !string.IsNullOrEmpty(str)));
    }
}