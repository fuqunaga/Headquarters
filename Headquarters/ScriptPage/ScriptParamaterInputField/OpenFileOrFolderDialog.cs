using System.IO;
using Microsoft.Win32;

namespace Headquarters;

/// <summary>
/// Select file or folder from the same dialog
/// 
/// https://www.codeproject.com/KB/dialog/OpenFileOrFolderDialog.aspx
/// </summary>
public class OpenFileOrFolderDialog
{
    private const string FileNameLabel = "Select folder";
    
    private readonly OpenFileDialog _openFileDialog = new()
    {
        ValidateNames = false,
        CheckFileExists = false,
        CheckPathExists = true,
        FileName = FileNameLabel
    };

    public string FileOrFolderName { get; private set; } = string.Empty;
    
    public bool ShowDialog()
    {
        if (_openFileDialog.ShowDialog() is not true)
        {
            return false;
        }

        FileOrFolderName = ConvertDialogFileNameToPath(_openFileDialog.FileName);
        return true;
    }

    private static string ConvertDialogFileNameToPath(string dialogFileName)
    {
        if (dialogFileName.EndsWith(FileNameLabel) || !File.Exists(dialogFileName))
        {
            return Path.GetDirectoryName(dialogFileName) ?? dialogFileName;
        }
        
        return dialogFileName;
    }
}