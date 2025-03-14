﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
    
    public static void DisposeAll()
    {
        foreach (var watcher in Watchers.Values)
        {
            watcher.Dispose();
        }
        
        Watchers.Clear();
    }
    
    #endregion
    
    
    private readonly string _folderPath;
    private readonly FileSystemWatcher _directoryWatcher;
    private FileSystemWatcher? _watcher;
    
    public ObservableCollection<Script> Scripts { get; } = [];
    public Dictionary<string, Script> NonExistentScriptTable { get; } = [];
    
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
        
        // イベントは別スレッドで呼ばれるのでそのままUIスレッドの処理までつなげると例外になる
        // メインスレッドで呼ぶようにする
        _directoryWatcher.Created += (_, _) => CallOnMainThread(() => OnDirectoryExistChanged(true));
        _directoryWatcher.Deleted += (_, _) => CallOnMainThread(() => OnDirectoryExistChanged(false));
        _directoryWatcher.Renamed += (_, e) => CallOnMainThread(() => OnDirectoryExistChanged(e.Name == directoryName));
        
        _directoryWatcher.EnableRaisingEvents = true;
        
        
        OnDirectoryExistChanged(Directory.Exists(folderPath));
    }

    public Script GetOrCreateNonExistentScript(string name)
    {
        var fullPath = Path.Combine(_folderPath, name + ScriptExtension);
        if (NonExistentScriptTable.TryGetValue(fullPath, out var script))
        {
            return script;
        }

        script = new Script(fullPath);
        NonExistentScriptTable.Add(fullPath, script);
        return script;
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
        
            // イベントは別スレッドで呼ばれるのでそのままUIスレッドの処理までつなげると例外になる
            // メインスレッドで呼ぶようにする
            _watcher.Changed += (_, e) => CallOnMainThread(() => OnScriptChanged(e.FullPath));
            _watcher.Created += (_, e) => CallOnMainThread(() => OnScriptCreated(e.FullPath));
            _watcher.Deleted += (_, e) => CallOnMainThread(() => OnScriptDeleted(e.FullPath));
            _watcher.Renamed += (_, e) => CallOnMainThread(() => OnScriptRenamed(e.OldFullPath, e.FullPath));
        }
        
        LoadScripts();
        _watcher.EnableRaisingEvents = true;
    }

    private void LoadScripts()
    {
        var filePaths = Directory.GetFiles(_folderPath, ScriptSearchPattern)
            .Where(s => s.EndsWith(ScriptExtension)) // GetFiles includes *.ps1*. (*.ps1~, *.ps1_, etc.)
            .OrderBy(Path.GetFileNameWithoutExtension);
        
        foreach(var script in filePaths.Select(path => new Script(path)))
        {
            Scripts.Add(script);
        }
    }
    
    private void OnScriptChanged(string filePath)
    {
        var script = Scripts.FirstOrDefault(s => s.FilePath == filePath);
        Thread.Sleep(10); // ファイルがロックされている場合があるので少し待つ
        script?.Update();
    }

    private void OnScriptCreated(string filePath)
    {
        if( NonExistentScriptTable.TryGetValue(filePath, out var script))
        {
            NonExistentScriptTable.Remove(filePath);
            script.Update();
        }
        else
        {
            script = new Script(filePath);
        }
        
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
            script.Update();
        }
    }
    
    private void OnScriptRenamed(string oldFilePath, string newFilePath)
    {
        OnScriptDeleted(oldFilePath);
        OnScriptCreated(newFilePath);
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