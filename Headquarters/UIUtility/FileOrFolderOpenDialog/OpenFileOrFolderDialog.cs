using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Headquarters.NativeDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using FileDialog = Microsoft.Win32.FileDialog;

namespace Headquarters;

/// <summary>
/// ファイルかフォルダを選択するダイアログ
/// Microsoft.Win32.OpenFileDialog の実装を参考にしている
/// Microsoft.Win32.OpenFileDialog のメソッドを利用するが実態はCOMのIFileOpenDialogを利用している
/// </summary>
public class OpenFileOrFolderDialog
{
    private static readonly Dictionary<string, MethodInfo> MethodInfos = new();

    public static string? ShowDialog(string path)
    {
        var dialog = new OpenFileOrFolderDialog
        {
            Title = "Select a file or folder",
        };
        
        try
        {
            dialog.InitialDirectory = Path.GetDirectoryName(path) ?? "";
        }
        catch (Exception)
        {
            // ignore
        }
        
        return dialog.ShowDialog() ? dialog.FileOrFolderName : null;
    }
    
    
    private readonly OpenFileDialog _openFileDialog = new();

    public string Title
    {
        get => _openFileDialog.Title;
        set => _openFileDialog.Title = value;
    }

    public string FileOrFolderName
    {
        get => _openFileDialog.FileName;
        set => _openFileDialog.FileName = value;
    }
    
    public string InitialDirectory
    {
        get => _openFileDialog.InitialDirectory;
        set => _openFileDialog.InitialDirectory = value;
    }

    public bool ShowDialog()
    {
        var windowHandle = IntPtr.Zero;
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            var helper = new WindowInteropHelper(mainWindow);
            windowHandle = helper.Handle;
        }
        return RunVistaDialog(windowHandle);
    }


    // FileDialog.RunVistaDialog の改造版
    private bool RunVistaDialog(IntPtr hwndOwner)
    {
        _openFileDialog.CheckPathExists = false;
        _openFileDialog.CheckFileExists = false;
        _openFileDialog.Filter = @"All files (*.*)|*.*";

        var vistaDialog = (IFileOpenDialog)CallNonPublicMethod(_openFileDialog, "CreateVistaDialog");
        CallNonPublicMethod(_openFileDialog, "PrepareVistaDialog", [vistaDialog]);

        using var events = new VistaDialogEvents(vistaDialog,
            (dialog) => (bool)CallNonPublicMethod(_openFileDialog, "HandleVistaFileOk", [dialog],
                typeof(FileDialog))
        );

        var result = vistaDialog.Show(hwndOwner);
        var success = (result == 0);
        if (success)
        {
            FileOrFolderName = string.IsNullOrEmpty(events.ResultFolderPath)
                ? _openFileDialog.FileName
                : events.ResultFolderPath;
        }

        return success;


        static object CallNonPublicMethod(OpenFileDialog openFileDialog, string methodName, object[]? parameters = null, Type? type = null)
        {
            if (!MethodInfos.TryGetValue(methodName, out var methodInfo))
            {
                type ??= typeof(OpenFileDialog);
                methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (methodInfo == null)
                {
                    throw new InvalidOperationException($"Cannot find {methodName} method");
                }

                MethodInfos[methodName] = methodInfo;
            }

            return methodInfo.Invoke(openFileDialog, parameters);
        }
    }


    // フォルダを選択し開くボタンを押したらフォルダで決定されるようにイベント制御する
    // 通常はフォルダ内に移動する
    // フォルダ選択中に開くボタンを押されてもOnFileOk()は呼ばれない
    // OnFileChanging(),OnFolderChange()は呼ばれるが、開くボタン以外にもダブルクリックとエンターキーで同様に呼ばれるのでこれを区別する
    // OnFileChanging()でマウスカーソルが開くボタン上にあるかどうかで判定
    internal class VistaDialogEvents : IFileDialogEvents, IDisposable
    {
        private readonly IFileOpenDialog _dialog;
        private readonly uint _eventCookie;
        private readonly OnOkCallback? _okCallback;

        public string ResultFolderPath { get; private set; } = "";

        public VistaDialogEvents(IFileOpenDialog dialog, OnOkCallback? okCallback)
        {
            _dialog = dialog;
            _okCallback = okCallback;
            dialog.Advise(this, out _eventCookie);
        }

        public int OnFileOk(IFileDialog pfd) => (_okCallback?.Invoke(pfd) ?? true) ? 0 : 1;

        public int OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (pfd is not IOleWindow window) return 0;
            
            
            window.GetWindow(out var hwndDialog);

            // 「開く」ボタンのハンドルを取得（IDOK = 1）
            var hwndOpenButton = NativeMethods.GetDlgItem(hwndDialog, 1);

            // 「開く」ボタンの位置を取得
            NativeMethods.GetWindowRect(hwndOpenButton, out var buttonRect);


            // マウスの現在位置を取得
            NativeMethods.GetCursorPos(out var mousePos);


            // マウスが「開く」ボタン上にあるか判定
            var isMouseOverOpenButton = (mousePos.X >= buttonRect.Left && mousePos.X <= buttonRect.Right &&
                                         mousePos.Y >= buttonRect.Top && mousePos.Y <= buttonRect.Bottom);

            if (isMouseOverOpenButton)
            {
                ResultFolderPath = GetPath(psiFolder);
                _dialog.Close(0);
                
                return 1;
            }

            return 0;
        }

        public int OnFolderChange(IFileDialog pfd) => 0;

        public int OnSelectionChange(IFileDialog pfd) => 0;

        public int OnShareViolation(IFileDialog pfd, IShellItem psi, out int pResponse)
        {
            pResponse = 0;
            return 0;
        }

        public int OnTypeChange(IFileDialog pfd) => 0;

        public int OnOverwrite(
            IFileDialog pfd,
            IShellItem psi,
            out int pResponse)
        {
            pResponse = 0;
            return 0;
        }

        void IDisposable.Dispose() => _dialog.Unadvise(_eventCookie);

        public delegate bool OnOkCallback(IFileDialog dialog);

        private static string GetPath(IShellItem? item)
        {
            var path = "";
            item?.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);

            return path;
        }
    }
}