using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// １つのスクリプトファイルに対応するパラメータ群
/// IPアドレスごとに別のパラメータを持つ場合はIpParameterSetでアクセスする
/// </summary>
public class ScriptParameterSet(Dictionary<string, string> parameters)
{
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