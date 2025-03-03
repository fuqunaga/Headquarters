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
    
    public static bool IsSymbolicLink(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }
    
    public static bool IsMissingTargetSymbolicLink(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return false;
        }
        
        var attrs = File.GetAttributes(path);
        
        if ((attrs & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
        {
            return false;
        }

        if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
        {
            var info = new DirectoryInfo(path);
            try
            {
                info.GetDirectories();
            }
            catch (DirectoryNotFoundException)
            {
                return true;
            }
        }
        else
        {
            var info = new FileInfo(path);
            try
            {
                info.OpenRead().Close();
            }
            catch (FileNotFoundException)
            {
                return true;
            }
        }

        return false;
    }
}