using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace Headquarters;

public static class SettingManager
{
    public const string SettingFileName = "setting.json";
    private static readonly string DefaultSettingFilePath = Path.Combine(Profile.DefaultPath, SettingFileName);
    
    /// <summary>
    /// Jsonによる設定データ
    /// </summary>
    public struct SettingData
    {
        public static SettingData Default => new()
        {
            GlobalParameterSet = new Dictionary<string, string>
            {
                [GlobalParameter.ShowConfirmationDialogOnExecuteParameterName] = "true",
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

    public static void Save(SettingData settingData)
    {
        Save(DefaultSettingFilePath, settingData);
    }

    private static void Save(string filepath, SettingData settingData)
    {
        var str = JsonConvert.SerializeObject(settingData, Formatting.Indented);
        File.WriteAllText(filepath, str);
    }

    
    public static SettingData? Load()
    {
        return Load(DefaultSettingFilePath);
    }

    private static SettingData? Load(string filepath)
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