using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Headquarters;


public class ScriptResult
{
    public required string name;
    public PSInvocationStateInfo? info;
    public PowerShellRunner.Result? result;

    public override string ToString()
    {
        var prefix = (result == null) ? "" : (result.IsSucceed ? "✔" : "⚠");
        var label = $"{prefix} {name}: {info?.State}";
        var resultStr = GetResultString();

        return StringJoinWithoutNullOrEmpty("\n", label, resultStr);
    }

    private string GetResultString()
    {
        if (result == null) return "";
        
        var objString = ListToString(result.objs);
        var errString = ListToString(result.errors);

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