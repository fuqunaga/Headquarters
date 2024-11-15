using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Headquarters;

/// <summary>
/// スクリプトのフォルダを監視して変更を検知する
/// </summary>
public class ScriptDirectoryWatcher : IDisposable
{
    #region Static
    
    public const string ScriptExtension = ".ps1";
    public const string ScriptSearchPattern = "*.ps1";
    
    private static readonly Dictionary<string, ScriptDirectoryWatcher> Watchers = new();
    
    public static ScriptDirectoryWatcher GetOrCreate(string folderPath)
    {
        if (Watchers.TryGetValue(folderPath, out var watcher))
        {
            return watcher;
        }

        watcher = new ScriptDirectoryWatcher(folderPath);
        Watchers.Add(folderPath, watcher);
        return watcher;
    }
    
    #endregion
    
    
    private readonly string _folderPath;
    private readonly FileSystemWatcher _directoryWatcher;
    private FileSystemWatcher? _watcher;
    
    public ObservableCollection<Script> Scripts { get; } = [];
    
    private ScriptDirectoryWatcher(string folderPath)
    {
        _folderPath = folderPath;
        var parentDirectory = Directory.GetParent(folderPath);
        if (parentDirectory == null)
        {
            throw new ArgumentException(@"Invalid folder path", nameof(folderPath));
        }
        
        // フォルダの存在を監視
        var directoryName = Path.GetFileName(folderPath);
        _directoryWatcher = new FileSystemWatcher(parentDirectory.FullName, directoryName);
        _directoryWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
        
        _directoryWatcher.Created += (_, _) => CallOnMainThread(() => OnDirectoryExistChanged(true));
        _directoryWatcher.Deleted += (_, _) => CallOnMainThread(() => OnDirectoryExistChanged(false));
        _directoryWatcher.Renamed += (_, e) => CallOnMainThread(() => OnDirectoryExistChanged(e.Name == directoryName));
        
        _directoryWatcher.EnableRaisingEvents = true;
        
        
        OnDirectoryExistChanged(Directory.Exists(folderPath));
    }

    private void OnDirectoryExistChanged(bool exist)
    {
        if (!exist)
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                Scripts.Clear();
            }
            
            return;
        }

        if (_watcher == null)
        {
            _watcher = new FileSystemWatcher(_folderPath, ScriptSearchPattern);
        
            // イベントは別スレッドで呼ばれるので注意
            // Application.Current.Dispatcher.Invokeでメインスレッドで呼ぶ
            
            // _watcher.Changed += OnChanged;
            _watcher.Created += (_, e) => CallOnMainThread(() =>OnScriptCreated(e.FullPath));
            _watcher.Deleted += (_, e) => CallOnMainThread(() =>OnScriptDeleted(e.FullPath));
            // _watcher.Renamed += (_,_) => ReloadScripts();
        }
        
        LoadScripts();
        _watcher.EnableRaisingEvents = true;
    }

    private void LoadScripts()
    {
        var filePaths = Directory.GetFiles(_folderPath, ScriptSearchPattern)
            .Where(s => s.EndsWith(ScriptExtension)) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
            .OrderBy(Path.GetFileNameWithoutExtension);

        var scripts = filePaths.Select(path =>
        {
            var script = new Script(path);
            script.Load();
            return script;
        });
        
        foreach(var script in scripts)
        {
            Scripts.Add(script);
        }
    }

    private void OnScriptCreated(string filePath)
    {
        var script = new Script(filePath);
        script.Load();
        var index = Scripts.IndexOf(Scripts.FirstOrDefault(s => Comparer<string>.Default.Compare(script.Name, s.Name) < 0));
        if (index == -1)
        {
            Scripts.Add(script);
        }
        else
        {
            Scripts.Insert(index, script);
        }
    }
    
    private void OnScriptDeleted(string filePath)
    {
        var script = Scripts.FirstOrDefault(s => s.FilePath == filePath);
        if (script != null)
        {
            Scripts.Remove(script);
        }
    }
    

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Changed: {e.FullPath}");
    }
    

    private static void CallOnMainThread(Action action)
    {
        Application.Current.Dispatcher.Invoke(action);
    }

    public void Dispose()
    {
        _directoryWatcher.Dispose();
        _watcher?.Dispose();
    }
}