using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace Headquarters;

public class SettingManager
{
    public struct SettingData
    {
        public List<MainTabModel.MainTabData> MainTabDataList { get; init; }
    }

    
    #region Static
    
    public static SettingManager Instance { get; } = new();
    
    #endregion
    
    
    private SettingManager()
    {
    }
    
    
    public void Save()
    {
        
    }

    public SettingData? Load(string filepath)
    {
        if (!File.Exists(filepath))
        {
            return null;
        }
        
        var str = File.ReadAllText(filepath);
        
        SettingData? data = null;
        try
        {
            data = JsonSerializer.Deserialize<SettingData>(str);
        }
        catch (JsonException e)
        {
        }

        return data;
    }
}