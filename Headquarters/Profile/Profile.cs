using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Headquarters;

/// <summary>
/// Headquartersのスクリプトを含む実行環境データ
/// 
/// 実態はスクリプトフォルダとSetting.jsonを含むフォルダ
/// Profileを切り替えることで特定用途用の環境を切り替える
/// </summary>
public static class Profile
{
    public const string DefaultPath =  $"{PathSetting.DataPath}\\Profile";
    private const string TempPath = $"{PathSetting.DataPath}\\Temp";
    private const string DefaultScriptsFolder = $"{DefaultPath}\\Scripts";
    

    public static async Task<bool> Change(string url, Action<string>? addMessage = null)
    {
        var folderPath = GetTemporallyPath();
        try
        {
            var (success, subfolder) = await DownloadProfile(url, folderPath, addMessage);
            if (!success)
            {
                return false;
            }

            var newProfileSourcePath = Path.Combine(folderPath, subfolder);
            // newProfileSourcePathが存在しない場合はエラー
            if (!Directory.Exists(newProfileSourcePath))
            {
                var message =　File.Exists(newProfileSourcePath)
                    ? $"{subfolder} はフォルダではありません"
                    : $"{subfolder} フォルダが見つかりません";

                addMessage?.Invoke(message);
                return false;
            }

            // MainWindowを閉じる
            // このときsetting.jsonが保存される
            // MainWindow.CloseCurrentWindow();
            MainWindowViewModel.Instance.SaveAndHideWindow();
            
            // 現在のProfileをBackupフォルダに移動する
            MoveCurrentProfileToBackup();

            // Profileフォルダを作成し、新しいProfileを移動する
            // .gitフォルダは隠しファイルでコピーされない。これは都合が良い
            Directory.CreateDirectory(DefaultPath);
            Directory.Move(newProfileSourcePath, DefaultScriptsFolder);
        }
        catch (Exception e)
        {
            addMessage?.Invoke(e.Message);
            return false;
        }
        finally
        {
            // MainWindowを表示する
            if (!MainWindowViewModel.Instance.IsWindowVisible)
            {
                MainWindowViewModel.Instance.LoadAndShowWindow();
                var profileWindow = Application.Current.Windows.OfType<ProfileWindow>().FirstOrDefault();
                profileWindow?.Focus();
            }
            
            // Tempフォルダを削除する
            DeleteReadOnlyDirectory(folderPath);
        }
        
        return true;
    }

    // UrlはUnityのGit URL拡張構文を使用する
    // https://docs.unity3d.com/ja/2022.3/Manual/upm-git.html#syntax
    private static async Task<(bool success, string subfolder)> DownloadProfile(string url, string tempPath, Action<string>? addMessage = null)
    {
        var uri = new Uri(url);
        var branchOrCommit = uri.Fragment.Replace("#", "");
        var subfolder = uri.Query.Replace("?path=", "").TrimStart('/');
        var repository = uri.GetLeftPart(UriPartial.Path);
        
        var depthOne = (branchOrCommit == "") ? "--depth=1 --single-branch" : "";
        
        // 一連のGitコマンドを1つの文字列にまとめる
        var commands = $"git clone --filter=tree:0 --no-tags {depthOne} --no-checkout --sparse {repository} {tempPath} && " +
                       $"cd {tempPath} && " +
                       $"git sparse-checkout set --no-cone {subfolder} && " +
                       $"git checkout {branchOrCommit}";
        
        var success = await RunCommand(commands, addMessage);
        
        return (success, subfolder);
    }

    // TempPathが存在するかチェックし、存在する場合は末尾に係数を追加し
    // さらに存在する場合はインクリメントしていく
    private static string GetTemporallyPath()
    {
        var path = TempPath;
        var count = 0;
        while (Directory.Exists(path))
        {
            path = $"{TempPath}{count}";
            count++;
        }

        return path;
    }


    private static async Task<bool> RunCommand(string command, Action<string>? addMessage = null)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();

        process.StartInfo = processInfo;
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                addMessage?.Invoke(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                addMessage?.Invoke(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await Task.Run(() => process.WaitForExit());

        return process.ExitCode == 0;
    }
    
    private static void MoveCurrentProfileToBackup(Action<string>? addMessage = null)
    {
        if(!Directory.Exists(DefaultPath))
        {
            return;
        }
        
        const string backupParentPath = $"{DefaultPath}Backup";
        Directory.CreateDirectory(backupParentPath);
        
        var backupPath = Path.Combine(backupParentPath, $"Profile_{DateTime.Now:yyyyMMddHHmmss}");
        Directory.Move(DefaultPath, backupPath);
        
        addMessage?.Invoke($"Profileを {backupPath} にバックアップしました");
    }
    
    /// <summary>
    /// Recursively deletes a directory as well as any subdirectories and files. If the files are read-only, they are flagged as normal and then deleted.
    /// </summary>
    /// <param name="directory">The name of the directory to remove.</param>
    /// 
    /// https://stackoverflow.com/a/26372070/2015881
    private static void DeleteReadOnlyDirectory(string directory)
    {
        foreach (var subdirectory in Directory.EnumerateDirectories(directory)) 
        {
            DeleteReadOnlyDirectory(subdirectory);
        }
        foreach (var fileName in Directory.EnumerateFiles(directory))
        {
            var fileInfo = new FileInfo(fileName)
            {
                Attributes = FileAttributes.Normal
            };
            fileInfo.Delete();
        }
        Directory.Delete(directory);
    }
}