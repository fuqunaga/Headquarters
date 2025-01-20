using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// ScriptChainの状態をJsonに変換するためのデータクラス
/// </summary>
public class ScriptChainData
{
    /// <summary>
    /// ScriptsPageに相当するデータクラス
    /// 選択中のスクリプトと入力済みの各スクリプトのパラメータも保存しておく
    /// </summary>
    public class ScriptData
    {
        public string ScriptName { get; set; } = "";
        public Dictionary<string, Dictionary<string, string>> ScriptToParameterSet { get; set; } = new();
     
    }
    
    public List<ScriptData> ScriptDataList { get; set; } = [];
}