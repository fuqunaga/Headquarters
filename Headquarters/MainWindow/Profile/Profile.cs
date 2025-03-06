using System;
using System.Collections.Generic;
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
    public const string ScriptsFolderPath = $"{DefaultPath}\\{ScriptsFolderName}";
    
    private const string BackupPath = $"{DefaultPath}Backup";
    private const string ScriptsFolderName = "Scripts";
    private const string TempPath = $"{PathSetting.DataPath}\\Temp";
    
    
    public static async Task<bool> ChangeProfileByUrl(string url, Action<string>? addMessage = null)
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
            return await ChangeProfileByFolder(newProfileSourcePath, addMessage);
        }
        catch (Exception e)
        {
            addMessage?.Invoke(e.Message);
            return false;
        }
        finally
        {
            // Tempフォルダを削除する
            DeleteReadOnlyDirectory(folderPath);
        }
    }

    public static async Task<bool> ChangeProfileByFolder(string newProfileSourcePath, Action<string>? outputMessage = null, bool useSymbolicLink = false)
    {
        try
        {
            // newProfileSourcePathが存在しない場合はエラー
            if (!Directory.Exists(newProfileSourcePath))
            {
                var folderName = Path.GetFileName(newProfileSourcePath);
                var message =　File.Exists(newProfileSourcePath)
                    ? $"{folderName} はフォルダではありません"
                    : $"{folderName} フォルダが見つかりません";

                outputMessage?.Invoke(message);
                return false;
            }

            var (isValid, hasScriptsFolder) = ValidateFolderContentsAsProfile(newProfileSourcePath, outputMessage);
            if (!isValid)
            {
                return false;
            }
            
            // MainWindowを閉じる
            // このときsetting.jsonが保存される
            // MainWindow.CloseCurrentWindow();
            MainWindowViewModel.Instance.SaveAndHideWindow();

            await Task.Run(() =>
            {
                // 現在のProfileをBackupフォルダに移動する
                MoveCurrentProfileToBackup();
                
                // Profileフォルダを作成し、新しいProfileを移動する
                // .gitフォルダは隠しファイルでコピーされない。これは都合が良い
                if (hasScriptsFolder)
                {
                    if (useSymbolicLink)
                    {
                        SymbolicLinkService.CreateSymbolicLink(DefaultPath, newProfileSourcePath);
                    }
                    else
                    {
                        Directory.Move(newProfileSourcePath, DefaultPath);
                    }
                }
                else
                {
                    Directory.CreateDirectory(DefaultPath);
                    if(useSymbolicLink)
                    {
                        SymbolicLinkService.CreateSymbolicLink(ScriptsFolderPath, newProfileSourcePath);
                    }
                    else
                    {
                        Directory.Move(newProfileSourcePath, ScriptsFolderPath);
                    }
                }
            });
        }
        catch (Exception e)
        {
            outputMessage?.Invoke(e.Message);
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
        
        addMessage?.Invoke($"> {commands}");
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

        Directory.CreateDirectory(BackupPath);
        
        var backupPath = Path.Combine(BackupPath, $"Profile_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.Move(DefaultPath, backupPath);
        
        addMessage?.Invoke($"Profileを {backupPath} にバックアップしました");
    }
    
    
    private static (bool isValid, bool hasScriptsFolder) ValidateFolderContentsAsProfile(string newProfileSourcePath, Action<string>? addMessage)
    {
        var folders = Directory.GetDirectories(newProfileSourcePath).Select(Path.GetFileName);
        var hasScriptsFolder = folders.Contains(ScriptsFolderName);

        // Scriptsフォルダがない場合は直下にスクリプトがあることを期待
        if (!hasScriptsFolder)
        {
            var files = Directory.GetFiles(newProfileSourcePath).Select(Path.GetFileName);
            var hasScriptFile = files.Any(x => x.EndsWith(".ps1"));
            if(!hasScriptFile)
            {
                addMessage?.Invoke(".ps1ファイル、Scriptsフォルダどちらも見つかりません");
                return (false, hasScriptsFolder);
            }
        }
        
        return (true, hasScriptsFolder);
    }
    
    /// <summary>
    /// Recursively deletes a directory as well as any subdirectories and files. If the files are read-only, they are flagged as normal and then deleted.
    /// </summary>
    /// <param name="directory">The name of the directory to remove.</param>
    /// 
    /// https://stackoverflow.com/a/26372070/2015881
    private static void DeleteReadOnlyDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }
        
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

    // BackupPath内のフォルダを日付順に降順で取得し名前を返す
    public static IEnumerable<string> GetBackupProfileNames()
    {
        if (!Directory.Exists(BackupPath))
        {
            return [];
        }

        return Directory.GetDirectories(BackupPath)
            .Select(x => new DirectoryInfo(x))
            .OrderByDescending(x => x.CreationTime)
            .Select(x => x.Name);
    }

 
    public static async Task<bool> RestoreBackup(string backupName, Action<string>? addMessage = null)
    {
        var backupPath = Path.Combine(BackupPath, backupName);
        return await ChangeProfileByFolder(backupPath, addMessage);
    }
}