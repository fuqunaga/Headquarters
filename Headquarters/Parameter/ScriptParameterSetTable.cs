using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// スクリプト名とパラメータ群のテーブル
/// </summary>
public class ScriptParameterSetTable_(Dictionary<string, Dictionary<string, string>> dictionary )
{
    public ParameterSet GetScriptParameterSet(string scriptName)
    {
        if (!dictionary.TryGetValue(scriptName, out var scriptParameterDictionary))
        {
            scriptParameterDictionary = new Dictionary<string, string>();
            dictionary[scriptName] = scriptParameterDictionary;
        }

        return new ParameterSet(scriptParameterDictionary);
    }
}