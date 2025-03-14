using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace Headquarters.NativeDialog;

[ComImport]
[Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    [PreserveSig]
    uint Show([In] IntPtr hwndParent);

    void SetFileTypes(); // not fully defined
    void SetFileTypeIndex(); // not fully defined
    void GetFileTypeIndex(); // not fully defined
    void Advise(IFileDialogEvents pfde, out uint pdwCookie);
    void Unadvise(uint pdwCookie);
    void SetOptions([In] FOS fos);
    void GetOptions(out FOS fos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetFileName();

    void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel(string pszText);
    void SetFileNameLabel(); // not fully defined
    void GetResult(out IShellItem ppsi);
    void AddPlace(); // not fully defined
    void SetDefaultExtension(); // not fully defined
    void Close(int result);
    void SetClientGuid(); // not fully defined
    void ClearClientData();
    void SetFilter(); // not fully defined
    void GetResults(); // not fully defined
    void GetSelectedItems(); // not fully defined
}

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(); // not fully defined
    void GetParent(); // not fully defined
    void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(); // not fully defined
    void Compare(); // not fully defined
}

[ComImport]
[Guid("00000114-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IOleWindow
{
    void GetWindow(out IntPtr phwnd);
}

internal enum SIGDN : uint
{
    SIGDN_FILESYSPATH = 0x80058000,
}

[Flags]
internal enum FOS
{
}

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
    
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);
    
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}


[ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog
{
}

[ComImport, Guid("B4DB1657-70D7-485E-8E3E-6FCB5A5C1802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialogEvents
{
    [PreserveSig]
    int OnFileOk(IFileDialog pfd);

    [PreserveSig]
    int OnFolderChanging(IFileDialog pfd, IShellItem psiFolder);

    [PreserveSig]
    int OnFolderChange(IFileDialog pfd);

    [PreserveSig]
    int OnSelectionChange(IFileDialog pfd);

    [PreserveSig]
    int OnShareViolation(IFileDialog pfd, IShellItem psi, out int pResponse);

    [PreserveSig]
    int OnTypeChange(IFileDialog pfd);

    [PreserveSig]
    int OnOverwrite(IFileDialog pfd, IShellItem psi, out int pResponse);
}