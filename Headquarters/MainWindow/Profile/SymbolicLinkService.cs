using System.IO;
using System.Runtime.InteropServices;

namespace Headquarters;

/// <summary>
/// .NET 4.8でシンボリックリンクを扱う
/// .NET 6ではDirectoryクラスにCreateSymbolicLinkメソッドが追加されている
/// </summary>
public static class SymbolicLinkService
{
    private enum SymbolicLink
    {
        File = 0,
        Directory = 1
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);


    public static bool CreateSymbolicLink(string symlinkFileName, string targetFileName)
    {
        var flag = Directory.Exists(targetFileName)
            ? SymbolicLink.Directory
            : SymbolicLink.File;
            
        return CreateSymbolicLink(symlinkFileName, targetFileName, flag);
    }
}