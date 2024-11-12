using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace Headquarters;

public class SettingManager
{
    /// <summary>
    /// Jsonによる設定データ
    /// </summary>
    public struct SettingData
    {
        public static SettingData Default => new()
        {
            GlobalParameterSet = new Dictionary<string, string>
            {
                [GlobalParameter.ConfirmationProcessCountParameterName] = "10"
            },
            MainTabDataList =
            [
                new MainTabData()
                {
                    IpList = []
                }
            ]
        };
        
        public Dictionary<string, string> GlobalParameterSet { get; set; }
        public List<MainTabData> MainTabDataList { get; set; }
    }

    
    #region Static
    
    public static SettingManager Instance { get; } = new();
    
    #endregion
    
    private SettingManager()
    {
    }
    
    
    public static void Save(string filepath, SettingData settingData)
    {
        var str = JsonConvert.SerializeObject(settingData, Formatting.Indented);
        File.WriteAllText(filepath, str);
    }

    public static SettingData? Load(string filepath)
    {
        if (!File.Exists(filepath))
        {
            return null;
        }
        
        var str = File.ReadAllText(filepath);
        
        SettingData? data = null;
        try
        {
            data = JsonConvert.DeserializeObject<SettingData>(str);
        }
        catch (JsonException)
        {
            MessageBox.Show("セッティングファイルの解析に失敗しました。\n初期状態で起動します.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return data;
    }
}