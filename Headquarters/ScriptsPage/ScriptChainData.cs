using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// ScriptChainの状態をJsonに変換するためのデータクラス
/// </summary>
public class ScriptChainData
{
    public class ScriptData
    {
        public string SelectedScriptName { get; set; } = "";
        public Dictionary<string, Dictionary<string, string>> ScriptToParameterSet { get; set; } = new();
     
    }
    
    public List<ScriptData> ScriptDataList { get; set; } = [];
}