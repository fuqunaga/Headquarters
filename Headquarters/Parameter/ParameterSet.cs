using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// Dictionaryベースのパラメータ群
/// 
/// SettingsのGlobalパラメータと、
/// １つのスクリプトファイルに対応するパラメータを表す際に使用している
/// </summary>
public class ParameterSet(Dictionary<string, string> parameters)
{
    public Dictionary<string, string> Parameters => parameters;
    
    public string Get(string name) => parameters.GetValueOrDefault(name, "");
    public bool Set(string name, string value)
    {
        if (parameters.TryGetValue(name, out var oldValue))
        {
            if (oldValue == value) return false;
        }

        parameters[name] = value;
        return true;
    }
}