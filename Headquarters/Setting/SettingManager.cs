using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using MaterialDesignThemes.Wpf;


namespace Headquarters;

public class SettingManager
{
    public struct SettingData
    {
        public List<MainTabData> MainTabDataList { get; init; }
    }

    
    #region Static
    
    public static SettingManager Instance { get; } = new();
    
    #endregion
    
    
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };
    
    
    private SettingManager()
    {
    }
    
    
    public void Save(string filepath, SettingData settingData)
    {
        var str = JsonSerializer.Serialize(settingData, _serializerOptions);
        File.WriteAllText(filepath, str);
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
        catch (JsonException)
        {
            MessageBox.Show("セッティングファイルの解析に失敗しました。\n初期状態で起動します.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return data;
    }
}