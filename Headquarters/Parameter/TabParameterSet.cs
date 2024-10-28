using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// メインタブ１枚に相当するPowerShellのパラメータ群
/// 複数のスクリプトごとに別々のパラメータ群を持つ
/// </summary>
public class TabParameterSet(Dictionary<string, Dictionary<string, string>> scriptParameterSetTable )
{
    public Dictionary<string, Dictionary<string, string>> ScriptParameterSetTable => scriptParameterSetTable;
    
    public ParameterSet GetScriptParameterSet(string scriptName)
    {
        if (!scriptParameterSetTable.TryGetValue(scriptName, out var scriptParameterDictionary))
        {
            scriptParameterDictionary = new Dictionary<string, string>();
            scriptParameterSetTable[scriptName] = scriptParameterDictionary;
        }

        return new ParameterSet(scriptParameterDictionary);
    }
}